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
    private TransactionDisplayModel? _selectedTransaction;
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    public ObservableCollection<TransactionDisplayModel> Transactions { get; } = new();

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

            var list = await _salesService.GetAllSalesAsync();
            Transactions.Clear();
            foreach (var sale in list)
            {
                Transactions.Add(new TransactionDisplayModel(sale));
            }

            if (Transactions.Any() && SelectedTransaction == null)
            {
                SelectedTransaction = Transactions.First();
            }

            OnPropertyChanged(nameof(HasNoTransactions));
            StatusMessage = $"{Transactions.Count} transaction(s) found.";
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

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string query = SearchText.Trim().ToLower();
                list = list.Where(s => s.TransactionNumber.ToLower().Contains(query) ||
                                       s.SaleDate.ToString().ToLower().Contains(query) ||
                                       s.SaleItems.Any(si => si.Product.Name.ToLower().Contains(query))).ToList();
            }

            Transactions.Clear();
            foreach (var sale in list)
            {
                Transactions.Add(new TransactionDisplayModel(sale));
            }

            SelectedTransaction = Transactions.FirstOrDefault();
            OnPropertyChanged(nameof(HasNoTransactions));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search error: {ex.Message}";
        }
    }
}
