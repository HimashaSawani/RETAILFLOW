using RetailFlow.Commands;
using RetailFlow.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RetailFlow.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly SalesService _salesService;

    private decimal _todaySales;
    private decimal _todayGrossProfit;
    private int _todayTransactions;
    private int _totalProducts;
    private int _lowStockProducts;
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    public ObservableCollection<TopProductDto> TopProducts { get; } = new();
    public ObservableCollection<DailySalesDto> WeeklySales { get; } = new();
    public ObservableCollection<RetailFlow.Models.Product> CriticalLowStockItems { get; } = new();

    public decimal TodaySales
    {
        get => _todaySales;
        set => SetProperty(ref _todaySales, value);
    }

    public decimal TodayGrossProfit
    {
        get => _todayGrossProfit;
        set => SetProperty(ref _todayGrossProfit, value);
    }

    public int TodayTransactions
    {
        get => _todayTransactions;
        set => SetProperty(ref _todayTransactions, value);
    }

    public int TotalProducts
    {
        get => _totalProducts;
        set => SetProperty(ref _totalProducts, value);
    }

    public int LowStockProducts
    {
        get => _lowStockProducts;
        set => SetProperty(ref _lowStockProducts, value);
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

    public ICommand RefreshCommand { get; }

    public DashboardViewModel(SalesService salesService)
    {
        _salesService = salesService;
        RefreshCommand = new RelayCommand(async () => await LoadDashboardDataAsync());
    }

    public async Task LoadDashboardDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading dashboard analytics...";

            var metrics = await _salesService.GetDashboardMetricsAsync();

            TodaySales = metrics.TodaySales;
            TodayGrossProfit = metrics.TodayGrossProfit;
            TodayTransactions = metrics.TodayTransactions;
            TotalProducts = metrics.TotalProducts;
            LowStockProducts = metrics.LowStockProducts;

            TopProducts.Clear();
            foreach (var item in metrics.TopProducts)
            {
                TopProducts.Add(item);
            }

            WeeklySales.Clear();
            foreach (var day in metrics.WeeklySales)
            {
                WeeklySales.Add(day);
            }

            CriticalLowStockItems.Clear();
            foreach (var item in metrics.CriticalLowStockItems)
            {
                CriticalLowStockItems.Add(item);
            }

            StatusMessage = "Dashboard updated.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load dashboard: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
