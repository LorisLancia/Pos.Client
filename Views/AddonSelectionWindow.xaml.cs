// Views/AddonSelectionWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using POS.Client.Models;
using POS.Client.Services;

namespace POS.Client.Views
{
    public partial class AddonSelectionWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly LocalProductAddon _addon;
        private readonly SyncService _syncService;
        private List<AddonItemViewModel> _items;

        public string AddonGroupName => _addon.Name;
        public string MaxQuantityText => _addon.MaxQuantity > 0 
            ? $"Max {_addon.MaxQuantity} items" 
            : "Unlimited quantity";

        public string TotalSelectedText => $"Selected: {TotalQuantity} / {(_addon.MaxQuantity > 0 ? _addon.MaxQuantity.ToString() : "∞")}";

        public int TotalQuantity => _items.Sum(i => i.Quantity);

        public List<SelectedAddon> SelectedAddons => _items
            .Where(i => i.Quantity > 0)
            .Select(i => new SelectedAddon
            {
                AddonProductId = i.AddonProductId,
                AddonProductName = i.ItemName,
                Quantity = i.Quantity,
                QuantityValue = i.QuantityValue,
                UnitPrice = i.UnitPrice
            })
            .ToList();

        public AddonSelectionWindow(LocalProductAddon addon, SyncService syncService)
        {
            InitializeComponent();
            _addon = addon;
            _syncService = syncService;
            DataContext = this;

            LoadItems();
        }

        private void LoadItems()
        {
            _items = new List<AddonItemViewModel>();

            foreach (var item in _addon.Items.Where(i => i.IsActive).OrderBy(i => i.SortOrder))
            {
                var addonProduct = _syncService.GetProducts().FirstOrDefault(p => p.ServerId == item.AddonProductId);
                
                _items.Add(new AddonItemViewModel
                {
                    AddonProductId = item.AddonProductId,
                    ItemName = addonProduct?.Name ?? $"Product #{item.AddonProductId}",
                    UnitPrice = addonProduct?.BasePrice ?? 0,
                    QuantityValue = item.QuantityValue,
                    Quantity = 0
                });
            }

            icAddonItems.ItemsSource = _items;
        }

        private void BtnDecrease_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button.Tag as AddonItemViewModel;
            if (item == null || item.Quantity <= 0) return;

            item.Quantity--;
            RefreshUI();
        }

        private void BtnIncrease_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button.Tag as AddonItemViewModel;
            if (item == null) return;

            // Check max quantity limit
            if (_addon.MaxQuantity > 0 && TotalQuantity >= _addon.MaxQuantity)
            {
                MessageBox.Show($"Maximum {_addon.MaxQuantity} addons allowed.", 
                    "Limit Reached", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            item.Quantity++;
            RefreshUI();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_addon.MaxQuantity > 0 && TotalQuantity > _addon.MaxQuantity)
            {
                MessageBox.Show($"Maximum {_addon.MaxQuantity} addons allowed. You selected {TotalQuantity}.",
                    "Limit Exceeded", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TotalQuantity == 0)
            {
                MessageBox.Show("Please select at least one addon.", 
                    "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void RefreshUI()
        {
            icAddonItems.Items.Refresh();
            OnPropertyChanged(nameof(TotalQuantity));
            OnPropertyChanged(nameof(TotalSelectedText));
        }

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class AddonItemViewModel
    {
        public int AddonProductId { get; set; }
        public string ItemName { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal QuantityValue { get; set; }
        public int Quantity { get; set; }

        public string PriceText => $"{UnitPrice:F2} THB (value: {QuantityValue})";
    }

    public class SelectedAddon
    {
        public int AddonProductId { get; set; }
        public string AddonProductName { get; set; }
        public int Quantity { get; set; }
        public decimal QuantityValue { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}