using IMS.Application.DTOs;
using IMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Application.Services
{
    internal class ReportService : IReportService
    {
        private readonly IAppDbContext _context;

        public ReportService(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<List<InventoryReportResponse>> GetInventoryReportAsync(ReportFilterDto filter)
        {
            // .AsNoTracking() drastically lowers memory footprints and speeds up execution on large lists
            var query = _context.InventoryBatches
                .AsNoTracking()
                .Include(ib => ib.Product)
                .Include(ib => ib.Supplier)
                .Include(ib => ib.Warehouse)
                .AsQueryable();

            // Dynamically append expressions matching our compound database indexes
            if (filter.WarehouseId.HasValue)
                query = query.Where(ib => ib.WarehouseId == filter.WarehouseId.Value);

            if (filter.SupplierId.HasValue)
                query = query.Where(ib => ib.SupplierId == filter.SupplierId.Value);

            if (!string.IsNullOrWhiteSpace(filter.ProductCategory))
                query = query.Where(ib => ib.Product.Category == filter.ProductCategory);

            if (filter.StartDate.HasValue)
                query = query.Where(ib => ib.ReceivedAt >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(ib => ib.ReceivedAt <= filter.EndDate.Value);

            return await query
                .Select(ib => new InventoryReportResponse(
                    ib.Id,
                    ib.BatchNumber,
                    ib.Product.Name,
                    ib.Product.SKU,
                    ib.Product.Category,
                    ib.Supplier.Name,
                    ib.Warehouse.Name,
                    ib.OriginalQuantity,
                    ib.RemainingQuantity,
                    ib.UnitCost,
                    ib.ReceivedAt
                ))
                .ToListAsync();
        }
    }
}
