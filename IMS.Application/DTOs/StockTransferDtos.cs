using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Application.DTOs
{
    public record InitiateTransferRequest(
    string TransferNumber,
    long SourceWarehouseId,
    long DestinationWarehouseId,
    List<TransferItemRequest> Items
);

    public record TransferItemRequest(
        long ProductId,
        int Quantity
    );

    public record CompleteTransferRequest(
        long TransferId
    );
}
