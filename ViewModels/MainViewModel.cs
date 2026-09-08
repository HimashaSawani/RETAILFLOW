using RetailFlow.Commands;
using RetailFlow.Data;
using RetailFlow.Models;
using RetailFlow.Services;
using RetailFlow.Views;
using System;
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
    private string _activeModuleName = "Products";

    public ProductManagementViewModel ProductManagementVM { get; }
    public StockManagementViewModel StockManagementVM { get; }
    public PosViewModel PosVM { get; }

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

    public ICommand ShowProductsCommand { get; }
    public ICommand ShowStockCommand { get; }
    public ICommand ShowPosCommand { get; }

    public MainViewModel()
    {
        _context = new AppDbContext();
        _productService = new ProductService(_context);
        _stockService = new StockService(_context);
        _salesService = new SalesService(_context);

        ProductManagementVM = new ProductManagementViewModel(_productService);
        StockManagementVM = new StockManagementViewModel(_stockService);
        PosVM = new PosViewModel(_productService, _salesService);

        // Product Management Dialog delegates
        ProductManagementVM.OpenProductFormDialog = OpenProductFormDialogAsync;
        ProductManagementVM.ShowConfirmationDialog = ShowConfirmation;
        ProductManagementVM.ShowMessageDialog = ShowMessage;

        // Stock Management Dialog delegates
        StockManagementVM.OpenRestockDialog = OpenRestockDialogAsync;

        // POS Dialog delegates
        PosVM.ShowMessageDialog = ShowMessage;

        _currentView = ProductManagementVM;

        ShowProductsCommand = new RelayCommand(async () =>
        {
            CurrentView = ProductManagementVM;
            ActiveModuleName = "Products";
            await ProductManagementVM.LoadProductsAsync();
        });

        ShowStockCommand = new RelayCommand(async () =>
        {
            CurrentView = StockManagementVM;
            ActiveModuleName = "Stock";
            await StockManagementVM.LoadStockItemsAsync();
        });

        ShowPosCommand = new RelayCommand(async () =>
        {
            CurrentView = PosVM;
            ActiveModuleName = "POS";
            await PosVM.LoadProductsAsync();
        });
    }

    public async Task InitializeAsync()
    {
        // Seed initial sample products if database is empty
        if (!_context.Products.Any())
        {
            _context.Products.AddRange(
                new Product { SKU = "P001", Name = "Coca Cola", Category = "Drinks", CostPrice = 140, SellingPrice = 180, StockQuantity = 25, ReorderLevel = 10 },
                new Product { SKU = "P002", Name = "Bread", Category = "Bakery", CostPrice = 170, SellingPrice = 220, StockQuantity = 4, ReorderLevel = 10 },
                new Product { SKU = "P003", Name = "Milk Powder", Category = "Grocery", CostPrice = 800, SellingPrice = 950, StockQuantity = 0, ReorderLevel = 5 }
            );
            await _context.SaveChangesAsync();
        }

        await ProductManagementVM.LoadProductsAsync();
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
