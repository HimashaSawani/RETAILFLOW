using RetailFlow.Commands;
using RetailFlow.Models;
using RetailFlow.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RetailFlow.ViewModels;

public class PosViewModel : ViewModelBase
{
    private readonly ProductService _productService;
    private readonly SalesService _salesService;

    private Product? _selectedProduct;
    private int _quantityInput = 1;
    private decimal _discount;
    private string _validationMessage = string.Empty;
    private bool _hasValidationError;
    private string _successMessage = string.Empty;
    private bool _hasSuccessMessage;
    private bool _isProcessingSale;
    private CartItemViewModel? _selectedCartItem;

    private string _selectedPaymentMethod = "Cash";
    private decimal _amountTendered;
    private decimal _changeDue;

    public ObservableCollection<string> PaymentMethods { get; } = new() { "Cash", "Card", "QR / Transfer" };

    public ObservableCollection<Product> AvailableProducts { get; } = new();
    public ObservableCollection<CartItemViewModel> CartItems { get; } = new();

    public string SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set
        {
            if (SetProperty(ref _selectedPaymentMethod, value))
            {
                OnPropertyChanged(nameof(IsCashPayment));
                RecalculateChange();
            }
        }
    }

    public bool IsCashPayment => SelectedPaymentMethod == "Cash";

    public decimal AmountTendered
    {
        get => _amountTendered;
        set
        {
            if (value < 0) value = 0;
            if (SetProperty(ref _amountTendered, value))
            {
                RecalculateChange();
            }
        }
    }

    public decimal ChangeDue
    {
        get => _changeDue;
        private set => SetProperty(ref _changeDue, value);
    }

    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                ClearValidation();
                ValidateStockAvailability();
            }
        }
    }

    public int QuantityInput
    {
        get => _quantityInput;
        set
        {
            if (SetProperty(ref _quantityInput, value))
            {
                ClearValidation();
                ValidateStockAvailability();
            }
        }
    }

    public decimal Discount
    {
        get => _discount;
        set
        {
            if (value < 0)
                value = 0;

            if (SetProperty(ref _discount, value))
            {
                RecalculateTotals();
            }
        }
    }

    public CartItemViewModel? SelectedCartItem
    {
        get => _selectedCartItem;
        set => SetProperty(ref _selectedCartItem, value);
    }

    public decimal Subtotal => CartItems.Sum(item => item.Total);

    public decimal Total => Math.Max(0, Subtotal - Discount);

    public string ValidationMessage
    {
        get => _validationMessage;
        set
        {
            SetProperty(ref _validationMessage, value);
            HasValidationError = !string.IsNullOrWhiteSpace(value);
        }
    }

    public bool HasValidationError
    {
        get => _hasValidationError;
        set => SetProperty(ref _hasValidationError, value);
    }

    public string SuccessMessage
    {
        get => _successMessage;
        set
        {
            SetProperty(ref _successMessage, value);
            HasSuccessMessage = !string.IsNullOrWhiteSpace(value);
        }
    }

    public bool HasSuccessMessage
    {
        get => _hasSuccessMessage;
        set => SetProperty(ref _hasSuccessMessage, value);
    }

    public bool IsProcessingSale
    {
        get => _isProcessingSale;
        set => SetProperty(ref _isProcessingSale, value);
    }

    public ICommand AddToCartCommand { get; }
    public ICommand RemoveFromCartCommand { get; }
    public ICommand ClearCartCommand { get; }
    public ICommand CompleteSaleCommand { get; }
    public ICommand RefreshProductsCommand { get; }
    public ICommand SetExactTenderCommand { get; }
    public ICommand AddTenderPresetCommand { get; }

    public Action<SaleReceipt>? OpenReceiptDialog { get; set; }
    public Action<string, string>? ShowMessageDialog { get; set; }

    public PosViewModel(ProductService productService, SalesService salesService)
    {
        _productService = productService;
        _salesService = salesService;

        AddToCartCommand = new RelayCommand(ExecuteAddToCart);
        RemoveFromCartCommand = new RelayCommand(ExecuteRemoveFromCart, () => SelectedCartItem != null);
        ClearCartCommand = new RelayCommand(ExecuteClearCart, () => CartItems.Count > 0);
        CompleteSaleCommand = new RelayCommand(async () => await ExecuteCompleteSaleAsync(), () => CartItems.Count > 0 && !IsProcessingSale);
        RefreshProductsCommand = new RelayCommand(async () => await LoadProductsAsync());
        SetExactTenderCommand = new RelayCommand(() => AmountTendered = Total);
        AddTenderPresetCommand = new RelayCommand<object>(param =>
        {
            if (param != null && decimal.TryParse(param.ToString(), out decimal addAmount))
            {
                AmountTendered += addAmount;
            }
        });
    }

    public async Task LoadProductsAsync()
    {
        try
        {
            var list = await _productService.GetAllProductsAsync();
            AvailableProducts.Clear();
            foreach (var product in list)
            {
                AvailableProducts.Add(product);
            }

            if (SelectedProduct != null)
            {
                SelectedProduct = AvailableProducts.FirstOrDefault(p => p.Id == SelectedProduct.Id);
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Error loading products: {ex.Message}";
        }
    }

    private void ClearValidation()
    {
        ValidationMessage = string.Empty;
        HasSuccessMessage = false;
        SuccessMessage = string.Empty;
    }

    private bool ValidateStockAvailability()
    {
        if (SelectedProduct == null)
            return false;

        if (QuantityInput <= 0)
        {
            ValidationMessage = "❌ Please enter a valid quantity greater than zero.";
            return false;
        }

        int existingQtyInCart = CartItems.FirstOrDefault(c => c.ProductId == SelectedProduct.Id)?.Quantity ?? 0;
        int totalRequested = existingQtyInCart + QuantityInput;

        if (totalRequested > SelectedProduct.StockQuantity)
        {
            ValidationMessage = $"❌ Insufficient Stock\nOnly {SelectedProduct.StockQuantity} units of '{SelectedProduct.Name}' are available.";
            return false;
        }

        return true;
    }

    private void ExecuteAddToCart()
    {
        ClearValidation();

        if (SelectedProduct == null)
        {
            ValidationMessage = "❌ Please select a product first.";
            return;
        }

        if (QuantityInput <= 0)
        {
            ValidationMessage = "❌ Quantity must be at least 1.";
            return;
        }

        int existingQtyInCart = CartItems.FirstOrDefault(c => c.ProductId == SelectedProduct.Id)?.Quantity ?? 0;
        int totalRequested = existingQtyInCart + QuantityInput;

        // Strict Stock Validation (Phase 10)
        if (totalRequested > SelectedProduct.StockQuantity)
        {
            ValidationMessage = $"❌ Insufficient Stock\nOnly {SelectedProduct.StockQuantity} units of '{SelectedProduct.Name}' are available.";
            return;
        }

        var existingItem = CartItems.FirstOrDefault(c => c.ProductId == SelectedProduct.Id);
        if (existingItem != null)
        {
            existingItem.Quantity += QuantityInput;
        }
        else
        {
            CartItems.Add(new CartItemViewModel(SelectedProduct, QuantityInput));
        }

        RecalculateTotals();
        QuantityInput = 1;
    }

    private void ExecuteRemoveFromCart()
    {
        if (SelectedCartItem != null)
        {
            CartItems.Remove(SelectedCartItem);
            RecalculateTotals();
        }
    }

    private void ExecuteClearCart()
    {
        CartItems.Clear();
        Discount = 0;
        AmountTendered = 0;
        ClearValidation();
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
        RecalculateChange();
    }

    private void RecalculateChange()
    {
        if (IsCashPayment)
        {
            ChangeDue = AmountTendered >= Total ? AmountTendered - Total : 0;
        }
        else
        {
            ChangeDue = 0;
        }
    }

    private async Task ExecuteCompleteSaleAsync()
    {
        if (CartItems.Count == 0)
        {
            ValidationMessage = "❌ Cart is empty. Add products to cart before checkout.";
            return;
        }

        decimal finalTendered = AmountTendered;
        decimal finalChange = ChangeDue;

        if (IsCashPayment)
        {
            if (AmountTendered <= 0)
            {
                // Auto-assume exact cash if user left tender field empty
                finalTendered = Total;
                finalChange = 0;
            }
            else if (AmountTendered < Total)
            {
                ValidationMessage = $"❌ Insufficient Cash Tendered!\nTendered: Rs. {AmountTendered:N2} is less than Total: Rs. {Total:N2}. Please collect remaining Rs. {(Total - AmountTendered):N2}.";
                return;
            }
            else
            {
                finalChange = AmountTendered - Total;
            }
        }
        else
        {
            finalTendered = Total;
            finalChange = 0;
        }

        try
        {
            IsProcessingSale = true;
            ClearValidation();

            // Prepare items for transaction
            var cartPayload = CartItems.Select(ci => (ci.ProductId, ci.Quantity)).ToList();

            // Execute atomic database transaction
            var receipt = await _salesService.ExecuteSaleTransactionAsync(
                cartPayload, 
                Discount, 
                SelectedPaymentMethod, 
                finalTendered, 
                finalChange);

            SuccessMessage = $"✔ Sale Completed!\nInvoice: #{receipt.InvoiceNumber} | Paid: Rs. {receipt.AmountTendered:N2} | Change: Rs. {receipt.ChangeDue:N2}";

            // Reset cart
            CartItems.Clear();
            Discount = 0;
            AmountTendered = 0;
            RecalculateTotals();

            // Refresh available product stocks
            await LoadProductsAsync();

            // Display Receipt Dialog
            OpenReceiptDialog?.Invoke(receipt);
        }
        catch (Exception ex)
        {
            // Transaction was rolled back automatically by SalesService
            ValidationMessage = $"❌ Sale failed\n{ex.Message}\nNo stock was deducted.";
            ShowMessageDialog?.Invoke($"Sale failed: {ex.Message}\n\nNo stock was deducted.", "Transaction Error");
        }
        finally
        {
            IsProcessingSale = false;
        }
    }
}
