using IMS.Application.Interfaces;
using IMS.Application.Notifications;
using IMS.Application.Services;
using IMS.Infrastructure.Hubs;
using IMS.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
using IMS.Infrastructure;
using IMS.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// 1. Add SQL Server 2019 Database Connection Setup
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("IMS.Infrastructure")));

// 2. Bind the decoupled application database interface contract
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

// 3. Register Core Business Application Logic Engine Services
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IStockTransferService, StockTransferService>();
builder.Services.AddScoped<IReportService, ReportService>();

// 4. Register Real-Time Infrastructure Services
builder.Services.AddScoped<IInventoryNotificationService, InventoryNotificationService>();

// 5. Add Controllers and Web-socket SignalR primitives
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Configure Swagger/OpenAPI options
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure HTTP pipeline middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// 6. Map the WebSocket SignalR downstream endpoint routing
app.MapHub<InventoryHub>("/hubs/inventory");

//insertion into the tables
// Clean and isolate the startup seed execution block
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Completely wipe and reset the schema layout safely
    context.Database.EnsureDeleted();
    context.Database.EnsureCreated();
}

// STAGE 1: Seed Parent Records (Products, Suppliers, Warehouses)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!context.Products.Any())
    {
        var now = DateTime.UtcNow;
        const string systemUser = "SystemSeed";

        var product = new Product
        {
            SKU = "PROD-FIFO-01",
            Name = "Premium Wireless Mouse",
            Category = "Electronics",
            CreatedBy = systemUser,
            CreatedAtUtc = now
        };

        var supplier = new Supplier
        {
            Name = "Global Tech Distributors",
            ContactEmail = "supply@globaltech.com",
            CreatedBy = systemUser,
            CreatedAtUtc = now
        };

        var warehouse1 = new Warehouse
        {
            Name = "Damascus Central Hub",
            BranchCode = "WH-DMS-01",
            Location = "Building A",
            CreatedBy = systemUser,
            CreatedAtUtc = now
        };

        var warehouse2 = new Warehouse
        {
            Name = "Aleppo Logistics Center",
            BranchCode = "WH-ALP-02",
            Location = "Building B",
            CreatedBy = systemUser,
            CreatedAtUtc = now
        };

        context.Products.Add(product);
        context.Suppliers.Add(supplier);
        context.Warehouses.AddRange(warehouse1, warehouse2);

        context.SaveChanges(); // This forces SQL Server to commit and generate real identity IDs
    }
}

// STAGE 2: Seed Dependent Child Records (Batches & Threshold Configurations)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Safely look up our committed records from Stage 1
    var product = context.Products.First(p => p.SKU == "PROD-FIFO-01");
    var supplier = context.Suppliers.First(s => s.Name == "Global Tech Distributors");
    var warehouse1 = context.Warehouses.First(w => w.BranchCode == "WH-DMS-01");

    if (!context.InventoryBatches.Any())
    {
        var now = DateTime.UtcNow;
        const string systemUser = "SystemSeed";

        // Seed initial inventory batches referencing our tracked entity objects
        context.InventoryBatches.Add(new InventoryBatch
        {
            ProductId = product.Id,
            SupplierId = supplier.Id,
            WarehouseId = warehouse1.Id,
            BatchNumber = "GRN-2026-05-001",
            OriginalQuantity = 10,
            RemainingQuantity = 10,
            UnitCost = 15.50m,
            ReceivedAt = now.AddDays(-5),
            CreatedBy = systemUser,
            CreatedAtUtc = now
        });

        context.InventoryBatches.Add(new InventoryBatch
        {
            ProductId = product.Id,
            SupplierId = supplier.Id,
            WarehouseId = warehouse1.Id,
            BatchNumber = "GRN-2026-05-002",
            OriginalQuantity = 10,
            RemainingQuantity = 10,
            UnitCost = 18.00m,
            ReceivedAt = now,
            CreatedBy = systemUser,
            CreatedAtUtc = now
        });

        // Seed a threshold configuration linked directly to our tracking objects
        context.WarehouseProductConfigurations.Add(new WarehouseProductConfiguration
        {
            WarehouseId = warehouse1.Id,
            ProductId = product.Id,
            MinStockThreshold = 5,
            CreatedBy = systemUser,
            CreatedAtUtc = now
        });

        context.SaveChanges();
    }
}
app.Run();