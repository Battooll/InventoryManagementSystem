using IMS.Domain.Common;
using IMS.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Domain.Entities
{
    public class StockTransfer : BaseEntity
    {
        public string TransferNumber { get; set; } = string.Empty;
        public long SourceWarehouseId { get; set; }
        public long DestinationWarehouseId { get; set; }
        public TransferStatus Status { get; set; } = TransferStatus.Draft;

        public DateTime? ShippedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }

        public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();

        // Navigation Properties
        public Warehouse SourceWarehouse { get; set; } = null!;
        public Warehouse DestinationWarehouse { get; set; } = null!;
    }
}
