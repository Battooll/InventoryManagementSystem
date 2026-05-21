using IMS.Application.Interfaces;
using IMS.Application.Notifications;
using IMS.Application.Services;
using IMS.Infrastructure.Hubs;
using IMS.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
using IMS.Infrastructure;

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

app.Run();