using IMS.Application.DTOs;
using IMS.Application.Exceptions;
using IMS.Application.Interfaces;
using IMS.Domain.Entities;
using IMS.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Application.Services
{
    public class StockTransferService : IStockTransferService
    {
        private readonly IAppDbContext _context;

        public StockTransferService(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<long> InitiateTransferAsync(InitiateTransferRequest request)
        {
            // Execute inside a database transaction block
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var transfer = new StockTransfer
                {
                    TransferNumber = request.TransferNumber,
                    SourceWarehouseId = request.SourceWarehouseId,
                    DestinationWarehouseId = request.DestinationWarehouseId,
                    Status = TransferStatus.InTransit,
                    ShippedAt = DateTime.UtcNow
                };

                foreach (var itemRequest in request.Items)
                {
                    // Find batches in the Source Warehouse using FIFO ordering
                    var availableBatches = await _context.InventoryBatches
                        .Where(ib => ib.ProductId == itemRequest.ProductId &&
                                     ib.WarehouseId == request.SourceWarehouseId &&
                                     ib.RemainingQuantity > 0)
                        .OrderBy(ib => ib.ReceivedAt)
                        .ToListAsync();

                    int totalAvailable = availableBatches.Sum(b => b.RemainingQuantity);
                    if (totalAvailable < itemRequest.Quantity)
                    {
                        var prod = await _context.Products.FindAsync(itemRequest.ProductId);
                        throw new InsufficientStockException(prod?.Name ?? "Unknown Product", itemRequest.Quantity, totalAvailable);
                    }

                    int remainingToDeduct = itemRequest.Quantity;

                    foreach (var batch in availableBatches)
                    {
                        if (remainingToDeduct <= 0) break;

                        int deduction = Math.Min(batch.RemainingQuantity, remainingToDeduct);

                        batch.RemainingQuantity -= deduction;
                        remainingToDeduct -= deduction;

                        // Track exactly which source batch this physical stock was extracted from
                        transfer.Items.Add(new StockTransferItem
                        {
                            ProductId = itemRequest.ProductId,
                            SourceBatchId = batch.Id,
                            Quantity = deduction
                        });
                    }
                }

                _context.StockTransfers.Add(transfer);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return transfer.Id;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                throw new InventoryConcurrencyException("Conflict identified while transferring inventory. Another process modified the stock status.", ex);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task CompleteTransferAsync(CompleteTransferRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var transfer = await _context.StockTransfers
                    .Include(st => st.Items)
                    .ThenInclude(sti => sti.SourceBatch)
                    .FirstOrDefaultAsync(st => st.Id == request.TransferId);

                if (transfer == null || transfer.Status != TransferStatus.InTransit)
                {
                    throw new KeyNotFoundException("Active In-Transit transfer record not found.");
                }

                // Update transfer state machine metadata
                transfer.Status = TransferStatus.Completed;
                transfer.ReceivedAt = DateTime.UtcNow;

                foreach (var item in transfer.Items)
                {
                    // Create a matching batch inside the destination warehouse 
                    // Retaining the original Supplier, original unit costs, and traceability link!
                    var newDestinationBatch = new InventoryBatch
                    {
                        ProductId = item.ProductId,
                        SupplierId = item.SourceBatch.SupplierId,
                        WarehouseId = transfer.DestinationWarehouseId,
                        BatchNumber = $"{item.SourceBatch.BatchNumber}-TRF",
                        OriginalQuantity = item.Quantity,
                        RemainingQuantity = item.Quantity,
                        UnitCost = item.SourceBatch.UnitCost,
                        ReceivedAt = DateTime.UtcNow
                    };

                    _context.InventoryBatches.Add(newDestinationBatch);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
