using IMS.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Infrastructure.Data.Configurations
{
    public class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
    {
        public void Configure(EntityTypeBuilder<StockTransfer> builder)
        {
            builder.HasKey(st => st.Id);
            builder.Property(st => st.TransferNumber).IsRequired().HasMaxLength(100);

            // Configure explicit relationships bypassing cyclic dependency issues
            builder.HasOne(st => st.SourceWarehouse)
                .WithMany()
                .HasForeignKey(st => st.SourceWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(st => st.DestinationWarehouse)
                .WithMany()
                .HasForeignKey(st => st.DestinationWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
