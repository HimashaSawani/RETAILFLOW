using Microsoft.EntityFrameworkCore;
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
        // 20 realistic sample retail products covering all categories and stock statuses (OK, LOW, OUT)
        var sampleProducts = new List<Product>
        {
            new Product { SKU = "P001", Name = "Coca Cola 500ml", Category = "Beverages", CostPrice = 120, SellingPrice = 180, StockQuantity = 25, ReorderLevel = 5 },
            new Product { SKU = "P002", Name = "Pepsi 500ml", Category = "Beverages", CostPrice = 115, SellingPrice = 170, StockQuantity = 20, ReorderLevel = 5 },
            new Product { SKU = "P003", Name = "Sprite 500ml", Category = "Beverages", CostPrice = 115, SellingPrice = 170, StockQuantity = 18, ReorderLevel = 5 },
            new Product { SKU = "P004", Name = "Mineral Water 1L", Category = "Beverages", CostPrice = 80, SellingPrice = 120, StockQuantity = 30, ReorderLevel = 8 },
            new Product { SKU = "P005", Name = "Fresh Milk 1L", Category = "Dairy", CostPrice = 250, SellingPrice = 320, StockQuantity = 12, ReorderLevel = 5 },
            new Product { SKU = "P006", Name = "Chocolate Milk 200ml", Category = "Dairy", CostPrice = 150, SellingPrice = 200, StockQuantity = 15, ReorderLevel = 5 },
            new Product { SKU = "P007", Name = "White Bread", Category = "Bakery", CostPrice = 120, SellingPrice = 180, StockQuantity = 3, ReorderLevel = 5 }, // 🟡 LOW Stock demo
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
            new Product { SKU = "P019", Name = "Shampoo 180ml", Category = "Personal Care", CostPrice = 380, SellingPrice = 480, StockQuantity = 2, ReorderLevel = 3 }, // 🟡 LOW Stock demo
            new Product { SKU = "P020", Name = "Bath Soap 100g", Category = "Personal Care", CostPrice = 120, SellingPrice = 170, StockQuantity = 0, ReorderLevel = 6 }  // 🔴 OUT OF STOCK demo
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
