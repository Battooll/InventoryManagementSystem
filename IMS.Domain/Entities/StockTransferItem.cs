using IMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Domain.Entities
{
    public class StockTransferItem : BaseEntity
    {
        public long StockTransferId { get; set; }
        public long ProductId { get; set; }
        public long SourceBatchId { get; set; } // Keep track of exactly which batch is traveling
        public int Quantity { get; set; }

        // Navigation Properties
        public StockTransfer StockTransfer { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public InventoryBatch SourceBatch { get; set; } = null!;
    }
}
