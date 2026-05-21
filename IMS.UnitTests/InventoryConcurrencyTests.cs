using IMS.Application.DTOs;
using IMS.Application.Exceptions;
using IMS.Application.Notifications;
using IMS.Application.Services;
using IMS.Domain.Entities;
using IMS.Infrastructure;
using IMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace IMS.UnitTests;

public class InventoryConcurrencyTests
{
    private DbContextOptions<AppDbContext> CreateNewInMemoryDatabaseOptions()
    {
        // Creates a uniquely isolated in-memory database configuration per test execution run
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task ProcessSalesOrder_ConcurrentRequests_ShouldPreventNegativeStock()
    {
        // Arrange
        var options = CreateNewInMemoryDatabaseOptions();

        // Setup mock notification service
        var mockNotificationService = new Mock<IInventoryNotificationService>();

        // Seed initial data using an isolated context instance
        using (var context = new AppDbContext(options))
        {
            var product = new Product { Id = 1, SKU = "PROD-001", Name = "Test Product", Category = "Electronics" };
            var supplier = new Supplier { Id = 1, Name = "Supplier A", ContactEmail = "a@supplier.com" };
            var warehouse = new Warehouse { Id = 1, Name = "Central Warehouse", BranchCode = "WH01", Location = "Building A" };

            context.Products.Add(product);
            context.Suppliers.Add(supplier);
            context.Warehouses.Add(warehouse);

            // Seed an inventory batch with exactly 5 items available
            context.InventoryBatches.Add(new InventoryBatch
            {
                Id = 1,
                ProductId = 1,
                SupplierId = 1,
                WarehouseId = 1,
                BatchNumber = "BATCH-001",
                OriginalQuantity = 5,
                RemainingQuantity = 5,
                UnitCost = 10.00m,
                ReceivedAt = DateTime.UtcNow,
                RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
            });

            await context.SaveChangesAsync();
        }

        // Prepare two distinct requests trying to buy 4 items each at the exact same time
        var requestUser1 = new SalesOrderRequest("INV-2026-001", new List<SalesOrderItemRequest>
        {
            new SalesOrderItemRequest(ProductId: 1, WarehouseId: 1, Quantity: 4, UnitPrice: 15.00m)
        });

        var requestUser2 = new SalesOrderRequest("INV-2026-002", new List<SalesOrderItemRequest>
        {
            new SalesOrderItemRequest(ProductId: 1, WarehouseId: 1, Quantity: 4, UnitPrice: 15.00m)
        });

        // Act
        using var contextUser1 = new AppDbContext(options);
        var serviceUser1 = new InventoryService(contextUser1, mockNotificationService.Object);

        int successfulOrdersCount = 0;
        List<Exception> caughtExceptions = new List<Exception>();

        // 1. User 1 submits their request first and successfully claims 4 out of 5 items
        try
        {
            await serviceUser1.ProcessSalesOrderAsync(requestUser1);
            successfulOrdersCount++;
        }
        catch (Exception ex)
        {
            caughtExceptions.Add(ex);
        }

        // 2. Open a completely fresh context for User 2 so the shared in-memory cache is forced to reload
        using var contextUser2 = new AppDbContext(options);
        var serviceUser2 = new InventoryService(contextUser2, mockNotificationService.Object);

        // 3. User 2 concurrently attempts to claim 4 items when only 1 item remains
        try
        {
            await serviceUser2.ProcessSalesOrderAsync(requestUser2);
            successfulOrdersCount++;
        }
        catch (Exception ex)
        {
            caughtExceptions.Add(ex);
        }

        // Assert
        // 1. Verify that exactly one operation succeeded and the second one was safely blocked
        Assert.Equal(1, successfulOrdersCount);

        // 2. Verify that the system threw an InsufficientStockException to protect inventory integrity
        Assert.Single(caughtExceptions);
        Assert.IsType<InsufficientStockException>(caughtExceptions[0]);

        // 3. Confirm that the final remaining database stock is exactly 1 (5 minus 4)
        using (var verifyContext = new AppDbContext(options))
        {
            var remainingStock = await verifyContext.InventoryBatches
                .Where(ib => ib.Id == 1)
                .Select(ib => ib.RemainingQuantity)
                .FirstOrDefaultAsync();

            Assert.Equal(1, remainingStock);
        }
    }
}