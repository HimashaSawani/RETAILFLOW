using RetailFlow.Commands;
using RetailFlow.Data;
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

    private object _currentView;
    private string _activeModuleName = "Dashboard";

    public DashboardViewModel DashboardVM { get; }
    public ProductManagementViewModel ProductManagementVM { get; }
    public PosViewModel PosVM { get; }
    public StockManagementViewModel StockManagementVM { get; }
    public TransactionHistoryViewModel TransactionHistoryVM { get; }

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

    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowProductsCommand { get; }
    public ICommand ShowPosCommand { get; }
    public ICommand ShowStockCommand { get; }
    public ICommand ShowTransactionsCommand { get; }

    public MainViewModel()
    {
        _context = new AppDbContext();
        _productService = new ProductService(_context);
        _stockService = new StockService(_context);
        _salesService = new SalesService(_context);

        DashboardVM = new DashboardViewModel(_salesService);
        ProductManagementVM = new ProductManagementViewModel(_productService);
        StockManagementVM = new StockManagementViewModel(_stockService);
        PosVM = new PosViewModel(_productService, _salesService);
        TransactionHistoryVM = new TransactionHistoryViewModel(_salesService);

        // Product Management Dialog delegates
        ProductManagementVM.OpenProductFormDialog = OpenProductFormDialogAsync;
        ProductManagementVM.ShowConfirmationDialog = ShowConfirmation;
        ProductManagementVM.ShowMessageDialog = ShowMessage;

        // Stock Management Dialog delegates
        StockManagementVM.OpenRestockDialog = OpenRestockDialogAsync;

        // POS Dialog delegates
        PosVM.OpenReceiptDialog = OpenReceiptDialog;
        PosVM.ShowMessageDialog = ShowMessage;

        _currentView = DashboardVM;

        ShowDashboardCommand = new RelayCommand(async () =>
        {
            CurrentView = DashboardVM;
            ActiveModuleName = "Dashboard";
            await DashboardVM.LoadDashboardDataAsync();
        });

        ShowProductsCommand = new RelayCommand(async () =>
        {
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
    }

    public async Task InitializeAsync()
    {
        // 1. Seed initial products if database is empty
        if (!_context.Products.Any())
        {
            var p1 = new Product { SKU = "P001", Name = "Coca Cola", Category = "Drinks", CostPrice = 140, SellingPrice = 180, StockQuantity = 25, ReorderLevel = 10 };
            var p2 = new Product { SKU = "P002", Name = "Bread", Category = "Bakery", CostPrice = 170, SellingPrice = 220, StockQuantity = 4, ReorderLevel = 10 };
            var p3 = new Product { SKU = "P003", Name = "Milk Powder", Category = "Grocery", CostPrice = 800, SellingPrice = 950, StockQuantity = 0, ReorderLevel = 5 };

            _context.Products.AddRange(p1, p2, p3);
            await _context.SaveChangesAsync();

            // 2. Seed initial sample transactions
            var sale1 = new Sale
            {
                TransactionNumber = "INV-0001",
                SaleDate = DateTime.UtcNow.AddHours(-4),
                Subtotal = 850,
                Discount = 0,
                Total = 850,
                SaleItems = new List<SaleItem>
                {
                    new SaleItem { Product = p1, Quantity = 2, UnitPrice = 180, Subtotal = 360 },
                    new SaleItem { Product = p2, Quantity = 1, UnitPrice = 220, Subtotal = 220 },
                    new SaleItem { Product = p1, Quantity = 1, UnitPrice = 180, Subtotal = 180 },
                    new SaleItem { Product = p2, Quantity = 1, UnitPrice = 90, Subtotal = 90 }
                }
            };

            var sale2 = new Sale
            {
                TransactionNumber = "INV-0002",
                SaleDate = DateTime.UtcNow.AddHours(-2),
                Subtotal = 450,
                Discount = 0,
                Total = 450,
                SaleItems = new List<SaleItem>
                {
                    new SaleItem { Product = p1, Quantity = 1, UnitPrice = 180, Subtotal = 180 },
                    new SaleItem { Product = p2, Quantity = 1, UnitPrice = 220, Subtotal = 220 },
                    new SaleItem { Product = p1, Quantity = 1, UnitPrice = 50, Subtotal = 50 }
                }
            };

            var sale3 = new Sale
            {
                TransactionNumber = "INV-0003",
                SaleDate = DateTime.UtcNow.AddMinutes(-30),
                Subtotal = 1970,
                Discount = 0,
                Total = 1970,
                SaleItems = new List<SaleItem>
                {
                    new SaleItem { Product = p1, Quantity = 2, UnitPrice = 180, Subtotal = 360 },
                    new SaleItem { Product = p2, Quantity = 3, UnitPrice = 220, Subtotal = 660 },
                    new SaleItem { Product = p3, Quantity = 1, UnitPrice = 950, Subtotal = 950 }
                }
            };

            _context.Sales.AddRange(sale1, sale2, sale3);
            await _context.SaveChangesAsync();
        }

        await DashboardVM.LoadDashboardDataAsync();
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
