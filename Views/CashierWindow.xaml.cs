using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using POS.Client.Data;
using POS.Client.Models;
using POS.Client.Services;
using SyncService = POS.Client.Services.SyncService;

namespace POS.Client.Views
{
    public partial class CashierWindow : Window
    {
        private readonly SyncService _syncService;
        private readonly ApiService _apiService;
        private List<CartItem> _cart = new();
        private decimal _subtotal = 0;
        private decimal _tax = 0;
        private decimal _total = 0;
        private LocalProduct _selectedProduct = null;

        public CashierWindow()
        {
            InitializeComponent();
            _syncService = new SyncService();
            _apiService = new ApiService();
            _apiService.SetToken(AppState.AuthToken);
            LoadProducts();
            OpenShiftIfNeeded();
        }

        private void OpenShiftIfNeeded()
        {
            var db = new POSDbContext();

            var localOpen = db.Shifts.FirstOrDefault(s => s.Status == "open");
            if (localOpen != null)
            {
                AppState.CurrentShiftId = localOpen.LocalId;
                string serverTag = localOpen.ServerId.HasValue ? $"Server#{localOpen.ServerId}" : "Local";
                Title = $"Cashier - Shift #{localOpen.LocalId} ({serverTag})";
                return;
            }

            var localShift = new LocalShift
            {
                PosClientId = 1,
                UserId = AppState.CurrentUserId > 0 ? AppState.CurrentUserId : 1,
                StartingCash = 1000,
                Status = "open",
                OpenedAt = DateTime.Now
            };
            db.Shifts.Add(localShift);
            db.SaveChanges();

            AppState.CurrentShiftId = localShift.LocalId;
            Title = $"Cashier - Shift #{localShift.LocalId} (Local)";
        }

        private void LoadProducts()
        {
            var products = _syncService.GetProducts();
            // Set HasModifiers flag for UI
            foreach (var p in products)
            {
                p.HasModifiers = p.Modifiers != null && p.Modifiers.Count > 0;
            }
            icProducts.ItemsSource = products;
        }

        // ==================== PRODUCT CLICK ====================
        private void ProductButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var productId = (int)button.Tag;

            var product = _syncService.GetProducts().FirstOrDefault(p => p.ServerId == productId);
            if (product == null) return;

            _selectedProduct = product;

            // PRIORITY 1: Check for MODIFIERS first
            if (product.Modifiers != null && product.Modifiers.Count > 0)
            {
                var optionWindow = new ProductOptionWindow(product, product.BasePrice, _syncService);
                if (optionWindow.ShowDialog() == true)
                {
                    AddToCart(product.ServerId, null, product.Name, product.BasePrice, product.TaxRate, optionWindow.SelectedModifiers);
                }
                return;
            }

            // PRIORITY 2: Check for VARIANTS
            var variants = _syncService.GetVariants(product.ServerId);
            if (variants.Count > 0)
            {
                cbVariants.ItemsSource = variants;
                cbVariants.Tag = product;
                cbVariants.Visibility = Visibility.Visible;
                return;
            }

            // No options, add directly
            AddToCart(product.ServerId, null, product.Name, product.BasePrice, product.TaxRate);
        }

        // ==================== VARIANT SELECTION ====================
        private void cbVariants_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var variant = cbVariants.SelectedItem as LocalProductVariant;
            if (variant == null || _selectedProduct == null) return;

            var price = _selectedProduct.BasePrice + variant.PriceAdjustment;

            // Check for modifiers on variant selection too
            if (_selectedProduct.Modifiers != null && _selectedProduct.Modifiers.Count > 0)
            {
                var optionWindow = new ProductOptionWindow(_selectedProduct, price, _syncService);
                if (optionWindow.ShowDialog() == true)
                {
                    AddToCart(_selectedProduct.ServerId, variant.ServerId,
                        $"{_selectedProduct.Name} ({variant.Name})", price, _selectedProduct.TaxRate, optionWindow.SelectedModifiers);
                }
                cbVariants.SelectedItem = null;
                cbVariants.Visibility = Visibility.Collapsed;
                return;
            }

            AddToCart(_selectedProduct.ServerId, variant.ServerId,
                $"{_selectedProduct.Name} ({variant.Name})", price, _selectedProduct.TaxRate);
            cbVariants.SelectedItem = null;
            cbVariants.Visibility = Visibility.Collapsed;
        }

        // ==================== ADD TO CART ====================
        private void AddToCart(int productId, int? variantId, string name, decimal price, decimal taxRate, List<SelectedModifier> modifiers = null)
        {
            _cart.Add(new CartItem
            {
                ProductId = productId,
                VariantId = variantId,
                Name = name,
                Price = price,
                TaxRate = taxRate,
                Modifiers = modifiers ?? new List<SelectedModifier>()
            });
            UpdateCartDisplay();
        }

        // ==================== UPDATE DISPLAY ====================
        private void UpdateCartDisplay()
        {
            lbCart.Items.Clear();
            _subtotal = 0;
            _tax = 0;

            foreach (var item in _cart)
            {
                var line = item.Name;
                if (item.Modifiers.Count > 0)
                {
                    line += " + " + string.Join(", ", item.Modifiers.Select(m => m.Name));
                }
                line += $" - {item.TotalPrice:F2} THB";
                lbCart.Items.Add(line);
                _subtotal += item.TotalPrice;
                _tax += item.TotalPrice * (item.TaxRate / 100);
            }

            _total = _subtotal + _tax;

            txtSubtotal.Text = $"Subtotal: {_subtotal:F2} THB";
            txtTax.Text = $"Tax: {_tax:F2} THB";
            txtTotal.Text = $"Total: {_total:F2} THB";
        }

        // ==================== PAYMENT ====================
        private async void btnPay_Click(object sender, RoutedEventArgs e)
        {
            if (_cart.Count == 0)
            {
                MessageBox.Show("Cart is empty!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AppState.CurrentShiftId == 0)
            {
                MessageBox.Show("No open shift! Please restart Cashier.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var token = AppState.AuthToken;
                if (string.IsNullOrEmpty(token))
                {
                    MessageBox.Show("Please login first!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var sale = new LocalSale
                {
                    StoreId = AppState.CurrentStoreId,
                    WarehouseId = 1,
                    PosClientId = 1,
                    ShiftId = AppState.CurrentShiftId,
                    UserId = AppState.CurrentUserId > 0 ? AppState.CurrentUserId : 1,
                    Subtotal = _subtotal,
                    TaxTotal = _tax,
                    DiscountTotal = 0,
                    Total = _total,
                    CreatedAt = DateTime.Now
                };

                var items = new List<LocalSaleItem>();
                var payments = new List<LocalPayment>();

                foreach (var cartItem in _cart)
                {
                    var item = new LocalSaleItem
                    {
                        ProductId = cartItem.ProductId,
                        ProductName = cartItem.Name,
                        Quantity = 1,
                        UnitPrice = cartItem.TotalPrice,
                        TotalPrice = cartItem.TotalPrice,
                        DiscountAmount = 0
                    };

                    if (cartItem.Modifiers.Count > 0)
                    {
                        item.Modifiers = new List<LocalSaleItemModifier>();
                        foreach (var mod in cartItem.Modifiers)
                        {
                            item.Modifiers.Add(new LocalSaleItemModifier
                            {
                                ModifierOptionId = mod.OptionId,
                                Quantity = 1,
                                PriceAdjustment = mod.PriceAdjustment
                            });
                        }
                    }

                    items.Add(item);
                }

                payments.Add(new LocalPayment
                {
                    Method = "cash",
                    Amount = _total,
                    Reference = ""
                });

                var queue = new OfflineQueueService(token);
                int localId = queue.QueueSale(sale, items, payments);

                int pending = queue.GetPendingCount();

                MessageBox.Show(
                    $"✅ SALE SAVED\n\n" +
                    $"Local ID: {localId}\n" +
                    $"Total: {_total:F2} THB\n" +
                    $"Pending sync: {pending} sale(s)\n\n" +
                    $"Will sync automatically when server is available.",
                    "Sale Saved", MessageBoxButton.OK, MessageBoxImage.Information);

                _cart.Clear();
                UpdateCartDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== CART ITEM CLASS ====================
        public class CartItem
        {
            public int ProductId { get; set; }
            public int? VariantId { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public decimal TaxRate { get; set; }
            public List<SelectedModifier> Modifiers { get; set; } = new();

            public decimal TotalPrice => Price + Modifiers.Sum(m => m.PriceAdjustment);
        }
    }
}