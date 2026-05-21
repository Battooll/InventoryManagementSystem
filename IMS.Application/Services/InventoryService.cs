using IMS.Application.DTOs;
using IMS.Application.Exceptions;
using IMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using IMS.Application.Interfaces;
namespace IMS.Application.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IAppDbContext _context;

        public InventoryService(IAppDbContext context)
        {
            _context = context;
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

                foreach (var itemRequest in request.Items)
                {
                    // 2. Fetch available inventory batches using our optimal FIFO composite index
                    var availableBatches = await _context.InventoryBatches
                        .Where(ib => ib.ProductId == itemRequest.ProductId &&
                                     ib.WarehouseId == itemRequest.WarehouseId &&
                                     ib.RemainingQuantity > 0)
                        .OrderBy(ib => ib.ReceivedAt)
                        .ToListAsync();

                    int totalAvailableStock = availableBatches.Sum(b => b.RemainingQuantity);

                    if (totalAvailableStock < itemRequest.Quantity)
                    {
                        var product = await _context.Products.FindAsync(itemRequest.ProductId);
                        throw new InsufficientStockException(product?.Name ?? "Unknown Product", itemRequest.Quantity, totalAvailableStock);
                    }

                    int remainingToFulfill = itemRequest.Quantity;

                    // 3. FIFO Allocation Loop (Problem 1 Strategy)
                    foreach (var batch in availableBatches)
                    {
                        if (remainingToFulfill <= 0) break;

                        int quantityFromThisBatch = Math.Min(batch.RemainingQuantity, remainingToFulfill);

                        // Deduct from batch tracking records
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

                // 4. Persist to DB. If a concurrency clash happens, EF Core automatically flags it here.
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return salesOrder.Id;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Problem 2 Safeguard: Catch race conditions cleanly
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
