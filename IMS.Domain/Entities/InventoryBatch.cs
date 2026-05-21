using IMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Domain.Entities
{
    public class InventoryBatch : BaseEntity
    {
        public long ProductId { get; set; }
        public long SupplierId { get; set; }
        public long WarehouseId { get; set; }

        public string BatchNumber { get; set; } = string.Empty; // e.g., GRN-2026-05-001
        public int OriginalQuantity { get; set; }
        public int RemainingQuantity { get; set; } // This value decrements down toward 0 as we sell
        public decimal UnitCost { get; set; }
        public DateTime ReceivedAt { get; set; } // Critical for ordering items by FIFO

        // Concurrency Token: Essential to prevent race-conditions/negative stock (Problem 2)
        public byte[] RowVersion { get; set; } = null!;

        // Navigation Properties
        public Product Product { get; set; } = null!;
        public Supplier Supplier { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
    }
}
