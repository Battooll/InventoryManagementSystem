using IMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Domain.Entities
{
    public class SalesOrderItem : BaseEntity
    {
        public long SalesOrderId { get; set; }
        public long ProductId { get; set; }
        public long InventoryBatchId { get; set; } // The exact batch this quantity was taken from!

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Navigation Properties
        public SalesOrder SalesOrder { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public InventoryBatch InventoryBatch { get; set; } = null!;
    }
}
