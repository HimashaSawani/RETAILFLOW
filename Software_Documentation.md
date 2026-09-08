# RetailFlow — Comprehensive Software Documentation

**Project Name:** RetailFlow  
**Version:** 1.0.0  
**Target Platform:** Windows (.NET 8.0 WPF)  
**Database Engine:** SQLite (`retailflow.db`) with Entity Framework Core 8  
**Architecture:** Model-View-ViewModel (MVVM)  

---

## 1. Executive Summary

**RetailFlow** is a modern desktop Retail Management and Point of Sale (POS) system engineered for high performance, ease of use, data consistency, and architectural maintainability.

The application allows retail operators to manage products, monitor live inventory with visual alert badges, execute restock orders, perform rapid sales checkout with strict stock validation and ACID database transactions, review historical itemized receipts, and analyze real-time sales trends via an interactive dashboard.

---

## 2. System Architecture & Design Pattern

### 2.1 MVVM (Model-View-ViewModel) Pattern
RetailFlow strictly adheres to the MVVM design pattern:

```
┌────────────────────────────────────────────────────────┐
│                        VIEW                            │
│  (MainWindow, DashboardView, ProductManagementView,    │
│   StockManagementView, PosView, TransactionHistoryView)│
└───────────────────────────▲────────────────────────────┘
                            │ Data Binding & Commands
┌───────────────────────────▼────────────────────────────┐
│                      VIEWMODEL                         │
│  (MainViewModel, DashboardVM, ProductManagementVM,     │
│   StockManagementVM, PosVM, TransactionHistoryVM)      │
└───────────────────────────▲────────────────────────────┘
                            │ Method Invocations
┌───────────────────────────▼────────────────────────────┐
│                      SERVICES                          │
│  (ProductService, StockService, SalesService)          │
└───────────────────────────▲────────────────────────────┘
                            │ Entity Framework Core 8
┌───────────────────────────▼────────────────────────────┐
│                    DATA & MODELS                       │
│  (AppDbContext, Product, Sale, SaleItem, SQLite DB)    │
└────────────────────────────────────────────────────────┘
```

- **Separation of Concerns**: Views contain zero business logic and only define layout and UI styling.
- **Data Binding**: Two-way data binding and `INotifyPropertyChanged` in `ViewModelBase` ensure instant synchronization between UI and data models.
- **Testability**: Services and ViewModels can be tested independently of the UI layer.

---

## 3. Database Design & Entity Relationships

### 3.1 Entity Relationship Diagram (ERD)

```
┌──────────────────────────┐             ┌──────────────────────────┐
│         Products         │             │          Sales           │
├──────────────────────────┤             ├──────────────────────────┤
│ PK  Id (int)             │             │ PK  Id (int)             │
│     SKU (string)         │             │     TransactionNo (str)  │
│     Name (string)        │             │     SaleDate (DateTime)  │
│     Category (string)    │             │     Subtotal (decimal)   │
│     CostPrice (decimal)  │             │     Discount (decimal)   │
│     SellingPrice(decimal)│             │     Total (decimal)      │
│     StockQuantity (int)  │             └────────────┬─────────────┘
│     ReorderLevel (int)   │                          │ 1
│     CreatedAt (DateTime) │                          │
└────────────┬─────────────┘                          │
             │ 1                                      │
             │                                        │
             │           ┌──────────────────────┐     │
             │           │      SaleItems       │     │
             │           ├──────────────────────┤     │
             │           │ PK  Id (int)         │     │
             └──────────<│ FK  ProductId (int)  │     │
               (Restrict)│ FK  SaleId (int)     │>────┘
                         │     Quantity (int)   │ (Cascade)
                         │     UnitPrice (dec)  │
                         │     Subtotal (dec)   │
                         └──────────────────────┘
```

### 3.2 Tables & Column Specifications
1. **Products Table**:
   - `Id` (INT, Primary Key, Auto-Increment)
   - `SKU` (NVARCHAR(50), Required, Unique)
   - `Name` (NVARCHAR(100), Required)
   - `Category` (NVARCHAR(50))
   - `CostPrice` (DECIMAL(18,2))
   - `SellingPrice` (DECIMAL(18,2))
   - `StockQuantity` (INT)
   - `ReorderLevel` (INT, Default 5)
   - `CreatedAt` (DATETIME, UTC)

2. **Sales Table**:
   - `Id` (INT, Primary Key, Auto-Increment)
   - `TransactionNumber` (NVARCHAR(50), Required, Sequential e.g., `INV-0001`)
   - `SaleDate` (DATETIME, UTC)
   - `Subtotal` (DECIMAL(18,2))
   - `Discount` (DECIMAL(18,2), Default 0)
   - `Total` (DECIMAL(18,2))

3. **SaleItems Table**:
   - `Id` (INT, Primary Key, Auto-Increment)
   - `SaleId` (INT, Foreign Key -> `Sales.Id`, ON DELETE CASCADE)
   - `ProductId` (INT, Foreign Key -> `Products.Id`, ON DELETE RESTRICT)
   - `Quantity` (INT)
   - `UnitPrice` (DECIMAL(18,2))
   - `Subtotal` (DECIMAL(18,2))

---

## 4. Key Functional Modules

### 4.1 Product Management (Feature #1)
- Full CRUD interface (Add, Edit, Delete, Search).
- Live search query against SKU, Name, and Category.
- Validations: Rejection of empty SKUs, empty names, negative pricing, negative inventory, and duplicate SKUs.

### 4.2 Stock Management & Restocking (Feature #2)
- Visual indicators:
  - `OK` (Stock > ReorderLevel)
  - `LOW` (0 < Stock <= ReorderLevel)
  - `OUT` (Stock == 0)
- Restock modal with live calculation of updated stock (`CurrentStock + QuantityToAdd`).

### 4.3 Point of Sale (POS) (Feature #3)
- Product lookup with unit prices and live available stock count.
- Cart with line totals, custom discount adjustment, and instant Total recalculation.

### 4.4 Stock Validation During Sale (Phase 10)
- Real-time stock validation at both cart addition and final checkout.
- If quantity requested exceeds stock: displays `❌ Insufficient Stock: Only X units available` and prohibits checkout.

### 4.5 Atomic Sales Database Transactions (Phase 11 & 12)
- Uses Entity Framework Core's `BeginTransactionAsync()`.
- Guarantees atomicity:
  1. Validate Cart
  2. Verify Real-time Stock
  3. Create `Sale` and generate `INV-XXXX`
  4. Create `SaleItems`
  5. Deduct `Product.StockQuantity`
  6. Commit transaction
- If any operation fails, `RollbackAsync()` is executed and zero stock is deducted.

### 4.6 Transaction History (Feature #4)
- Displays all past sales with date/time, item counts, and totals.
- Selecting any row renders the complete receipt breakdown in the side panel.

### 4.7 Sales Dashboard (Feature #5 - Additional Feature)
- KPI Cards: Today's Revenue, Transaction Count, Product Count, and Low Stock Alerts.
- Best-Selling Products table sorted by units sold.
- 7-Day sales volume bar chart.

---

## 5. Error Handling & Reliability Strategy

1. **Global Unhandled Exception Handlers**: In `App.xaml.cs` for `DispatcherUnhandledException`, `AppDomain.CurrentDomain.UnhandledException`, and `TaskScheduler.UnobservedTaskException`.
2. **Never-Crash Principle**: User operations always present friendly error dialogues rather than crashing the process.
3. **Database Fault Tolerance**: Database connection and creation errors are caught gracefully on startup.

---

## 6. Test Suite & Verification Results

All 19 verification test scenarios executed with **100% pass rate**:

| # | Test Scenario | Category | Expected Result | Status |
|---|---|---|---|---|
| 1 | Add product | Products | Insert product with ID > 0 | **PASS** ☑ |
| 2 | Edit product | Products | Update price/stock | **PASS** ☑ |
| 3 | Search product | Products | Filter products by keyword | **PASS** ☑ |
| 4 | Duplicate SKU validation | Products | Block duplicate SKU | **PASS** ☑ |
| 5 | Invalid price validation | Products | Reject negative prices | **PASS** ☑ |
| 6 | Delete product | Products | Delete product from database | **PASS** ☑ |
| 7 | Add product to cart | Sales / POS | Line item added to cart | **PASS** ☑ |
| 8 | Change quantity | Sales / POS | Correct quantity in cart | **PASS** ☑ |
| 9 | Calculate total | Sales / POS | Total matches Subtotal - Discount | **PASS** ☑ |
| 10 | Prevent insufficient stock | Sales / POS | Prohibit adding > available stock | **PASS** ☑ |
| 11 | Complete sale | Sales / POS | Generates `INV-XXXX` receipt | **PASS** ☑ |
| 12 | Stock decreases | Sales / POS | Stock reduced accurately | **PASS** ☑ |
| 13 | Transaction appears | History | Sale stored in database | **PASS** ☑ |
| 14 | Transaction details work | History | Line items retrieved | **PASS** ☑ |
| 15 | Correct total | History | Total matches invoice | **PASS** ☑ |
| 16 | Correct date/time | History | UTC timestamp recorded | **PASS** ☑ |
| 17 | Sales total correct | Dashboard | Aggregates daily sales | **PASS** ☑ |
| 18 | Transaction count correct | Dashboard | Counts daily sales | **PASS** ☑ |
| 19 | Low stock count correct | Dashboard | Counts items <= reorder level | **PASS** ☑ |
