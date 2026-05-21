using IMS.Application.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IMS.Application.DTOs;
using IMS.Application.Exceptions;
using IMS.Application.Services;
using IMS.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace IMS.Infrastructure.Notifications
{
    public class InventoryNotificationService : IInventoryNotificationService
    {
        private readonly IHubContext<InventoryHub> _hubContext;

        public InventoryNotificationService(IHubContext<InventoryHub> hubContext)
        {
            _hubContext = hubContext;
        }
        public async Task NotifyThresholdWarningAsync(long warehouseId, long productId, int remainingStock, int threshold)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveThresholdWarning", new
            {
                WarehouseId = warehouseId,
                ProductId = productId,
                RemainingStock = remainingStock,
                Threshold = threshold,
                Message = $"Warning: Product {productId} in Warehouse {warehouseId} has fallen below safety threshold! Current Stock: {remainingStock}."
            });
        }
    }
}
