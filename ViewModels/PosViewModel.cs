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

    public ObservableCollection<Product> AvailableProducts { get; } = new();
    public ObservableCollection<CartItemViewModel> CartItems { get; } = new();

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
                OnPropertyChanged(nameof(Total));
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
        ClearValidation();
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
    }

    private async Task ExecuteCompleteSaleAsync()
    {
        if (CartItems.Count == 0)
        {
            ValidationMessage = "❌ Cart is empty. Add products to cart before checkout.";
            return;
        }

        try
        {
            IsProcessingSale = true;
            ClearValidation();

            // Validate all items against fresh stock quantities in DB
            foreach (var item in CartItems)
            {
                var dbProduct = await _productService.GetProductByIdAsync(item.ProductId);
                if (dbProduct == null)
                {
                    ValidationMessage = $"❌ Product '{item.ProductName}' was not found in database.";
                    return;
                }

                if (dbProduct.StockQuantity < item.Quantity)
                {
                    ValidationMessage = $"❌ Insufficient Stock\nOnly {dbProduct.StockQuantity} units of '{dbProduct.Name}' are available.";
                    return;
                }
            }

            // Create Sale entity
            var sale = new Sale
            {
                Discount = Discount,
                SaleItems = CartItems.Select(ci => new SaleItem
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.UnitPrice,
                    Subtotal = ci.Total
                }).ToList()
            };

            var processedSale = await _salesService.ProcessSaleAsync(sale);

            SuccessMessage = $"✔ Sale Completed Successfully!\nTransaction #: {processedSale.TransactionNumber}\nTotal Paid: {processedSale.Total:C}";
            ShowMessageDialog?.Invoke(
                $"Sale completed successfully!\n\nTransaction No: {processedSale.TransactionNumber}\nTotal Items: {CartItems.Sum(c => c.Quantity)}\nTotal Amount: {processedSale.Total:N2}",
                "Sale Success");

            // Reset cart
            CartItems.Clear();
            Discount = 0;
            RecalculateTotals();

            // Refresh available product stocks
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            ValidationMessage = $"❌ Failed to process sale: {ex.Message}";
        }
        finally
        {
            IsProcessingSale = false;
        }
    }
}
