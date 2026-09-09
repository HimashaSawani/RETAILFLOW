# RetailFlow — Software Documentation

**Project Name:** RetailFlow — Retail Inventory & POS Desktop Application  
**Target Platform:** Windows (.NET 8.0 WPF)  
**Database:** SQLite (`retailflow.db`) with Entity Framework Core 8  
**Architecture:** Model-View-ViewModel (MVVM)  
**Author:** Software Engineering Intern Technical Assignment (Hayleys Aventura)  

---

## 1. Application Overview & Main Features

**RetailFlow** is a modern, lightweight, and robust desktop Retail Management and Point of Sale (POS) system built using .NET 8 WPF. It provides retail store operators and cashiers with an intuitive solution to manage products, maintain live stock, process sales transactions with instant cash tender/change calculations, inspect transaction history, and monitor business health via an executive dashboard.

### Main Features:
1. **Product Management (Feature #1)**:
   - Full CRUD (Create, Read, Update, Delete) product management.
   - Real-time search by Product Name, SKU, or Category.
   - **Category Filter Dropdown** to instantly view specific product groups.
   - Strict field validations (unique SKU check, non-empty names, positive pricing, non-negative inventory).

2. **Sales Screen / Point of Sale (Feature #2)**:
   - Fast product lookup with unit prices and real-time available stock count.
   - Interactive cart with quantity adjustments, single-click row deletion (`✕`), and custom discounts.
   - **Payment Method Selection**: Cash, Card, QR / Bank Transfer.
   - **Cash Tender & Change Due**: Live change calculation (`Tendered - Total`), quick cash presets (`Exact`, `+50`, `+100`, `+500`, `+1000`, `+5000`), and under-payment prevention.
   - Real-time stock validation preventing sales exceeding available physical inventory.

3. **Stock Management (Feature #3)**:
   - Visual stock health badges:
     - 🟢 `IN STOCK` (Stock > Reorder Level)
     - 🟡 `LOW STOCK` (0 < Stock $\le$ Reorder Level)
     - 🔴 `OUT OF STOCK` (Stock = 0)
   - 1-Click **Quick Restock Modal** with live calculated new stock.
   - Automatic real-time stock deduction upon checkout.

4. **Transaction History (Feature #4)**:
   - Searchable list of all completed sales with invoice numbers (`INV-0001`, `INV-0002`, etc.).
   - **Date Filter**: Quick filter by *All Time, Today, This Week*.
   - Itemized slide-out drawer showing complete receipt breakdown, payment method, amount paid, and change returned.

5. **Additional Features (Feature #5)**:
   - **Real-Time Executive Analytics Dashboard**: KPI cards (*Today's Revenue, Gross Profit, Total Transactions, Product Count, Low Stock Alerts*), 5 most critical low-stock items table, best-sellers ranking, and a 7-day sales bar chart.
   - **Print / PDF Receipt Generator**: Modal receipt with native Windows `PrintDialog` and formatted text file export (`Receipt_INV-XXXX.txt`).

---

## 2. Technologies Used & Reasons for Choices

| Technology | Role | Reason for Choice |
| :--- | :--- | :--- |
| **.NET 8.0 & C#** | Core Framework & Logic | Modern, high-performance, strongly typed runtime with long-term support (LTS). |
| **WPF (Windows Presentation Foundation)** | Desktop UI Framework | Industry-standard for Windows desktop applications. Offers powerful data binding, XAML styling, and clean MVVM decoupling over older WinForms. |
| **SQLite (`Microsoft.EntityFrameworkCore.Sqlite`)** | Embedded Relational Database | Lightweight, serverless, self-contained single-file database. Requires **zero installation** for reviewers—the app creates and runs the database automatically. |
| **Entity Framework Core 8** | ORM / Data Access | Provides type-safe LINQ queries, automatic schema management, and ACID transaction support (`BeginTransactionAsync()`). |
| **MVVM Pattern** | Software Architecture | Strict separation of Concerns: Views (XAML) $\leftrightarrow$ ViewModels (State/Commands) $\leftrightarrow$ Services (Business Logic) $\leftrightarrow$ Data (EF Core). |

---

## 3. How to Set Up & Run the Application

### Prerequisites:
- Windows 10 or Windows 11 PC
- [.NET 8.0 SDK / Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (with *.NET desktop development* workload) or VS Code

### Steps to Run:
1. **Extract ZIP File**: Extract `FirstName_LastName_RetailAssignment.zip` to a folder.
2. **Open the Solution**:
   - Double-click `RetailFlow.sln` in Visual Studio 2022 and press **F5** (or `Ctrl + F5`).
3. **Or Run via Command Line (PowerShell / Terminal)**:
   ```powershell
   cd d:\RETAILFLOW
   dotnet run
   ```
4. **Zero Configuration Needed**: The application automatically creates `retailflow.db` and seeds 20 realistic sample products on first launch.

---

## 4. Database Structure & Entity Relationships

### 4.1 Entity Relationship Diagram (ERD)
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
│     StockQuantity (int)  │             │     PaymentMethod (str)  │
│     ReorderLevel (int)   │             │     AmountTendered (dec) │
│     CreatedAt (DateTime) │             │     ChangeDue (decimal)  │
└────────────┬─────────────┘             └────────────┬─────────────┘
             │ 1                                      │ 1
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

### 4.2 Tables Description
- **`Products`**: Stores product master data (`SKU`, `Name`, `Category`, `CostPrice`, `SellingPrice`, `StockQuantity`, `ReorderLevel`).
- **`Sales`**: Stores invoice header data (`TransactionNumber`, `SaleDate`, `Subtotal`, `Discount`, `Total`, `PaymentMethod`, `AmountTendered`, `ChangeDue`).
- **`SaleItems`**: Stores itemized line records linked to `Sales` (Cascade Delete) and `Products` (Restrict Delete).

---

## 5. Explanation of the Selected Additional Feature

### Feature: Executive Analytics Dashboard & Print Receipt

#### Why It Was Selected:
In real-world retail operations, store managers need immediate business visibility (revenue velocity, gross profit, inventory risk) without manually tallying sales receipts or opening spreadsheets. Additionally, completing a sale without a physical or digital receipt undermines customer trust.

#### How It Improves the Application:
1. **Gross Profit Visibility**: Tracks `Revenue - Cost of Goods Sold` in real-time, giving store owners a clear picture of true earnings.
2. **Stockout Prevention**: Features an **Urgent Low-Stock Items** table directly on the home screen to facilitate timely reordering.
3. **Weekly Sales Forecasting**: A 7-day sales trend visualizer monitors sales velocity day-by-day.
4. **Receipt Validation**: The **Print / PDF Receipt** dialog validates transactions with instant printing or disk backup.

---

## 6. Assumptions, Limitations & Future Improvements

### Assumptions Made:
- The currency unit is Sri Lankan Rupees (`Rs.`).
- Cashiers operate on a single terminal during a shift.
- SQLite is local to the device for maximum speed and simplicity.

### Current Limitations:
- Single-user terminal without role-based access control (Admin vs. Cashier login).
- Standalone local database without cloud synchronization.

### Improvements with More Time:
1. **Role-Based Authentication**: Cashier mode (POS only) vs. Store Manager mode (Full access + Inventory).
2. **Hardware Integration**: Direct connection to thermal ESC/POS receipt printers and barcode barcode scanners.
3. **Customer Loyalty**: Customer phone lookup to accumulate purchase points and discounts.
4. **Barcode Printing**: Generate and print SKU barcode label stickers for products.

---

## 7. Verification Test Summary

| # | Test Case | Category | Status |
|---|---|---|:---:|
| 1 | Product CRUD & Search | Products | **PASS** ☑ |
| 2 | Unique SKU & Positive Price Validation | Products | **PASS** ☑ |
| 3 | Category Filtering | Products | **PASS** ☑ |
| 4 | Real-time Stock Check (Prevents Overselling) | POS | **PASS** ☑ |
| 5 | Cash Tender & Change Due Calculation | POS | **PASS** ☑ |
| 6 | Digital Payment (Card / QR) Exact Settlement | POS | **PASS** ☑ |
| 7 | Atomic Stock Deduction (Rollback on Error) | Sales Service | **PASS** ☑ |
| 8 | Printable Receipt Generation & Export | POS / Receipt | **PASS** ☑ |
| 9 | Transaction History & Date Range Filtering | History | **PASS** ☑ |
| 10 | Dashboard KPIs, Gross Profit & 7-Day Chart | Analytics | **PASS** ☑ |
