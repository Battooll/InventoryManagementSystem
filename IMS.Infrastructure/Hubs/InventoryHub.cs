using Microsoft.AspNetCore.SignalR;


namespace IMS.Infrastructure.Hubs
{
    public class InventoryHub : Hub
    {
        // Managers will connect to this hub stream to receive live alerts.
        // We leave this empty because clients only listen to server-sent broadcasts.
    }
}
