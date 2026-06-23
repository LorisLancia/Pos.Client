using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using POS.Client.Data;
using POS.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace POS.Client.Services
{
    public class SyncService
    {
        private readonly ApiService _apiService;
        private readonly POSDbContext _db;

        public SyncService()
        {
            _apiService = new ApiService();
            _db = new POSDbContext();
        }

        public async Task SyncProductsAsync(int storeId)
        {
            var products = await _apiService.GetProductsForPOSAsync(storeId);
            var inventory = await _apiService.GetInventoryForPOSAsync(1);

            // Clear old data
            _db.ProductVariants.RemoveRange(_db.ProductVariants);
            _db.ProductModifiers.RemoveRange(_db.ProductModifiers);
            _db.ModifierOptions.RemoveRange(_db.ModifierOptions);
            _db.ModifierGroups.RemoveRange(_db.ModifierGroups);
            _db.Products.RemoveRange(_db.Products);
            _db.Materials.RemoveRange(_db.Materials);
            _db.Inventory.RemoveRange(_db.Inventory);
            _db.SaveChanges();

            foreach (var p in products)
            {
                var product = new LocalProduct
                {
                    ServerId = p.Id,
                    StoreId = storeId,
                    Name = p.Name,
                    BasePrice = p.BasePrice,
                    TaxRate = p.TaxRate,
                    IsActive = true,
                    LastUpdated = DateTime.Now
                };

                _db.Products.Add(product);
                _db.SaveChanges();

                foreach (var v in p.Variants ?? new List<VariantResponse>())
                {
                    _db.ProductVariants.Add(new LocalProductVariant
                    {
                        ServerId = v.Id,
                        ProductId = p.Id,
                        Name = v.Name,
                        PriceAdjustment = v.PriceAdjustment,
                        IsActive = true
                    });
                }

                foreach (var m in p.Modifiers ?? new List<ModifierResponse>())
                {
                    _db.ProductModifiers.Add(new LocalProductModifier
                    {
                        ServerId = m.Id,
                        ProductId = p.Id,
                        GroupId = m.Group?.Id ?? 0,
                        IsRequired = m.IsRequired
                    });

                    if (m.Group != null)
                    {
                        if (!_db.ModifierGroups.Any(g => g.ServerId == m.Group.Id))
                        {
                            var group = new LocalModifierGroup
                            {
                                ServerId = m.Group.Id,
                                StoreId = storeId,
                                Name = m.Group.Name,
                                SelectionType = m.Group.SelectionType,
                                MinSelect = 0,
                                MaxSelect = 1,
                                IsActive = true
                            };
                            _db.ModifierGroups.Add(group);
                            _db.SaveChanges();

                            foreach (var o in m.Group.Options ?? new List<OptionResponse>())
                            {
                                _db.ModifierOptions.Add(new LocalModifierOption
                                {
                                    ServerId = o.Id,
                                    GroupId = m.Group.Id,
                                    Name = o.Name,
                                    PriceAdjustment = o.PriceAdjustment,
                                    IsActive = true
                                });
                            }
                        }
                    }
                }
            }

            foreach (var inv in inventory)
            {
                _db.Inventory.Add(new LocalInventory
                {
                    WarehouseId = inv.WarehouseId,
                    MaterialId = inv.MaterialId,
                    Quantity = inv.Quantity,
                    LastUpdated = DateTime.Now
                });

                if (!_db.Materials.Any(m => m.ServerId == inv.MaterialId))
                {
                    _db.Materials.Add(new LocalMaterial
                    {
                        ServerId = inv.MaterialId,
                        StoreId = storeId,
                        Name = inv.Material?.Name ?? "Unknown",
                        Unit = inv.Material?.Unit ?? "piece",
                        IsActive = true
                    });
                }
            }

            _db.SaveChanges();
        }

        public List<LocalProduct> GetProducts()
        {
            return _db.Products.Where(p => p.IsActive).ToList();
        }

        public List<LocalProductVariant> GetVariants(int productId)
        {
            return _db.ProductVariants.Where(v => v.ProductId == productId && v.IsActive).ToList();
        }

        public List<LocalModifierGroup> GetModifierGroups(int productId)
        {
            var groupIds = _db.ProductModifiers
                .Where(m => m.ProductId == productId)
                .Select(m => m.GroupId)
                .ToList();

            return _db.ModifierGroups
                .Where(g => groupIds.Contains(g.ServerId))
                .ToList();
        }

        public List<LocalModifierOption> GetModifierOptions(int groupId)
        {
            return _db.ModifierOptions.Where(o => o.GroupId == groupId && o.IsActive).ToList();
        }
        public LocalModifierGroup GetModifierGroup(int groupId)
        {
            using var db = new POSDbContext();
            return db.ModifierGroups
                .Include(g => g.Options)
                .FirstOrDefault(g => g.ServerId == groupId);
        }
    }
}