using RetailFlow.Commands;
using RetailFlow.Models;
using RetailFlow.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RetailFlow.ViewModels;

public class StockManagementViewModel : ViewModelBase
{
    private readonly StockService _stockService;
    private StockItemViewModel? _selectedStockItem;
    private string _searchText = string.Empty;
    private string _selectedFilter = "All";
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    public ObservableCollection<StockItemViewModel> StockItems { get; } = new();

    public StockItemViewModel? SelectedStockItem
    {
        get => _selectedStockItem;
        set => SetProperty(ref _selectedStockItem, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = LoadStockItemsAsync();
            }
        }
    }

    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value))
            {
                _ = LoadStockItemsAsync();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand RestockCommand { get; }
    public ICommand RefreshCommand { get; }

    public Func<Product, Task<bool>>? OpenRestockDialog { get; set; }

    public StockManagementViewModel(StockService stockService)
    {
        _stockService = stockService;

        RestockCommand = new RelayCommand(async () => await ExecuteRestockAsync(), () => SelectedStockItem != null);
        RefreshCommand = new RelayCommand(async () => await LoadStockItemsAsync());
    }

    public async Task LoadStockItemsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading stock inventory...";

            var products = await _stockService.GetAllStockProductsAsync();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string query = SearchText.Trim().ToLower();
                products = products.Where(p => p.Name.ToLower().Contains(query) || p.SKU.ToLower().Contains(query)).ToList();
            }

            if (SelectedFilter == "Low Stock")
            {
                products = products.Where(p => p.StockQuantity <= p.ReorderLevel && p.StockQuantity > 0).ToList();
            }
            else if (SelectedFilter == "Out of Stock")
            {
                products = products.Where(p => p.StockQuantity == 0).ToList();
            }

            StockItems.Clear();
            foreach (var product in products)
            {
                StockItems.Add(new StockItemViewModel(product));
            }

            int lowCount = StockItems.Count(s => s.Status == "LOW");
            int outCount = StockItems.Count(s => s.Status == "OUT");
            StatusMessage = $"Total: {StockItems.Count} items | Low Stock: {lowCount} | Out of Stock: {outCount}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading stock: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteRestockAsync()
    {
        if (SelectedStockItem == null || OpenRestockDialog == null)
            return;

        bool restocked = await OpenRestockDialog.Invoke(SelectedStockItem.Product);
        if (restocked)
        {
            await LoadStockItemsAsync();
            StatusMessage = $"Successfully restocked '{SelectedStockItem.Name}'! ✔";
        }
    }
}
