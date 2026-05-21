using IMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Infrastructure.Data.Configurations
{
    public class InventoryBatchConfiguration : IEntityTypeConfiguration<InventoryBatch>
    {
        public void Configure(EntityTypeBuilder<InventoryBatch> builder)
        {
            builder.HasKey(ib => ib.Id);
            builder.Property(ib => ib.BatchNumber).IsRequired().HasMaxLength(100);
            builder.Property(ib => ib.UnitCost).HasColumnType("decimal(18,4)");

            // Optimistic Concurrency Token Setup
            builder.Property(ib => ib.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            // High-Performance Composite Indexes for FIFO & Reporting (Problems 1 & 5)
            builder.HasIndex(ib => new { ib.WarehouseId, ib.ProductId, ib.RemainingQuantity })
                .HasDatabaseName("IX_InventoryBatch_FIFO_Lookup")
                .HasFilter("[IsDeleted] = 0");

            builder.HasIndex(ib => new { ib.WarehouseId, ib.SupplierId, ib.ProductId })
                .HasDatabaseName("IX_InventoryBatch_Reporting_Filter")
                .HasFilter("[IsDeleted] = 0");
        }
    }
}
