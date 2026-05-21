using IMS.Application.DTOs;
using IMS.Application.Exceptions;
using IMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using IMS.Application.Interfaces;
using IMS.Application.Notifications;
namespace IMS.Application.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IAppDbContext _context;
        private readonly IInventoryNotificationService _notificationService; // 1. Inject the notification interface

        public InventoryService(IAppDbContext context, IInventoryNotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }
        public async Task<long> ProcessSalesOrderAsync(SalesOrderRequest request)
        {
            // 1. Begin an explicit database transaction for block safety
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var salesOrder = new SalesOrder
                {
                    OrderNumber = request.OrderNumber,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = 0 // Will compute dynamically below
                };

                decimal calculatedTotal = 0;

                // ==========================================
                // LOOP 1: STOCK ALLOCATION & DEDUCTION (FIFO)
                // ==========================================
                foreach (var itemRequest in request.Items)
                {
                    // Fetch available inventory batches ordered by oldest first (FIFO)
                    var availableBatches = await _context.InventoryBatches
                        .Where(ib => ib.ProductId == itemRequest.ProductId &&
                                     ib.WarehouseId == itemRequest.WarehouseId &&
                                     ib.RemainingQuantity > 0)
                        .OrderBy(ib => ib.ReceivedAt)
                        .ToListAsync();

                    int totalAvailableStock = availableBatches.Sum(b => b.RemainingQuantity);

                    // Guard clause if there isn't enough inventory across all batches
                    if (totalAvailableStock < itemRequest.Quantity)
                    {
                        var product = await _context.Products.FindAsync(itemRequest.ProductId);
                        throw new InsufficientStockException(product?.Name ?? "Unknown Product", itemRequest.Quantity, totalAvailableStock);
                    }

                    int remainingToFulfill = itemRequest.Quantity;

                    foreach (var batch in availableBatches)
                    {
                        if (remainingToFulfill <= 0) break;

                        int quantityFromThisBatch = Math.Min(batch.RemainingQuantity, remainingToFulfill);

                        // Deduct from the batch tracking record
                        batch.RemainingQuantity -= quantityFromThisBatch;
                        remainingToFulfill -= quantityFromThisBatch;

                        // Save fulfillment reference pointing directly to the origin batch (Traceability)
                        var orderItem = new SalesOrderItem
                        {
                            ProductId = itemRequest.ProductId,
                            InventoryBatchId = batch.Id, // The Magic Link for traceability reports
                            Quantity = quantityFromThisBatch,
                            UnitPrice = itemRequest.UnitPrice
                        };

                        salesOrder.Items.Add(orderItem);
                        calculatedTotal += (quantityFromThisBatch * itemRequest.UnitPrice);
                    }
                }

                salesOrder.TotalAmount = calculatedTotal;
                _context.SalesOrders.Add(salesOrder);

                // Save modifications locally. If another thread changed stock simultaneously, 
                // EF Core will throw a DbUpdateConcurrencyException right here.
                await _context.SaveChangesAsync();

                // ==========================================
                // LOOP 2: REAL-TIME THRESHOLD ALERTS (Problem 4)
                // ==========================================
                // This runs AFTER SaveChangesAsync so that it calculates true, updated quantities.
                foreach (var itemRequest in request.Items)
                {
                    // Calculate total remaining aggregate stock for this product in this warehouse
                    int absoluteRemaining = await _context.InventoryBatches
                        .Where(ib => ib.ProductId == itemRequest.ProductId &&
                                     ib.WarehouseId == itemRequest.WarehouseId &&
                                     ib.RemainingQuantity > 0)
                        .SumAsync(ib => ib.RemainingQuantity);

                    // Fetch safety configuration thresholds
                    var config = await _context.WarehouseProductConfigurations
                        .FirstOrDefaultAsync(wpc => wpc.WarehouseId == itemRequest.WarehouseId &&
                                                    wpc.ProductId == itemRequest.ProductId);

                    // If the stock falls below the threshold, fire an instant SignalR notice
                    if (config != null && absoluteRemaining < config.MinStockThreshold)
                    {
                        await _notificationService.NotifyThresholdWarningAsync(
                            itemRequest.WarehouseId,
                            itemRequest.ProductId,
                            absoluteRemaining,
                            config.MinStockThreshold
                        );
                    }
                }

                // Commit the transaction only when both parts pass successfully
                await transaction.CommitAsync();

                return salesOrder.Id;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Catch race conditions cleanly (Problem 2 Safeguard)
                await transaction.RollbackAsync();
                throw new InventoryConcurrencyException("The stock for one or more items was updated by another user. Please refresh and try your transaction again.", ex);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}