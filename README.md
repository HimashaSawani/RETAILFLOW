# RetailFlow

A modern, enterprise-grade Retail Management and Point of Sale (POS) desktop application engineered with **WPF**, **C# 12**, **.NET 8.0**, and **Entity Framework Core (SQLite)**, adhering strictly to the **MVVM (Model-View-ViewModel)** architectural pattern.

---

## 📌 Executive Summary

**RetailFlow** bridges the gap between everyday retail operations and enterprise point-of-sale environments. Designed for small-to-medium retail supermarkets and storefronts, RetailFlow combines robust inventory management, strict stock-level gating, atomic ACID database transactions, and an intuitive POS workflow with enterprise peripherals: **thermal ESC/POS receipt printing**, **cash drawer kick pulses**, **USB/HID keyboard-wedge barcode scanning**, **Code 128 sticker generation**, and a **customer loyalty rewards program**.

---

## 🚀 Key Highlights & Architectural Strengths

- **Zero-Configuration SQLite Database**: The application automatically creates, migrates, and seeds the SQLite database (`retailflow.db`) upon initial execution.
- **Strict ACID Transactions**: Every sale commit—line-item generation, stock deduction, customer points accumulation, and invoice sequencing—executes within an atomic `BeginTransactionAsync` block with automatic rollback.
- **Peripherals & Hardware Integration**: Native Win32 Spooler raw printing for 80mm/58mm thermal printers (Epson, Xprinter, POS-80), cash drawer pulse integration, and USB HID keyboard-wedge barcode scanner listeners.
- **Triple-Channel Receipt Engine**:
  1. `🖨️ ESC/POS Thermal`: Raw byte commands with automated paper cut (`GS V 66 0`) and laser-aligned 80mm ticket preview.
  2. `🖨️ Windows System Print`: Off-screen measured visual document printing compatible with Microsoft Print to PDF and office laser/inkjet printers.
  3. `📄 Digital HTML/PDF Invoice`: One-click vector invoice export that opens directly in default web browsers for immediate viewing or printing.
- **Fault-Tolerant Runtime**: Comprehensive global unhandled exception dispatchers (`AppDomain`, `DispatcherUnhandledException`, and `TaskScheduler`) ensure the application never abruptly terminates.

---

## 🔑 Demonstration & Evaluation Credentials

The application comes pre-configured with active roles and sample loyalty customers out of the box:

| Role | Username | Full Name | 4-Digit PIN | Permissions |
| :--- | :--- | :--- | :--- | :--- |
| **Store Manager** | `manager` | Store Manager | **`1234`** | Full access to Dashboard, Catalog CRUD, Barcode Printing, Restocking, POS, History, Settings |
| **Front Cashier** | `cashier` | Front Cashier | **`0000`** | POS Sales & Transaction lookup (Admin and Stock sections locked) |

### Sample Loyalty Customers (Pre-Seeded)
- **Phone**: `0771234567` — **Name**: Nimal Perera — **Loyalty Balance**: `250 pts`
- **Phone**: `0719876543` — **Name**: Sunethra Silva — **Loyalty Balance**: `120 pts`

### Sample Barcode / SKU Codes for Testing
- `P001` (Coca Cola 500ml), `P005` (Fresh Milk 1L), `P007` (White Bread), `P009` (Potato Chips), `P012` (Rice 5kg)

---

## 🌟 Core Modules

### 1. 📦 Product Catalog Management
- **Full CRUD Capabilities**: Create, view, update, and remove products with confirmation dialogs.
- **Real-Time Filtering**: Instant live search by SKU, Product Name, or Category.
- **Strict Data Integrity**: Rejects duplicate SKUs (case-insensitive), empty descriptions, and negative prices/stocks.
- **Direct Barcode Label Trigger**: Generate and print Code 128 sticker labels for any product.

### 2. 📈 Stock Inventory & Restocking
- **Dynamic Inventory Badges**:
  - `OK` (Green): Stock exceeds reorder threshold.
  - `LOW` (Amber): Stock is at or below reorder threshold.
  - `OUT` (Red): Out of stock (0 units).
- **Interactive Restock Modal**: Allows adding stock units with live mathematical preview of new inventory levels before committing to the database.

### 3. 🛒 Point of Sale (POS) & Fast Checkout
- **Instant Barcode Input & Live Preview**:
  - Top barcode bar automatically focuses on load and after every scan.
  - Typing an SKU instantly previews the product name, price, and stock in real time.
  - Pressing `Enter` or scanning via USB scanner plays a confirmation tone (`🎵 Beep`) and adds the item to the cart.
- **Active Shopping Cart Table**:
  - Columns: **SKU**, **Product Name**, **Unit Price (Rs.)**, **Quantity** with inline `+` / `-` adjustment buttons, **Line Total (Rs.)**, and a row-level `[ ✕ ]` Remove button.
  - Real-time cart item counter and one-click `[ ✕ Clear Cart ]`.
- **Tendered & Change Due Calculations**:
  - Light green cash quick-keys (`+50`, `+100`, `+500`, `+1000`, `+5000`) and `[Exact]` total button.
  - Live **Change Due** display in a clear green banner.
- **Prominent Green Checkout Action**:
  - Full-width `[ 💳 COMPLETE TRANSACTION & PAY NOW [F12] ]` button.
  - Atomically validates stock, commits the transaction, triggers receipt printing, kicks the cash drawer, and clears the cart.

### 4. 📜 Transaction History & Receipt Inspection
- **Historical Audit Trail**: View all completed sales with sequential invoices (e.g. `#INV-0001`), timestamps, cashier names, and payment channels.
- **Date Range Filtering**: Filter by presets (`All Time`, `Today`, `This Week`, `This Month`) or custom **From: [Calendar]** and **To: [Calendar]** date pickers.
- **Interactive Details & Double-Click**: Double-clicking any invoice opens the full itemized receipt breakdown with multi-channel printing options.

### 5. 📊 Sales Analytics Dashboard
- **Real-Time KPI Cards**: Today's Sales Revenue (Rs.), Total Transactions Count, Total Products in Catalog, and Urgent Low-Stock Alerts.
- **Best-Selling Products**: Ranked breakdown of top revenue generators and fast-moving items.
- **Weekly Sales Bar Chart**: Custom visual bar chart displaying sales performance across the past 7 days with pre-seeded upward trend data.

---

## 💎 Advanced Enterprise Features

### 1. 🔐 Role-Based Access Control (RBAC)
- Clean executive badge in the top bar (`👤 Store Manager [Manager]` or `👤 Front Cashier [Cashier]`).
- Cashier mode restricts inventory edits and financial reporting. Attempting to enter manager areas displays a PIN authentication challenge.
- Quick user switching in 2 seconds via the **"🔒 Switch / Lock"** header button.

### 2. 🖨️ Hardware Integration (ESC/POS & Barcode Scanner)
- **Direct Thermal ESC/POS Receipt Printing**: Native Win32 spooler raw byte communication (`winspool.drv`) supporting Epson TM-T20, Xprinter, POS-58, POS-80, or the built-in virtual spooler simulator.
- **Automatic Paper Cut & Cash Drawer Kick**: Dispatches standard ESC/POS commands (`GS V 66 0` and `ESC p`) upon checkout.
- **USB/HID Barcode Scanner Listener**: Fast keystroke burst listener (<50ms delay) with audio feedback and auto-cart addition.
- **Peripherals Configuration**: The top **"⚙️ Hardware"** button opens a configuration panel with printer discovery, drawer pulse test, and scanner test pad.

### 3. ⭐ Customer Loyalty Rewards Program
- **Customer Phone Lookup**: Cashier types phone number (e.g. `0771234567`) to fetch available points.
- **Earning Rules**: 1 Point for every Rs. 100 spent.
- **Redemption Rules**: 1 Point = Rs. 1.00 instant discount on the current bill.
- **Quick Registration**: 3-second customer modal to sign up new shoppers without leaving the POS.

### 4. 🏷️ Barcode Printing (Code 128 Sticker Labels)
- Pure native C# vector Code 128 (Subset B) barcode generator.
- Select any product in Product Management and click **"🏷️ Barcode Labels"** to generate sticker labels with custom quantities or matching stock levels.

---

## 🛠️ Technology Stack

| Component | Technology |
| :--- | :--- |
| **Language** | C# 12 |
| **Framework** | .NET 8.0 (Windows Desktop / WPF) |
| **Architecture** | MVVM (Model-View-ViewModel) with XAML data binding |
| **Database** | SQLite via Entity Framework Core 8.0 |
| **Printing & Hardware** | Native Win32 Spooler API (`winspool.drv`), `System.Printing`, ESC/POS |
| **Barcode Engine** | Native C# Code 128 Vector Renderer (`System.Windows.Media.DrawingVisual`) |

---

## 📂 Project Structure

```
RetailFlow/
│
├── Commands/
│   └── RelayCommand.cs                 # ICommand implementation for MVVM command binding
│
├── Data/
│   └── AppDbContext.cs                 # SQLite DbContext & Entity Framework mapping
│
├── Helpers/
│   ├── BarcodeScannerListener.cs       # USB HID keyboard wedge fast keystroke listener
│   └── BarHeightConverter.cs           # Value converter for dynamic dashboard bar heights
│
├── Models/
│   ├── Customer.cs                     # Customer loyalty entity (Phone, Points, TotalSpent)
│   ├── Product.cs                      # Product inventory entity (SKU, Prices, Stock)
│   ├── Sale.cs                         # Sale header entity (Invoice, Date, Totals, Loyalty)
│   ├── SaleItem.cs                     # Sale line-item entity linking sales to products
│   ├── SaleReceipt.cs                  # Receipt presentation & data transfer object
│   └── User.cs                         # Staff account entity (Role, PIN, Status)
│
├── Services/
│   ├── AuthService.cs                  # Session management & PIN-based authentication
│   ├── BarcodeService.cs               # Native Code 128 vector barcode & label engine
│   ├── CustomerService.cs              # Loyalty customer lookup & registration
│   ├── EscPosPrinterService.cs         # Thermal ESC/POS raw spooler & cash drawer engine
│   ├── ProductService.cs               # Product CRUD, duplicate SKU checking, search
│   ├── SalesService.cs                 # ACID sales transactions & analytics queries
│   └── StockService.cs                 # Inventory status calculations & restock processing
│
├── ViewModels/
│   ├── ViewModelBase.cs                # INotifyPropertyChanged base implementation
│   ├── MainViewModel.cs                # Main shell, navigation coordinator, and seeding
│   ├── DashboardViewModel.cs           # KPI metrics, top products, and weekly chart logic
│   ├── PosViewModel.cs                 # POS cart, barcode scanner, loyalty, and checkout
│   ├── CartItemViewModel.cs            # Cart line-item model with quantity mutations
│   ├── ProductManagementViewModel.cs   # Product CRUD & filtering logic
│   ├── ProductFormViewModel.cs         # Add/Edit product form state & validation
│   ├── StockManagementViewModel.cs     # Inventory stock status table & restock triggers
│   ├── StockItemViewModel.cs           # Dynamic stock status badge calculator
│   ├── RestockDialogViewModel.cs       # Restock quantity calculation & execution
│   ├── TransactionHistoryViewModel.cs  # Transaction history, search & date range filters
│   └── ReceiptDialogViewModel.cs      # Post-checkout receipt dialog coordinator
│
├── Views/
│   ├── BarcodePrintDialog.xaml / .cs   # Code 128 label sticker preview & printer dialog
│   ├── DashboardView.xaml / .cs        # Sales intelligence dashboard screen
│   ├── HardwareSettingsDialog.xaml/.cs # Peripherals configuration (Printer, Drawer, Scanner)
│   ├── LoginDialog.xaml / .cs          # 4-digit PIN authentication pad dialog
│   ├── PosView.xaml / .cs              # Point of Sale (POS) screen
│   ├── ProductFormDialog.xaml / .cs    # Add/Edit product modal dialog
│   ├── ProductManagementView.xaml / .cs# Product catalog management screen
│   ├── QuickCustomerDialog.xaml / .cs  # 3-second customer loyalty signup dialog
│   ├── ReceiptDialog.xaml / .cs        # Post-checkout multi-channel receipt dialog
│   ├── RestockDialog.xaml / .cs        # Stock restocking modal dialog
│   ├── ThermalTicketPreviewDialog.xaml # Monospaced 80mm thermal receipt roll simulator
│   └── TransactionHistoryView.xaml/.cs # Transaction history audit log & date pickers
│
├── App.xaml / App.xaml.cs              # Application entry point, crash handlers & DB init
├── MainWindow.xaml / .cs               # Main shell window with sidebar navigation & badges
├── RetailFlow.csproj                   # Project configuration (.NET 8 WPF)
└── README.md                           # Master documentation
```

---

## 🚀 Getting Started & Execution

### Prerequisites
- **Operating System**: Windows 10 (Build 19041+) or Windows 11
- **Runtime / SDK**: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Running Locally
1. Clone or extract the repository into your working directory.
2. Open a terminal in the project directory (`D:\RETAILFLOW`) and run:
   ```powershell
   dotnet run
   ```
3. The application will restore packages, compile, verify the local SQLite database, auto-seed demo data, and launch immediately.

---

## 🔒 Reliability & Exception Handling

<<<<<<< HEAD
- **Crash-Proof Application Core**: Global exception handlers in `App.xaml.cs` catch all runtime exceptions gracefully across `DispatcherUnhandledException`, `AppDomain.UnhandledException`, and `TaskScheduler.UnobservedTaskException`, displaying clear notification dialogs rather than crashing.
- **Concurrency & Concurrency Protection**: Database writes are atomic. If a product has only 2 units available and a cashier attempts to sell 3, the inventory guard blocks the transaction before database submission.
- **Spooler Fallbacks**: If a physical thermal printer is powered off or unplugged, raw spooler exceptions are captured gracefully, falling back to the virtual thermal simulator preview without interrupting the POS sale flow.
=======
The **Sales Dashboard** provides immediate business intelligence to retail managers:
1. **Real-time KPI Tiles**: Today's Revenue, Transaction Count, Total Catalog Items, and Urgent Low-Stock Alerts.
2. **Top Selling Products**: Dynamically aggregates units sold and revenue from all transaction line items.
3. **Weekly Sales Bar Chart**: A lightweight XAML chart displaying sales volume over the last 7 days with weekday headers.

---

## Screenshots

### 1. Main Navigation & Dashboard (Cashier)
<img width="1532" height="973" alt="image" src="https://github.com/user-attachments/assets/47c302b0-d41e-4eee-a43f-f69c9b51c6c9" />


### 2. Main Navigation & Dashboard (Store Manage)

<img width="1533" height="967" alt="image" src="https://github.com/user-attachments/assets/9b5113a8-83d7-4f9b-8a4f-b77a195aa1e9" />


---

## Error Handling & Reliability

- **Never-Crash Architecture**: Global unhandled exception handlers (`DispatcherUnhandledException`, `AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`) in `App.xaml.cs` catch all runtime exceptions gracefully and display helpful alerts rather than crashing.
- **ACID Reliability**: Database operations utilize transactions with automatic rollback on error.
>>>>>>> ef50f0cab76357520f56a81dc7c2f1be4f38198d
