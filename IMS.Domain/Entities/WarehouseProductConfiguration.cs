using IMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Domain.Entities
{
    public class WarehouseProductConfiguration : BaseEntity
    {
        public long WarehouseId { get; set; }
        public long ProductId { get; set; }
        public int MinStockThreshold { get; set; } // Triggers the real-time alert

        // Navigation Properties
        public Warehouse Warehouse { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
