using RetailFlow.Commands;
using RetailFlow.Models;
using RetailFlow.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RetailFlow.ViewModels;

public class TransactionDisplayModel : ViewModelBase
{
    public Sale Sale { get; }

    public int Id => Sale.Id;
    public string TransactionNumber => Sale.TransactionNumber;
    public DateTime SaleDate => Sale.SaleDate;
    public string FormattedDate => Sale.SaleDate.ToString("dd/MM/yy HH:mm");
    public string FormattedFullDate => Sale.SaleDate.ToString("dd MMMM yyyy HH:mm");
    public int TotalItems => Sale.SaleItems.Sum(si => si.Quantity);
    public decimal Subtotal => Sale.Subtotal;
    public decimal Discount => Sale.Discount;
    public decimal Total => Sale.Total;
    public string PaymentMethod => string.IsNullOrWhiteSpace(Sale.PaymentMethod) ? "Cash" : Sale.PaymentMethod;
    public decimal AmountTendered => Sale.AmountTendered > 0 ? Sale.AmountTendered : Sale.Total;
    public decimal ChangeDue => Sale.ChangeDue;
    public ICollection<SaleItem> SaleItems => Sale.SaleItems;

    public TransactionDisplayModel(Sale sale)
    {
        Sale = sale;
    }
}

public class TransactionHistoryViewModel : ViewModelBase
{
    private readonly SalesService _salesService;
    private string _searchText = string.Empty;
    private string _selectedDateFilter = "All Time";
    private TransactionDisplayModel? _selectedTransaction;
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    public ObservableCollection<TransactionDisplayModel> Transactions { get; } = new();
    public ObservableCollection<string> DateFilters { get; } = new() { "All Time", "Today", "This Week" };

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = FilterTransactionsAsync();
            }
        }
    }

    public string SelectedDateFilter
    {
        get => _selectedDateFilter;
        set
        {
            if (SetProperty(ref _selectedDateFilter, value))
            {
                _ = FilterTransactionsAsync();
            }
        }
    }

    public TransactionDisplayModel? SelectedTransaction
    {
        get => _selectedTransaction;
        set
        {
            if (SetProperty(ref _selectedTransaction, value))
            {
                OnPropertyChanged(nameof(HasSelectedTransaction));
            }
        }
    }

    public bool HasSelectedTransaction => SelectedTransaction != null;

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

    public bool HasNoTransactions => !IsLoading && Transactions.Count == 0;

    public ICommand RefreshCommand { get; }

    public TransactionHistoryViewModel(SalesService salesService)
    {
        _salesService = salesService;
        RefreshCommand = new RelayCommand(async () => await LoadTransactionsAsync());
    }

    public async Task LoadTransactionsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading transaction history...";

            await FilterTransactionsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading transactions: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoTransactions));
        }
    }

    private async Task FilterTransactionsAsync()
    {
        try
        {
            var list = await _salesService.GetAllSalesAsync();

            // Apply Date Filter
            var today = DateTime.UtcNow.Date;
            if (SelectedDateFilter == "Today")
            {
                list = list.Where(s => s.SaleDate >= today && s.SaleDate < today.AddDays(1)).ToList();
            }
            else if (SelectedDateFilter == "This Week")
            {
                var startOfWeek = today.AddDays(-7);
                list = list.Where(s => s.SaleDate >= startOfWeek).ToList();
            }

            // Apply Search Query
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string query = SearchText.Trim().ToLower();
                list = list.Where(s => s.TransactionNumber.ToLower().Contains(query) ||
                                       s.SaleDate.ToString().ToLower().Contains(query) ||
                                       s.SaleItems.Any(si => si.Product != null && si.Product.Name.ToLower().Contains(query))).ToList();
            }

            Transactions.Clear();
            foreach (var sale in list)
            {
                Transactions.Add(new TransactionDisplayModel(sale));
            }

            SelectedTransaction = Transactions.FirstOrDefault();
            OnPropertyChanged(nameof(HasNoTransactions));
            StatusMessage = $"{Transactions.Count} transaction(s) displayed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search error: {ex.Message}";
        }
    }
}
