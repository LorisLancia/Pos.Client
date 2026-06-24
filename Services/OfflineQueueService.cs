// Services/OfflineQueueService.cs
using Newtonsoft.Json;
using POS.Client.Data;
using POS.Client.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace POS.Client.Services
{
    public class OfflineQueueService
    {
        private readonly ApiService _api;
        private static readonly string LogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "POS_Client_Log.txt");

        public OfflineQueueService(string token)
        {
            _api = new ApiService();
            _api.SetToken(token);
        }

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(LogFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
            }
            catch { }
        }

        public int QueueSale(LocalSale sale, List<LocalSaleItem> items, List<LocalPayment> payments)
        {
            using var db = new POSDbContext();
            sale.Status = "pending";
            sale.CreatedAt = DateTime.Now;
            db.Sales.Add(sale);
            db.SaveChanges();

            foreach (var item in items)
            {
                item.LocalSaleId = sale.LocalId;
                db.SaleItems.Add(item);
                db.SaveChanges();

                foreach (var mod in item.Modifiers)
                {
                    mod.LocalSaleItemId = item.LocalId;
                    db.SaleItemModifiers.Add(mod);
                }

                foreach (var addon in item.Addons)
                {
                    addon.LocalSaleItemId = item.LocalId;
                    db.SaleItemAddons.Add(addon);
                }
            }

            foreach (var payment in payments)
            {
                payment.LocalSaleId = sale.LocalId;
                db.Payments.Add(payment);
            }

            db.SaveChanges();
            Log($">>> QueueSale: LocalId={sale.LocalId} saved with {items.Sum(i => i.Addons?.Count ?? 0)} addons");
            return sale.LocalId;
        }

        public async Task<int> TrySyncPendingSalesAsync()
        {
            using var db = new POSDbContext();

            var pending = db.Sales
                .Where(s => s.Status == "pending" || s.Status == "error")
                .Where(s => s.RetryCount < 5)
                .ToList();

            Log($">>> TrySync: found {pending.Count} pending");

            if (!pending.Any()) return 0;

            int syncedCount = 0;

            foreach (var sale in pending)
            {
                Log($">>> Processing LocalId: {sale.LocalId}");

                try
                {
                    var items = db.SaleItems
                        .Where(i => i.LocalSaleId == sale.LocalId)
                        .ToList();

                    var payments = db.Payments
                        .Where(p => p.LocalSaleId == sale.LocalId)
                        .ToList();

                    var saleDto = new
                    {
                        warehouseId = sale.WarehouseId,
                        posClientId = sale.PosClientId,
                        shiftId = sale.ShiftId,
                        userId = sale.UserId,
                        storeId = sale.StoreId,
                        startingCash = 1000,
                        clientSaleId = $"client-{sale.PosClientId}-{sale.LocalId}",
                        items = items.Select(i => new
                        {
                            productId = i.ProductId,
                            variantId = i.VariantId,
                            quantity = i.Quantity,
                            unitPrice = i.UnitPrice,
                            modifiers = db.SaleItemModifiers
                                .Where(m => m.LocalSaleItemId == i.LocalId)
                                .Select(m => new
                                {
                                    modifierOptionId = m.ModifierOptionId,
                                    quantity = m.Quantity
                                }).ToList(),
                            addons = db.SaleItemAddons
                                .Where(a => a.LocalSaleItemId == i.LocalId)
                                .Select(a => new
                                {
                                    addonProductId = a.AddonProductId,
                                    quantity = a.Quantity,
                                    quantityValue = a.QuantityValue,
                                    unitPrice = a.UnitPrice,
                                    totalPrice = a.TotalPrice
                                }).ToList()
                        }).ToList(),
                        payments = payments.Select(p => new
                        {
                            method = p.Method,
                            amount = p.Amount,
                            reference = p.Reference
                        }).ToList()
                    };

                    var result = await _api.CreateSaleAsync(saleDto);

                    Log($">>> API returned: Id={result?.Id}, SaleNumber={result?.SaleNumber}");

                    if (result == null || result.Id == 0)
                    {
                        Log(">>> ERROR: Invalid result");
                        sale.Status = "error";
                        sale.SyncError = "Invalid server response";
                        sale.RetryCount++;
                        db.SaveChanges();
                        continue;
                    }

                    sale.Status = "synced";
                    sale.ServerId = result.Id;
                    sale.SaleNumber = result.SaleNumber;
                    sale.SyncedAt = DateTime.Now;
                    db.SaveChanges();

                    var verify = db.Sales.Find(sale.LocalId);
                    Log($">>> Verify after save: Status={verify?.Status}, ServerId={verify?.ServerId}");

                    syncedCount++;
                    Log($">>> Synced! Count={syncedCount}");
                }
                catch (Exception ex)
                {
                    Log($">>> EXCEPTION: {ex.Message}");
                    string errorMsg = ex.Message ?? "";
                    bool isConnectionError = errorMsg.Contains("Unable to connect") ||
                                             errorMsg.Contains("connection") ||
                                             errorMsg.Contains("refused") ||
                                             errorMsg.Contains("timeout") ||
                                             errorMsg.Contains("Task was canceled");

                    if (isConnectionError)
                    {
                        Log(">>> Connection error, breaking");
                        break;
                    }
                    else
                    {
                        sale.Status = "error";
                        sale.SyncError = errorMsg.Length > 200 ? errorMsg.Substring(0, 200) : errorMsg;
                        sale.RetryCount++;
                        db.SaveChanges();
                    }
                }
            }

            Log($">>> TrySync completed. Synced: {syncedCount}");
            return syncedCount;
        }

        public LocalSale GetSaleByLocalId(int localId)
        {
            using var db = new POSDbContext();
            return db.Sales.FirstOrDefault(s => s.LocalId == localId);
        }

        public List<LocalSale> GetPendingSales()
        {
            using var db = new POSDbContext();
            return db.Sales
                .Where(s => s.Status == "pending" || s.Status == "error")
                .OrderByDescending(s => s.CreatedAt)
                .ToList();
        }

        public int GetPendingCount()
        {
            using var db = new POSDbContext();
            return db.Sales.Count(s => s.Status == "pending" || s.Status == "error");
        }
    }
}