using IMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace IMS.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Warehouse> Warehouses { get; }
    DbSet<Product> Products { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<WarehouseProductConfiguration> WarehouseProductConfigurations { get; }
    DbSet<InventoryBatch> InventoryBatches { get; }
    DbSet<SalesOrder> SalesOrders { get; }
    DbSet<SalesOrderItem> SalesOrderItems { get; }
    DbSet<StockTransfer> StockTransfers { get; }
    DbSet<StockTransferItem> StockTransferItems { get; }

    DatabaseFacade Database { get; } // Allows our services to manage transactions
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}