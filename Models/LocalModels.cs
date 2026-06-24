using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Client.Models
{
    public class LocalProduct
    {
        [Key]
        public int ServerId { get; set; }
        public int StoreId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Sku { get; set; }
        public decimal BasePrice { get; set; }
        public decimal TaxRate { get; set; }
        public bool TrackInventory { get; set; }
        public bool AllowDecimalQty { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastUpdated { get; set; }

        [NotMapped]
        public bool HasModifiers { get; set; }

        [NotMapped]
        public bool HasAddons { get; set; }

        public List<LocalProductVariant> Variants { get; set; } = new List<LocalProductVariant>();
        public List<LocalProductModifier> Modifiers { get; set; } = new List<LocalProductModifier>();
        public List<LocalProductAddon> Addons { get; set; } = new List<LocalProductAddon>();
    }

    public class LocalProductVariant
    {
        [Key]
        public int ServerId { get; set; }
        public int ProductId { get; set; }
        public string Sku { get; set; }
        public string Name { get; set; }
        public decimal PriceAdjustment { get; set; }
        public bool IsActive { get; set; }
    }

    public class LocalProductModifier
    {
        [Key]
        public int ServerId { get; set; }
        public int ProductId { get; set; }
        public int GroupId { get; set; }
        public bool IsRequired { get; set; }
    }

    public class LocalModifierGroup
    {
        [Key]
        public int ServerId { get; set; }
        public int StoreId { get; set; }
        public string Name { get; set; }
        public string SelectionType { get; set; }
        public int MinSelect { get; set; }
        public int MaxSelect { get; set; }
        public bool IsActive { get; set; }
        public List<LocalModifierOption> Options { get; set; } = new List<LocalModifierOption>();
    }

    public class LocalModifierOption
    {
        [Key]
        public int ServerId { get; set; }
        public int GroupId { get; set; }
        public string Name { get; set; }
        public decimal PriceAdjustment { get; set; }
        public int? MaterialId { get; set; }
        public decimal? QuantityConsumed { get; set; }
        public bool IsActive { get; set; }
    }

    // ==================== ADDON (NUOVO) ====================
    public class LocalProductAddon
    {
        [Key]
        public int ServerId { get; set; }
        // public int LocalId { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; }
        public int MaxQuantity { get; set; } // 0 = illimitato
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<LocalProductAddonItem> Items { get; set; } = new List<LocalProductAddonItem>();
    }

    public class LocalProductAddonItem
    {
        [Key]
        public int ServerId { get; set; }
        public int AddonId { get; set; }
        public int AddonProductId { get; set; }
        public decimal QuantityValue { get; set; } // es. caraffa = 3
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }

        [NotMapped]
        public string AddonProductName { get; set; }
    }

    public class LocalMaterial
    {
        [Key]
        public int ServerId { get; set; }
        public int StoreId { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public string Category { get; set; }
        public decimal CostPerUnit { get; set; }
        public decimal? MinStock { get; set; }
        public bool IsActive { get; set; }
    }

    public class LocalInventory
    {
        [Key]
        public int Id { get; set; }
        public int WarehouseId { get; set; }
        public int MaterialId { get; set; }
        public decimal Quantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class LocalUser
    {
        [Key]
        public int ServerId { get; set; }
        public int StoreId { get; set; }
        public int RoleId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string RoleName { get; set; }
        public string PermissionsJson { get; set; }
        public bool IsActive { get; set; }
    }

    public class LocalSale
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LocalId { get; set; }
        public int? ServerId { get; set; }
        public int StoreId { get; set; }
        public int WarehouseId { get; set; }
        public int PosClientId { get; set; }
        public int ShiftId { get; set; }
        public int UserId { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = "pending";
        public string SyncError { get; set; } = string.Empty;
        public int RetryCount { get; set; } = 0;
        public int? CustomerCount { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? SyncedAt { get; set; }
        public List<LocalSaleItem> Items { get; set; } = new List<LocalSaleItem>();
        public List<LocalPayment> Payments { get; set; } = new List<LocalPayment>();
    }

    public class LocalSaleItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LocalId { get; set; }
        public int LocalSaleId { get; set; }
        public int ProductId { get; set; }
        public int? VariantId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public List<LocalSaleItemModifier> Modifiers { get; set; } = new List<LocalSaleItemModifier>();
        public List<LocalSaleItemAddon> Addons { get; set; } = new List<LocalSaleItemAddon>();
    }

    public class LocalSaleItemModifier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LocalId { get; set; }
        public int LocalSaleItemId { get; set; }
        public int ModifierOptionId { get; set; }
        public int Quantity { get; set; }
        public decimal PriceAdjustment { get; set; }
    }

    // ==================== SALE ITEM ADDON (NUOVO) ====================
    public class LocalSaleItemAddon
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LocalId { get; set; }
        public int LocalSaleItemId { get; set; }
        public int AddonProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityValue { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string AddonProductName { get; set; } = string.Empty;
    }

    public class LocalPayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LocalId { get; set; }
        public int LocalSaleId { get; set; }
        public string Method { get; set; }
        public decimal Amount { get; set; }
        public string Reference { get; set; }
    }

    public class LocalShift
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LocalId { get; set; }
        public int? ServerId { get; set; }
        public int PosClientId { get; set; }
        public int UserId { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal StartingCash { get; set; }
        public decimal? ExpectedCash { get; set; }
        public decimal? ActualCash { get; set; }
        public decimal? Difference { get; set; }
        public string Status { get; set; } = "open";
        public string Notes { get; set; } = string.Empty;
        public string SyncError { get; set; } = string.Empty;
        public int RetryCount { get; set; } = 0;
    }

    public class SyncState
    {
        [Key]
        public int Id { get; set; }
        public string EntityType { get; set; }
        public DateTime LastSyncAt { get; set; }
        public int LastVersion { get; set; }
    }

    public class AppConfig
    {
        [Key]
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}