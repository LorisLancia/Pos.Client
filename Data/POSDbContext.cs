using Microsoft.EntityFrameworkCore;
using POS.Client.Models;

namespace POS.Client.Data


{
    public class POSDbContext : DbContext
    {
        public string DbPath { get; }
        public DbSet<AppConfig> AppConfigs { get; set; }

        public POSDbContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "POSClient.db");

            // Aggiungi questa riga:
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Esistenti
    modelBuilder.Entity<LocalSaleItem>()
        .HasOne<LocalSale>()
        .WithMany()
        .HasForeignKey(i => i.LocalSaleId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<LocalPayment>()
        .HasOne<LocalSale>()
        .WithMany()
        .HasForeignKey(p => p.LocalSaleId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<LocalSaleItemModifier>()
        .HasOne<LocalSaleItem>()
        .WithMany()
        .HasForeignKey(m => m.LocalSaleItemId)
        .OnDelete(DeleteBehavior.Cascade);

    // NUOVE - usa Restrict per evitare cicli
    modelBuilder.Entity<LocalProductAddon>()
        .HasOne<LocalProduct>()
        .WithMany(p => p.Addons)
        .HasForeignKey(a => a.ProductId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<LocalProductAddonItem>()
        .HasOne<LocalProductAddon>()
        .WithMany(a => a.Items)
        .HasForeignKey(i => i.AddonId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<LocalSaleItemAddon>()
        .HasOne<LocalSaleItem>()
        .WithMany()
        .HasForeignKey(a => a.LocalSaleItemId)
        .OnDelete(DeleteBehavior.Cascade);
}

        public DbSet<LocalProduct> Products { get; set; }
        public DbSet<LocalProductVariant> ProductVariants { get; set; }
        public DbSet<LocalProductModifier> ProductModifiers { get; set; }
        public DbSet<LocalModifierGroup> ModifierGroups { get; set; }
        public DbSet<LocalModifierOption> ModifierOptions { get; set; }
        public DbSet<LocalMaterial> Materials { get; set; }
        public DbSet<LocalInventory> Inventory { get; set; }
        public DbSet<LocalUser> Users { get; set; }
        public DbSet<LocalSale> Sales { get; set; }
        public DbSet<LocalSaleItem> SaleItems { get; set; }
        public DbSet<LocalSaleItemModifier> SaleItemModifiers { get; set; }
        public DbSet<LocalPayment> Payments { get; set; }
        public DbSet<LocalShift> Shifts { get; set; }
        public DbSet<SyncState> SyncStates { get; set; }
        public DbSet<LocalProductAddon> ProductAddons { get; set; }
        public DbSet<LocalProductAddonItem> ProductAddonItems { get; set; }
        public DbSet<LocalSaleItemAddon> SaleItemAddons { get; set; }
    }
}