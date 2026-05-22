# Advanced Enterprise Inventory Management System (IMS)

An enterprise-grade, highly optimized inventory tracking module built using **Clean Architecture** principles and **Domain-Driven Design (DDD)** metrics. This system implements precise tracking logic, concurrency safeguards, state-machine stock movements, real-time push alerts, and high-performance reporting.

---

## 🏗️ Architectural Blueprint & Clean Design

The solution is divided into strictly decoupled layers to enforce separation of concerns, high maintainability, and testing isolation:

* **`IMS.Domain` (Core Engine):** Contains enterprise business entities, value objects, and tracking configurations. Zero dependencies on external libraries or frameworks.
* **`IMS.Application` (Business Logic Orchestrator):** Defines business interface contracts, DTO configurations, and processing pipelines (FIFO allocation, stock movement state logic). Completely decoupled from infrastructure using interface abstractions (`IAppDbContext`).
* **`IMS.Infrastructure` (Data & Comms Boundary):** Implements EF Core `AppDbContext` data mapping, transaction execution logic, and WebSocket-driven SignalR alert push delivery networks.
* **`IMS.API` (Presentation Layer):** Exposes high-performance RESTful endpoints, handles HTTP pipeline middlewares, manages dependency injection configurations, and configures Swagger documentation UI.

---

## 🚀 Key Technical Problem Solutions

### 1. FIFO Inventory Allocation Engine (Problem 1)
To track historical acquisition costs and handle tiered profitability structures, inventory is recorded in granular **Batches**. 
* When a sales order is processed, the system executes a FIFO allocation algorithm that fetches available inventory batches ordered chronologically (`OrderBy(ib => ib.ReceivedAt)`).
* It drains older batches completely before allocating quantities from newer, varying-cost receipts.
* **Traceability Assurance:** The system records a direct foreign key relationship from `SalesOrderItems` to the exact originating `InventoryBatch` source row, enabling comprehensive audit trail reporting.

### 2. High-Performance Concurrency Guardrails (Problem 2)
To prevent race conditions where two concurrent sales operations try to claim the same limited stock, the system implements a two-layered protection strategy:
* **Application-Level Validations:** Evaluates stock levels dynamically before deduction processing.
* **Database-Level Optimistic Concurrency:** Utilizes a native SQL Server `rowversion` column (`byte[] RowVersion`) on the `InventoryBatches` table. If a concurrent request modifies the remaining quantity while another operation is mid-transaction, EF Core instantly identifies the token mismatch and throws a `DbUpdateConcurrencyException`, triggering an automatic transaction rollback to prevent over-allocation.

### 3. Transaction-Safe Stock Transfer Engine (Problem 3)
Moving inventory between physical warehouses introduces the risk of data anomalies if a network drop or system failure occurs mid-transit. The system implements a strict state-machine workflow:
* **Initiate (In-Transit):** Allocates inventory from the source warehouse using the FIFO loop, decrements the active quantity, and stores the items in a `StockTransfer` ledger record marked with a `TransferStatus.InTransit` state enum—all wrapped securely in an explicit `BeginTransactionAsync()` block.
* **Complete (Finalized):** Upon destination warehouse confirmation, the engine retrieves the unique moving trace metadata and automatically spawns *new corresponding receipt batches* localized to the target warehouse, preserving original unit costs and supplier tracking links.

### 4. Real-Time WebSocket Threshold Alerts (Problem 4)
Warehouse managers must be notified instantly when stock levels drop below a safe operating threshold.
* Immediately following a successful sales order save transaction, the system runs an automated post-commit hook checking total remaining aggregate stock against individual `WarehouseProductConfigurations`.
* If stock falls below the minimum limit, the application invokes `IInventoryNotificationService`, leveraging **ASP.NET Core SignalR** to broadcast real-time metrics downstream over WebSockets to all active clients.

### 5. High-Performance Query Reporting (Problem 5)
Historical trace reports can quickly become a bottleneck as rows scale into millions. The reporting engine bypasses standard performance overhead:
* **Non-Tracking Execution:** Uses `.AsNoTracking()` to avoid entity state tracking overhead, minimizing memory footprint and drastically speeding up DB read operations.
* **Index Alignment:** Formulates queries to align perfectly with optimized database composite indexes, allowing quick filtering by `WarehouseId`, `SupplierId`, `Category`, or custom date ranges.

---

## 🛠️ Local Installation & Testing Guide

### Prerequisites
* .NET 8.0 SDK
* SQL Server 2019 (or LocalDB instance)
* Visual Studio 2022

### Database Setup & Connection Configuration
1. Open the `appsettings.json` file inside the `IMS.API` project.
2. Update the `DefaultConnection` string to point to your local SQL Server instance:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=IMS_InventoryModuleDb;Trusted_Connection=True;TrustServerCertificate=True;"
   }