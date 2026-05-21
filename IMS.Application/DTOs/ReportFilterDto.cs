using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Application.DTOs
{
    public record ReportFilterDto(
     long? WarehouseId,
     long? SupplierId,
     string? ProductCategory,
     DateTime? StartDate,
     DateTime? EndDate
 );

    public record InventoryReportResponse(
        long BatchId,
        string BatchNumber,
        string ProductName,
        string SKU,
        string Category,
        string SupplierName,
        string WarehouseName,
        int OriginalQuantity,
        int RemainingQuantity,
        decimal UnitCost,
        DateTime ReceivedAt
    );
}
