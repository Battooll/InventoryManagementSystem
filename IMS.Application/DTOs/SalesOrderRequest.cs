using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Application.DTOs
{
    public record SalesOrderRequest(
    string OrderNumber,
    List<SalesOrderItemRequest> Items
);

    public record SalesOrderItemRequest(
        long ProductId,
        long WarehouseId,
        int Quantity,
        decimal UnitPrice
    );
}
