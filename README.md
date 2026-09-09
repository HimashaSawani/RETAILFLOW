# RetailFlow

A modern, robust Retail Management and Point of Sale (POS) desktop application built with **WPF**, **C#**, **.NET 8**, and **Entity Framework Core (SQLite)** following the **MVVM (Model-View-ViewModel)** architectural pattern.

---

## Overview

**RetailFlow** is designed to streamline day-to-day retail operations for small-to-medium businesses. It provides an intuitive interface for product inventory management, real-time stock tracking with restock capabilities, a fast Point of Sale (POS) checkout system with strict stock validation and atomic database transactions, comprehensive transaction history with itemized receipts, and an analytics sales dashboard.

---

## Features

### 1. 📦 Product Management
- **Full CRUD Operations**: Add, view, edit, and delete products with confirmation dialogs.
- **Real-Time Search**: Instant filtering by SKU, Product Name, or Category.
- **Detailed Product Fields**: SKU, Name, Category, Cost Price, Selling Price, Stock Quantity, and Reorder Level.
- **Strict Validations**: Rejects duplicate SKUs, empty fields, and negative numbers.

### 2. 📈 Stock Management & Restocking
- **Inventory Status Badges**:
  - `OK` (Green): Stock is above reorder threshold.
  - `LOW` (Amber): Stock is at or below reorder threshold.
  - `OUT` (Red): Out of stock (0 units).
- **One-Click Restocking**: Interactive modal displaying current stock, quantity to add, and dynamic live preview of new stock level before saving.

### 3. 🛒 Point of Sale (POS) & Checkout
- **Product Selection**: Searchable product dropdown with live price and stock indicators.
- **Cart Management**: Add items, adjust quantities, calculate line totals, and remove items.
- **Instant Calculations**: Automatic computation of Subtotal, custom Discount, and Total.
- **Inventory Stock Guard**: Prevents adding more items than currently in stock (`❌ Insufficient Stock`).

### 4. 🔒 Atomic Database Transactions (ACID)
- All sales operations (Sale creation, SaleItem records, and Product stock deduction) run within a single **Entity Framework Core database transaction (`BeginTransactionAsync`)**.
- If any step fails or concurrency conflicts arise, the transaction rolls back completely with zero stock deduction.
- Generates sequential invoice identifiers (e.g., `#INV-0001`, `#INV-0002`).
- Displays an itemized post-sale receipt dialog with transition tracking (e.g., `Coca Cola: 25 ➔ 23`).

### 5. 📜 Transaction History
- Historical log of all completed sales with invoice numbers, timestamps, total items, and amounts.
- Interactive detail panel displaying complete line-item breakdowns, unit prices, discounts, and final totals.
- Search and filter transactions by invoice number or date.

### 6. 📊 Sales Dashboard (Additional Feature)
- **4 Key Performance Indicator (KPI) Cards**:
  - Today's Sales Revenue (Rs.)
  - Total Transactions Count
  - Total Products in Catalog
  - Low Stock Alerts Count
- **Best-Selling Products**: Ranked list of top products by units sold and revenue.
- **7-Day Sales Bar Chart**: Visual trend chart mapping daily revenue across the last 7 days.

---

## Technologies

- **Language**: C# 12
- **Framework**: .NET 8.0 (Windows Desktop / WPF)
- **Design Pattern**: MVVM (Model-View-ViewModel)
- **Database Engine**: SQLite (`retailflow.db`)
- **ORM**: Entity Framework Core 8.0 (`Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Tools`)
- **UI Architecture**: XAML with custom styles, responsive layouts, data binding, and converters

---

## Requirements

- **Operating System**: Windows 10 (Build 19041+) or Windows 11
- **Runtime / SDK**: .NET 8.0 SDK or .NET 8.0 Desktop Runtime
- **IDE (Optional)**: Visual Studio 2022 (with *.NET Desktop Development* workload) or VS Code

---

## Installation

1. **Clone or Extract the Project Folder**:
   ```bash
   cd d:\RETAILFLOW
   ```

2. **Restore Dependencies**:
   ```bash
   dotnet restore
   ```

---

## Running the Application

### Via Command Line:
```bash
dotnet run
```

### Via Visual Studio:
1. Open `RetailFlow.sln` in Visual Studio 2022.
2. Ensure the build configuration is set to **Debug** or **Release** (Any CPU).
3. Press **F5** or click **Start** to run.

---

## Database

- **Zero Manual Database Setup**: The application automatically creates and configures the SQLite database file `retailflow.db` in the application execution folder upon launch using `context.Database.EnsureCreated()`.
- **Pre-Seeded Demo Data**: Includes initial demo products and historical transactions out of the box for immediate demonstration.

---

## Project Structure

```
RetailFlow/
│
├── Models/
│   ├── Product.cs              # Product entity (SKU, Name, Price, Cost, Stock, Reorder)
│   ├── Sale.cs                 # Sale entity (Invoice #, Date, Subtotal, Discount, Total)
│   ├── SaleItem.cs             # Sale line item entity linking Sales to Products
│   └── SaleReceipt.cs          # Data transfer model for receipts
│
├── Data/
│   └── AppDbContext.cs         # SQLite DbContext configuration & EF Core model mapping
│
├── Services/
│   ├── ProductService.cs       # Product CRUD, duplicate SKU checking, search
│   ├── StockService.cs         # Stock level queries and restock processing
│   └── SalesService.cs         # ACID sales transactions & dashboard analytics queries
│
├── ViewModels/
│   ├── ViewModelBase.cs        # INotifyPropertyChanged implementation
│   ├── MainViewModel.cs        # Main shell & navigation coordinator
│   ├── DashboardViewModel.cs   # KPI metrics, top products, and weekly chart logic
│   ├── ProductManagementViewModel.cs # Product CRUD & filtering logic
│   ├── ProductFormViewModel.cs # Add/Edit form state & validation
│   ├── StockManagementViewModel.cs   # Stock status table & restock triggers
│   ├── StockItemViewModel.cs   # Dynamic stock status badge calculator
│   ├── RestockDialogViewModel.cs     # Restock calculation & execution
│   ├── PosViewModel.cs         # POS cart, stock guard, and checkout workflow
│   ├── CartItemViewModel.cs    # Cart line item model
│   ├── TransactionHistoryViewModel.cs # Transaction history & detail panel
│   └── ReceiptDialogViewModel.cs      # Completed sale receipt presentation
│
├── Views/
│   ├── DashboardView.xaml / .cs           # Sales Dashboard screen
│   ├── ProductManagementView.xaml / .cs   # Product CRUD screen
│   ├── ProductFormDialog.xaml / .cs       # Add/Edit product modal dialog
│   ├── StockManagementView.xaml / .cs     # Stock status screen
│   ├── RestockDialog.xaml / .cs           # Restock modal dialog
│   ├── PosView.xaml / .cs                 # Point of Sale (POS) screen
│   ├── TransactionHistoryView.xaml / .cs  # Transaction history & receipt view
│   └── ReceiptDialog.xaml / .cs           # Post-checkout receipt dialog
│
├── Commands/
│   └── RelayCommand.cs         # ICommand implementation for MVVM command binding
│
├── Helpers/
│   └── BarHeightConverter.cs   # Value converter for dynamic chart bar heights
│
├── App.xaml / App.xaml.cs      # Application entry point & global crash handlers
├── MainWindow.xaml / .cs       # Shell window with sidebar navigation
├── RetailFlow.csproj           # Project configuration (.NET 8 WPF + EF Core)
├── RetailFlow.sln              # Visual Studio solution file
└── README.md                   # Complete documentation
```

---

## Validation Rules

| Rule | Enforcement Point | Behavior |
|---|---|---|
| **Empty SKU** | `ProductFormViewModel` | Blocks submission; displays `SKU cannot be empty. ❌` |
| **Empty Product Name** | `ProductFormViewModel` | Blocks submission; displays `Product Name cannot be empty. ❌` |
| **Negative Price** | `ProductFormViewModel` | Rejects negative `CostPrice` or `SellingPrice` |
| **Negative Stock** | `ProductFormViewModel` | Rejects negative `StockQuantity` or `ReorderLevel` |
| **Duplicate SKU** | `ProductFormViewModel` & `ProductService` | Case-insensitive database check prevents duplicate SKUs |
| **Insufficient Stock** | `PosViewModel` & `SalesService` | Blocks adding to cart or checkout if requested quantity > stock |
| **Empty Cart Sale** | `PosViewModel` | Disables and guards the `Complete Sale` action |
| **Invalid Restock Qty** | `RestockDialogViewModel` | Restock quantity must be greater than zero |

---

## Additional Feature: 📊 Sales Dashboard

The **Sales Dashboard** provides immediate business intelligence to retail managers:
1. **Real-time KPI Tiles**: Today's Revenue, Transaction Count, Total Catalog Items, and Urgent Low-Stock Alerts.
2. **Top Selling Products**: Dynamically aggregates units sold and revenue from all transaction line items.
3. **Weekly Sales Bar Chart**: A lightweight XAML chart displaying sales volume over the last 7 days with weekday headers.

---

## Screenshots

### 1. Main Navigation & Dashboard
<img width="1485" height="937" alt="image" src="https://github.com/user-attachments/assets/97d6d474-8ba8-4539-a6e9-6e463e077cbc" />


### 2. Point of Sale (POS) & Cart
<img width="1478" height="932" alt="image" src="https://github.com/user-attachments/assets/26ad96ba-e30a-458a-9a15-c4f4f415bf52" />


---

## Error Handling & Reliability

- **Never-Crash Architecture**: Global unhandled exception handlers (`DispatcherUnhandledException`, `AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`) in `App.xaml.cs` catch all runtime exceptions gracefully and display helpful alerts rather than crashing.
- **ACID Reliability**: Database operations utilize transactions with automatic rollback on error.
