using RetailFlow.Commands;
using RetailFlow.Models;
using RetailFlow.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RetailFlow.ViewModels;

public class RestockDialogViewModel : ViewModelBase
{
    private readonly StockService _stockService;
    private readonly Product _product;

    private int _quantityToAdd = 1;
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isSaving;

    public event Action<bool>? RequestClose;

    public string ProductName => _product.Name;
    public string SKU => _product.SKU;
    public int CurrentStock => _product.StockQuantity;

    public int QuantityToAdd
    {
        get => _quantityToAdd;
        set
        {
            if (SetProperty(ref _quantityToAdd, value))
            {
                OnPropertyChanged(nameof(NewStock));
                ClearError();
            }
        }
    }

    public int NewStock => CurrentStock + Math.Max(0, QuantityToAdd);

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            SetProperty(ref _errorMessage, value);
            HasError = !string.IsNullOrWhiteSpace(value);
        }
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        set => SetProperty(ref _isSaving, value);
    }

    public ICommand RestockCommand { get; }
    public ICommand CancelCommand { get; }

    public RestockDialogViewModel(StockService stockService, Product product)
    {
        _stockService = stockService;
        _product = product;

        RestockCommand = new RelayCommand(async () => await ExecuteRestockAsync(), () => !IsSaving);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false));
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
    }

    private async Task ExecuteRestockAsync()
    {
        if (QuantityToAdd <= 0)
        {
            ErrorMessage = "Quantity to add must be greater than zero. ❌";
            return;
        }

        try
        {
            IsSaving = true;
            bool success = await _stockService.RestockProductAsync(_product.Id, QuantityToAdd);
            if (success)
            {
                RequestClose?.Invoke(true);
            }
            else
            {
                ErrorMessage = "Failed to restock product. Product not found.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error during restock: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }
}
