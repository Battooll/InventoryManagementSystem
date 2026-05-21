using IMS.Domain.Common;
using IMS.Domain.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IMS.Application.Interfaces; // Add this using statement

namespace IMS.Infrastructure
{
    public class AppDbContext : DbContext, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<WarehouseProductConfiguration> WarehouseProductConfigurations => Set<WarehouseProductConfiguration>();
        public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();
        public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
        public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
        public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
        public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all Fluent Configurations automatically from this assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            modelBuilder.Entity<SalesOrderItem>()
            .HasOne(soi => soi.Product)
            .WithMany()
            .HasForeignKey(soi => soi.ProductId)
            .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<StockTransferItem>()
            .HasOne(sti => sti.Product)
            .WithMany()
            .HasForeignKey(sti => sti.ProductId)
            .OnDelete(DeleteBehavior.NoAction);
            // Automatically apply global Soft Delete query filter to all entities deriving from BaseEntity
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                    var falseConstant = Expression.Constant(false);
                    var compare = Expression.Equal(property, falseConstant);
                    var lambda = Expression.Lambda(compare, parameter);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                    entry.Entity.CreatedBy = "SystemUser"; // Soft-coded for assignment simplicity
                    entry.Entity.IsDeleted = false;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.ModifiedAtUtc = DateTime.UtcNow;
                    entry.Entity.ModifiedBy = "SystemUser";
                }
                else if (entry.State == EntityState.Deleted)
                {
                    // Intercept Hard Delete commands and switch them to Soft Delete updates
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.ModifiedAtUtc = DateTime.UtcNow;
                    entry.Entity.ModifiedBy = "SystemUser";
                }
            }
        }
    }
}