using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Application.Notifications
{
    public interface IInventoryNotificationService
    {
        Task NotifyThresholdWarningAsync(long warehouseId, long productId, int remainingStock, int threshold);
    }
}
