using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using POS.Client.Models;
using SyncService = POS.Client.Services.SyncService;

namespace POS.Client.Views
{
    public partial class ProductOptionWindow : Window
    {
        private readonly LocalProduct _product;
        private readonly decimal _basePrice;
        private readonly SyncService _syncService;
        public List<SelectedModifier> SelectedModifiers { get; private set; } = new();

        public ProductOptionWindow(LocalProduct product, decimal basePrice, SyncService syncService)
        {
            InitializeComponent();
            _product = product;
            _basePrice = basePrice;
            _syncService = syncService;
            txtProductName.Text = product.Name;
            BuildOptions();
            UpdateTotal();
        }

        private void BuildOptions()
        {
            if (_product.Modifiers == null || _product.Modifiers.Count == 0) return;

            foreach (var mod in _product.Modifiers)
            {
                var group = _syncService.GetModifierGroup(mod.GroupId);
                if (group == null) continue;

                var gb = new GroupBox
                {
                    Header = group.Name + (mod.IsRequired ? " *" : ""),
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(10)
                };

                var panel = new StackPanel();
                gb.Content = panel;

                if (group.SelectionType == "MULTI")
                {
                    foreach (var opt in group.Options)
                    {
                        var cb = new CheckBox
                        {
                            Content = $"{opt.Name} (+{opt.PriceAdjustment:F2} THB)",
                            Tag = opt,
                            Margin = new Thickness(0, 2, 0, 2)
                        };
                        cb.Checked += (s, e) => UpdateTotal();
                        cb.Unchecked += (s, e) => UpdateTotal();
                        panel.Children.Add(cb);
                    }
                }
                else // SINGLE
                {
                    var combo = new ComboBox { Margin = new Thickness(0, 2, 0, 2) };
                    combo.Items.Add(new ComboBoxItem { Content = "None", Tag = null });
                    foreach (var opt in group.Options)
                    {
                        combo.Items.Add(new ComboBoxItem
                        {
                            Content = $"{opt.Name} (+{opt.PriceAdjustment:F2} THB)",
                            Tag = opt
                        });
                    }
                    combo.SelectedIndex = 0;
                    combo.SelectionChanged += (s, e) => UpdateTotal();
                    panel.Children.Add(combo);
                }

                spOptions.Children.Insert(spOptions.Children.Count - 1, gb);
            }
        }

        private void UpdateTotal()
        {
            decimal adjustment = 0;
            SelectedModifiers.Clear();

            foreach (var child in spOptions.Children)
            {
                if (child is GroupBox gb && gb.Content is StackPanel panel)
                {
                    foreach (var ctrl in panel.Children)
                    {
                        if (ctrl is CheckBox cb && cb.IsChecked == true && cb.Tag is LocalModifierOption opt)
                        {
                            adjustment += opt.PriceAdjustment;
                            SelectedModifiers.Add(new SelectedModifier
                            {
                                OptionId = opt.ServerId,
                                Name = opt.Name,
                                PriceAdjustment = opt.PriceAdjustment,
                                GroupId = opt.GroupId
                            });
                        }
                        else if (ctrl is ComboBox combo && combo.SelectedItem is ComboBoxItem item && item.Tag is LocalModifierOption opt2)
                        {
                            adjustment += opt2.PriceAdjustment;
                            SelectedModifiers.Add(new SelectedModifier
                            {
                                OptionId = opt2.ServerId,
                                Name = opt2.Name,
                                PriceAdjustment = opt2.PriceAdjustment,
                                GroupId = opt2.GroupId
                            });
                        }
                    }
                }
            }

            txtTotal.Text = $"Total: {_basePrice + adjustment:F2} THB";
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            foreach (var mod in _product.Modifiers.Where(m => m.IsRequired))
            {
                var group = _syncService.GetModifierGroup(mod.GroupId);
                if (group == null) continue;

                bool hasSelection = SelectedModifiers.Any(sm => sm.GroupId == mod.GroupId);
                if (!hasSelection)
                {
                    MessageBox.Show($"Please select an option for: {group.Name}", "Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class SelectedModifier
    {
        public int OptionId { get; set; }
        public int GroupId { get; set; }
        public string Name { get; set; }
        public decimal PriceAdjustment { get; set; }
    }
}