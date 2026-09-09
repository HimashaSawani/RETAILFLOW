using Microsoft.EntityFrameworkCore;
using RetailFlow.Commands;
using RetailFlow.Data;
using RetailFlow.Helpers;
using RetailFlow.Models;
using RetailFlow.Services;
using RetailFlow.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RetailFlow.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly ProductService _productService;
    private readonly StockService _stockService;
    private readonly SalesService _salesService;
    private readonly AuthService _authService;
    private readonly CustomerService _customerService;
    private readonly EscPosPrinterService _printerService;
    private readonly BarcodeService _barcodeService;
    private readonly BarcodeScannerListener _scannerListener;

    private object _currentView;
    private string _activeModuleName = "Dashboard";

    public DashboardViewModel DashboardVM { get; }
    public ProductManagementViewModel ProductManagementVM { get; }
    public PosViewModel PosVM { get; }
    public StockManagementViewModel StockManagementVM { get; }
    public TransactionHistoryViewModel TransactionHistoryVM { get; }

    public AuthService AuthService => _authService;
    public EscPosPrinterService PrinterService => _printerService;
    public BarcodeService BarcodeService => _barcodeService;
    public BarcodeScannerListener ScannerListener => _scannerListener;

    public object CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public string ActiveModuleName
    {
        get => _activeModuleName;
        set => SetProperty(ref _activeModuleName, value);
    }

    public bool IsManager => _authService.IsManager;
    public bool IsCashier => _authService.IsCashier;

    public string UserRoleText => _authService.CurrentUser != null
        ? $"{_authService.CurrentUser.FullName} [{(_authService.IsManager ? "Manager" : "Cashier")}]"
        : "Not Logged In";

    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowProductsCommand { get; }
    public ICommand ShowPosCommand { get; }
    public ICommand ShowStockCommand { get; }
    public ICommand ShowTransactionsCommand { get; }
    public ICommand SwitchUserCommand { get; }
    public ICommand OpenHardwareSettingsCommand { get; }

    public MainViewModel()
    {
        _context = new AppDbContext();
        _productService = new ProductService(_context);
        _stockService = new StockService(_context);
        _salesService = new SalesService(_context);
        _authService = new AuthService(_context);
        _customerService = new CustomerService(_context);
        _printerService = new EscPosPrinterService();
        _barcodeService = new BarcodeService();
        _scannerListener = new BarcodeScannerListener();

        DashboardVM = new DashboardViewModel(_salesService);
        ProductManagementVM = new ProductManagementViewModel(_productService);
        StockManagementVM = new StockManagementViewModel(_stockService);
        PosVM = new PosViewModel(_productService, _salesService, _customerService, _authService, _printerService);
        TransactionHistoryVM = new TransactionHistoryViewModel(_salesService);

        // Product Management Dialog delegates
        ProductManagementVM.OpenProductFormDialog = OpenProductFormDialogAsync;
        ProductManagementVM.OpenBarcodePrintDialog = OpenBarcodePrintDialog;
        ProductManagementVM.ShowConfirmationDialog = ShowConfirmation;
        ProductManagementVM.ShowMessageDialog = ShowMessage;

        // Stock Management Dialog delegates
        StockManagementVM.OpenRestockDialog = OpenRestockDialogAsync;

        // POS Dialog delegates
        PosVM.OpenReceiptDialog = OpenReceiptDialog;
        PosVM.OpenQuickCustomerDialog = OpenQuickCustomerDialogAsync;
        PosVM.ShowMessageDialog = ShowMessage;

        // Transaction History Dialog delegates
        TransactionHistoryVM.OpenReceiptDetailsDialog = OpenReceiptDialog;

        _currentView = DashboardVM;

        ShowDashboardCommand = new RelayCommand(async () =>
        {
            if (!EnsureManagerRole("Dashboard Analytics")) return;
            CurrentView = DashboardVM;
            ActiveModuleName = "Dashboard";
            await DashboardVM.LoadDashboardDataAsync();
        });

        ShowProductsCommand = new RelayCommand(async () =>
        {
            if (!EnsureManagerRole("Product Management")) return;
            CurrentView = ProductManagementVM;
            ActiveModuleName = "Products";
            await ProductManagementVM.LoadProductsAsync();
        });

        ShowPosCommand = new RelayCommand(async () =>
        {
            CurrentView = PosVM;
            ActiveModuleName = "Sales";
            await PosVM.LoadProductsAsync();
        });

        ShowStockCommand = new RelayCommand(async () =>
        {
            if (!EnsureManagerRole("Stock Inventory")) return;
            CurrentView = StockManagementVM;
            ActiveModuleName = "Stock";
            await StockManagementVM.LoadStockItemsAsync();
        });

        ShowTransactionsCommand = new RelayCommand(async () =>
        {
            CurrentView = TransactionHistoryVM;
            ActiveModuleName = "Transactions";
            await TransactionHistoryVM.LoadTransactionsAsync();
        });

        SwitchUserCommand = new RelayCommand(OpenSwitchUserDialog);
        OpenHardwareSettingsCommand = new RelayCommand(OpenHardwareSettingsDialog);

        // Auth state changes
        _authService.CurrentUserChanged += user =>
        {
            OnPropertyChanged(nameof(IsManager));
            OnPropertyChanged(nameof(IsCashier));
            OnPropertyChanged(nameof(UserRoleText));

            // If Cashier logs in, redirect away from manager-only screens to POS
            if (_authService.IsCashier && (CurrentView == DashboardVM || CurrentView == ProductManagementVM || CurrentView == StockManagementVM))
            {
                ShowPosCommand.Execute(null);
            }
        };

        // Connect global barcode scanner to POS view model
        _scannerListener.BarcodeScanned += code =>
        {
            if (CurrentView != PosVM)
            {
                // Switch to POS and scan
                ShowPosCommand.Execute(null);
            }
            PosVM.HandleBarcodeScanned(code);
        };
    }

    public async Task InitializeAsync()
    {
        // Default initial session: Store Manager (PIN: 1234)
        await _authService.AuthenticateByPinAsync("1234");

        // 20 realistic sample retail products covering all categories and stock statuses (OK, LOW, OUT)
        var sampleProducts = new List<Product>
        {
            new Product { SKU = "P001", Name = "Coca Cola 500ml", Category = "Beverages", CostPrice = 120, SellingPrice = 180, StockQuantity = 25, ReorderLevel = 5 },
            new Product { SKU = "P002", Name = "Pepsi 500ml", Category = "Beverages", CostPrice = 115, SellingPrice = 170, StockQuantity = 20, ReorderLevel = 5 },
            new Product { SKU = "P003", Name = "Sprite 500ml", Category = "Beverages", CostPrice = 115, SellingPrice = 170, StockQuantity = 18, ReorderLevel = 5 },
            new Product { SKU = "P004", Name = "Mineral Water 1L", Category = "Beverages", CostPrice = 80, SellingPrice = 120, StockQuantity = 30, ReorderLevel = 8 },
            new Product { SKU = "P005", Name = "Fresh Milk 1L", Category = "Dairy", CostPrice = 250, SellingPrice = 320, StockQuantity = 12, ReorderLevel = 5 },
            new Product { SKU = "P006", Name = "Chocolate Milk 200ml", Category = "Dairy", CostPrice = 150, SellingPrice = 200, StockQuantity = 15, ReorderLevel = 5 },
            new Product { SKU = "P007", Name = "White Bread", Category = "Bakery", CostPrice = 120, SellingPrice = 180, StockQuantity = 3, ReorderLevel = 5 },
            new Product { SKU = "P008", Name = "Chocolate Cream Biscuit", Category = "Snacks", CostPrice = 100, SellingPrice = 150, StockQuantity = 35, ReorderLevel = 10 },
            new Product { SKU = "P009", Name = "Potato Chips", Category = "Snacks", CostPrice = 180, SellingPrice = 250, StockQuantity = 22, ReorderLevel = 5 },
            new Product { SKU = "P010", Name = "Chocolate Bar", Category = "Snacks", CostPrice = 130, SellingPrice = 200, StockQuantity = 28, ReorderLevel = 8 },
            new Product { SKU = "P011", Name = "White Sugar 1kg", Category = "Grocery", CostPrice = 220, SellingPrice = 270, StockQuantity = 16, ReorderLevel = 5 },
            new Product { SKU = "P012", Name = "Rice 5kg", Category = "Grocery", CostPrice = 1000, SellingPrice = 1200, StockQuantity = 10, ReorderLevel = 3 },
            new Product { SKU = "P013", Name = "Wheat Flour 1kg", Category = "Grocery", CostPrice = 180, SellingPrice = 230, StockQuantity = 14, ReorderLevel = 5 },
            new Product { SKU = "P014", Name = "Cooking Oil 1L", Category = "Grocery", CostPrice = 450, SellingPrice = 550, StockQuantity = 9, ReorderLevel = 4 },
            new Product { SKU = "P015", Name = "Tea 100g", Category = "Grocery", CostPrice = 280, SellingPrice = 350, StockQuantity = 20, ReorderLevel = 5 },
            new Product { SKU = "P016", Name = "Washing Powder 1kg", Category = "Household", CostPrice = 450, SellingPrice = 550, StockQuantity = 7, ReorderLevel = 3 },
            new Product { SKU = "P017", Name = "Dishwashing Liquid 500ml", Category = "Household", CostPrice = 220, SellingPrice = 300, StockQuantity = 11, ReorderLevel = 4 },
            new Product { SKU = "P018", Name = "Toothpaste 120g", Category = "Personal Care", CostPrice = 250, SellingPrice = 330, StockQuantity = 13, ReorderLevel = 5 },
            new Product { SKU = "P019", Name = "Shampoo 180ml", Category = "Personal Care", CostPrice = 380, SellingPrice = 480, StockQuantity = 2, ReorderLevel = 3 },
            new Product { SKU = "P020", Name = "Bath Soap 100g", Category = "Personal Care", CostPrice = 120, SellingPrice = 170, StockQuantity = 0, ReorderLevel = 6 }
        };

        foreach (var sample in sampleProducts)
        {
            var existing = await _context.Products.FirstOrDefaultAsync(p => p.SKU == sample.SKU);
            if (existing == null)
            {
                _context.Products.Add(sample);
            }
            else
            {
                existing.Name = sample.Name;
                existing.Category = sample.Category;
                existing.CostPrice = sample.CostPrice;
                existing.SellingPrice = sample.SellingPrice;
                existing.StockQuantity = sample.StockQuantity;
                existing.ReorderLevel = sample.ReorderLevel;
            }
        }
        await _context.SaveChangesAsync();

        // Seed 7-day realistic sales if empty to populate the Dashboard chart & transaction history
        if (!await _context.Sales.AnyAsync() && await _context.Products.AnyAsync())
        {
            var allProds = await _context.Products.ToListAsync();
            var p1 = allProds.FirstOrDefault(p => p.SKU == "P001") ?? allProds[0];
            var p2 = allProds.FirstOrDefault(p => p.SKU == "P005") ?? allProds[Math.Min(1, allProds.Count - 1)];
            var p3 = allProds.FirstOrDefault(p => p.SKU == "P007") ?? allProds[Math.Min(2, allProds.Count - 1)];
            var p4 = allProds.FirstOrDefault(p => p.SKU == "P009") ?? allProds[Math.Min(3, allProds.Count - 1)];
            var p5 = allProds.FirstOrDefault(p => p.SKU == "P011") ?? allProds[Math.Min(4, allProds.Count - 1)];
            var p6 = allProds.FirstOrDefault(p => p.SKU == "P012") ?? allProds[Math.Min(5, allProds.Count - 1)];
            var p7 = allProds.FirstOrDefault(p => p.SKU == "P014") ?? allProds[Math.Min(6, allProds.Count - 1)];

            var now = DateTime.UtcNow;

            // Day -6
            var s1 = new Sale { TransactionNumber = "INV-0001", SaleDate = now.AddDays(-6).AddHours(2), Subtotal = 1450, Discount = 50, Total = 1400, PaymentMethod = "Cash", AmountTendered = 1500, ChangeDue = 100 };
            s1.SaleItems.Add(new SaleItem { ProductId = p1.Id, Quantity = 4, UnitPrice = p1.SellingPrice, Subtotal = 4 * p1.SellingPrice });
            s1.SaleItems.Add(new SaleItem { ProductId = p3.Id, Quantity = 2, UnitPrice = p3.SellingPrice, Subtotal = 2 * p3.SellingPrice });
            s1.SaleItems.Add(new SaleItem { ProductId = p4.Id, Quantity = 1, UnitPrice = p4.SellingPrice, Subtotal = 1 * p4.SellingPrice });

            // Day -5
            var s2 = new Sale { TransactionNumber = "INV-0002", SaleDate = now.AddDays(-5).AddHours(3), Subtotal = 2670, Discount = 0, Total = 2670, PaymentMethod = "Card", AmountTendered = 2670, ChangeDue = 0 };
            s2.SaleItems.Add(new SaleItem { ProductId = p6.Id, Quantity = 2, UnitPrice = p6.SellingPrice, Subtotal = 2 * p6.SellingPrice });
            s2.SaleItems.Add(new SaleItem { ProductId = p5.Id, Quantity = 1, UnitPrice = p5.SellingPrice, Subtotal = 1 * p5.SellingPrice });

            // Day -4
            var s3 = new Sale { TransactionNumber = "INV-0003", SaleDate = now.AddDays(-4).AddHours(1), Subtotal = 1820, Discount = 20, Total = 1800, PaymentMethod = "Cash", AmountTendered = 2000, ChangeDue = 200 };
            s3.SaleItems.Add(new SaleItem { ProductId = p2.Id, Quantity = 3, UnitPrice = p2.SellingPrice, Subtotal = 3 * p2.SellingPrice });
            s3.SaleItems.Add(new SaleItem { ProductId = p4.Id, Quantity = 2, UnitPrice = p4.SellingPrice, Subtotal = 2 * p4.SellingPrice });
            s3.SaleItems.Add(new SaleItem { ProductId = p1.Id, Quantity = 2, UnitPrice = p1.SellingPrice, Subtotal = 2 * p1.SellingPrice });

            // Day -3
            var s4 = new Sale { TransactionNumber = "INV-0004", SaleDate = now.AddDays(-3).AddHours(4), Subtotal = 3680, Discount = 80, Total = 3600, PaymentMethod = "Cash", AmountTendered = 4000, ChangeDue = 400 };
            s4.SaleItems.Add(new SaleItem { ProductId = p6.Id, Quantity = 2, UnitPrice = p6.SellingPrice, Subtotal = 2 * p6.SellingPrice });
            s4.SaleItems.Add(new SaleItem { ProductId = p7.Id, Quantity = 2, UnitPrice = p7.SellingPrice, Subtotal = 2 * p7.SellingPrice });
            s4.SaleItems.Add(new SaleItem { ProductId = p3.Id, Quantity = 1, UnitPrice = p3.SellingPrice, Subtotal = 1 * p3.SellingPrice });

            // Day -2
            var s5 = new Sale { TransactionNumber = "INV-0005", SaleDate = now.AddDays(-2).AddHours(2), Subtotal = 2290, Discount = 40, Total = 2250, PaymentMethod = "Card", AmountTendered = 2250, ChangeDue = 0 };
            s5.SaleItems.Add(new SaleItem { ProductId = p1.Id, Quantity = 5, UnitPrice = p1.SellingPrice, Subtotal = 5 * p1.SellingPrice });
            s5.SaleItems.Add(new SaleItem { ProductId = p2.Id, Quantity = 2, UnitPrice = p2.SellingPrice, Subtotal = 2 * p2.SellingPrice });
            s5.SaleItems.Add(new SaleItem { ProductId = p4.Id, Quantity = 3, UnitPrice = p4.SellingPrice, Subtotal = 3 * p4.SellingPrice });

            // Day -1
            var s6 = new Sale { TransactionNumber = "INV-0006", SaleDate = now.AddDays(-1).AddHours(5), Subtotal = 4140, Discount = 140, Total = 4000, PaymentMethod = "Cash", AmountTendered = 5000, ChangeDue = 1000 };
            s6.SaleItems.Add(new SaleItem { ProductId = p6.Id, Quantity = 3, UnitPrice = p6.SellingPrice, Subtotal = 3 * p6.SellingPrice });
            s6.SaleItems.Add(new SaleItem { ProductId = p1.Id, Quantity = 3, UnitPrice = p1.SellingPrice, Subtotal = 3 * p1.SellingPrice });

            // Today - Sale 1
            var s7 = new Sale { TransactionNumber = "INV-0007", SaleDate = now.AddHours(-3), Subtotal = 1750, Discount = 50, Total = 1700, PaymentMethod = "Cash", AmountTendered = 2000, ChangeDue = 300, CustomerName = "Nimal Perera", CustomerPhone = "0771234567", LoyaltyPointsEarned = 17 };
            s7.SaleItems.Add(new SaleItem { ProductId = p2.Id, Quantity = 2, UnitPrice = p2.SellingPrice, Subtotal = 2 * p2.SellingPrice });
            s7.SaleItems.Add(new SaleItem { ProductId = p4.Id, Quantity = 3, UnitPrice = p4.SellingPrice, Subtotal = 3 * p4.SellingPrice });
            s7.SaleItems.Add(new SaleItem { ProductId = p3.Id, Quantity = 2, UnitPrice = p3.SellingPrice, Subtotal = 2 * p3.SellingPrice });

            // Today - Sale 2
            var s8 = new Sale { TransactionNumber = "INV-0008", SaleDate = now.AddHours(-1), Subtotal = 3130, Discount = 30, Total = 3100, PaymentMethod = "QR / Transfer", AmountTendered = 3100, ChangeDue = 0, CustomerName = "Sunethra Silva", CustomerPhone = "0719876543", LoyaltyPointsEarned = 31 };
            s8.SaleItems.Add(new SaleItem { ProductId = p6.Id, Quantity = 2, UnitPrice = p6.SellingPrice, Subtotal = 2 * p6.SellingPrice });
            s8.SaleItems.Add(new SaleItem { ProductId = p7.Id, Quantity = 1, UnitPrice = p7.SellingPrice, Subtotal = 1 * p7.SellingPrice });
            s8.SaleItems.Add(new SaleItem { ProductId = p1.Id, Quantity = 1, UnitPrice = p1.SellingPrice, Subtotal = 1 * p1.SellingPrice });

            _context.Sales.AddRange(s1, s2, s3, s4, s5, s6, s7, s8);
            await _context.SaveChangesAsync();
        }

        await DashboardVM.LoadDashboardDataAsync();
    }

    private bool EnsureManagerRole(string featureName)
    {
        if (IsManager)
            return true;

        var result = MessageBox.Show(
            $"🔒 Manager Access Required\n\nAccess to '{featureName}' is restricted to Store Managers.\nWould you like to authenticate with a Manager PIN now?",
            "Access Restricted",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            OpenSwitchUserDialog();
            return IsManager;
        }

        return false;
    }

    private void OpenSwitchUserDialog()
    {
        var loginDialog = new LoginDialog(_authService)
        {
            Owner = Application.Current.MainWindow
        };

        loginDialog.ShowDialog();
    }

    private void OpenHardwareSettingsDialog()
    {
        var hwDialog = new HardwareSettingsDialog(_printerService)
        {
            Owner = Application.Current.MainWindow
        };

        hwDialog.ShowDialog();
    }

    private void OpenBarcodePrintDialog(Product product)
    {
        var barcodeDlg = new BarcodePrintDialog(_barcodeService, product)
        {
            Owner = Application.Current.MainWindow
        };

        barcodeDlg.ShowDialog();
    }

    private Task<Customer?> OpenQuickCustomerDialogAsync(string initialPhone)
    {
        var dialog = new QuickCustomerDialog(_customerService, initialPhone)
        {
            Owner = Application.Current.MainWindow
        };

        bool? result = dialog.ShowDialog();
        if (result == true)
        {
            return Task.FromResult<Customer?>(dialog.RegisteredCustomer);
        }

        return Task.FromResult<Customer?>(null);
    }

    private Task<bool> OpenProductFormDialogAsync(Product? productToEdit)
    {
        var formVM = new ProductFormViewModel(_productService, productToEdit);
        var dialog = new ProductFormDialog(formVM)
        {
            Owner = Application.Current.MainWindow
        };

        bool result = dialog.ShowDialog() ?? false;
        return Task.FromResult(result);
    }

    private Task<bool> OpenRestockDialogAsync(Product product)
    {
        var restockVM = new RestockDialogViewModel(_stockService, product);
        var dialog = new RestockDialog(restockVM)
        {
            Owner = Application.Current.MainWindow
        };

        bool result = dialog.ShowDialog() ?? false;
        return Task.FromResult(result);
    }

    private void OpenReceiptDialog(SaleReceipt receipt)
    {
        var receiptVM = new ReceiptDialogViewModel(receipt);
        var dialog = new ReceiptDialog(receiptVM)
        {
            Owner = Application.Current.MainWindow
        };

        dialog.ShowDialog();
    }

    private bool ShowConfirmation(string message, string title)
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    private void ShowMessage(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
