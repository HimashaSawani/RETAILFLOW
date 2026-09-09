using RetailFlow.Commands;
using RetailFlow.Helpers;
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
    private readonly CustomerService _customerService;
    private readonly AuthService _authService;
    private readonly EscPosPrinterService _printerService;

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

    // Barcode Scanner
    private string _barcodeInput = string.Empty;

    // Customer Loyalty
    private string _customerPhoneInput = string.Empty;
    private Customer? _currentCustomer;
    private int _pointsToRedeem;
    private string _loyaltyStatusText = "Enter phone number to look up customer";

    public ObservableCollection<string> PaymentMethods { get; } = new() { "Cash", "Card", "QR / Transfer" };
    public ObservableCollection<Product> AvailableProducts { get; } = new();
    public ObservableCollection<CartItemViewModel> CartItems { get; } = new();

    public string BarcodeInput
    {
        get => _barcodeInput;
        set
        {
            if (SetProperty(ref _barcodeInput, value))
            {
                // Live auto-preview: As soon as user types or scans a valid SKU, automatically select and show the product!
                if (!string.IsNullOrWhiteSpace(value))
                {
                    string code = value.Trim();
                    var match = AvailableProducts.FirstOrDefault(p =>
                        string.Equals(p.SKU, code, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.Name, code, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        SelectedProduct = match;
                    }
                }
            }
        }
    }

    public string CustomerPhoneInput
    {
        get => _customerPhoneInput;
        set => SetProperty(ref _customerPhoneInput, value);
    }

    public Customer? CurrentCustomer
    {
        get => _currentCustomer;
        set
        {
            if (SetProperty(ref _currentCustomer, value))
            {
                OnPropertyChanged(nameof(IsCustomerSelected));
                PointsToRedeem = 0;
                RecalculateTotals();
            }
        }
    }

    public bool IsCustomerSelected => CurrentCustomer != null;

    public int PointsToRedeem
    {
        get => _pointsToRedeem;
        set
        {
            int maxAllowed = CurrentCustomer?.LoyaltyPoints ?? 0;
            if (value > maxAllowed) value = maxAllowed;
            if (value < 0) value = 0;

            decimal maxRedeemableValue = Math.Max(0, Subtotal - Discount);
            if (value > maxRedeemableValue)
            {
                value = (int)maxRedeemableValue;
            }

            if (SetProperty(ref _pointsToRedeem, value))
            {
                OnPropertyChanged(nameof(LoyaltyDiscount));
                RecalculateTotals();
            }
        }
    }

    public decimal LoyaltyDiscount => PointsToRedeem * 1.0m;

    public string LoyaltyStatusText
    {
        get => _loyaltyStatusText;
        set => SetProperty(ref _loyaltyStatusText, value);
    }

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
            if (value < 0) value = 0;
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

    public int TotalCartQuantity => CartItems.Sum(ci => ci.Quantity);

    public decimal Subtotal => CartItems.Sum(item => item.Total);

    public decimal Total => Math.Max(0, Subtotal - Discount - LoyaltyDiscount);

    public int PointsToEarn => (int)(Total / 100);

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
    public ICommand ScanBarcodeCommand { get; }
    public ICommand LookupCustomerCommand { get; }
    public ICommand OpenQuickRegisterCustomerCommand { get; }
    public ICommand ClearCustomerCommand { get; }
    public ICommand RedeemMaxPointsCommand { get; }
    public ICommand ApplyPointsDiscountCommand { get; }
    public ICommand IncreaseQuantityCommand { get; }
    public ICommand DecreaseQuantityCommand { get; }

    public Action<SaleReceipt>? OpenReceiptDialog { get; set; }
    public Func<string, Task<Customer?>>? OpenQuickCustomerDialog { get; set; }
    public Action<string, string>? ShowMessageDialog { get; set; }

    public PosViewModel(
        ProductService productService, 
        SalesService salesService,
        CustomerService customerService,
        AuthService authService,
        EscPosPrinterService printerService)
    {
        _productService = productService;
        _salesService = salesService;
        _customerService = customerService;
        _authService = authService;
        _printerService = printerService;

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

        ScanBarcodeCommand = new RelayCommand(() => HandleBarcodeScanned(BarcodeInput));
        LookupCustomerCommand = new RelayCommand(async () => await ExecuteCustomerLookupAsync());
        OpenQuickRegisterCustomerCommand = new RelayCommand(async () => await ExecuteQuickCustomerRegisterAsync());
        ClearCustomerCommand = new RelayCommand(() =>
        {
            CurrentCustomer = null;
            CustomerPhoneInput = string.Empty;
            LoyaltyStatusText = "Customer removed.";
        });

        RedeemMaxPointsCommand = new RelayCommand(() =>
        {
            if (CurrentCustomer != null)
            {
                PointsToRedeem = CurrentCustomer.LoyaltyPoints;
            }
        });

        ApplyPointsDiscountCommand = new RelayCommand(() =>
        {
            if (PointsToRedeem > 0)
            {
                RecalculateTotals();
                SuccessMessage = $"✔ Applied Rs. {LoyaltyDiscount:N2} loyalty reward discount.";
            }
        });

        IncreaseQuantityCommand = new RelayCommand<CartItemViewModel>(item =>
        {
            if (item == null) return;
            var prod = AvailableProducts.FirstOrDefault(p => p.Id == item.ProductId);
            if (prod != null && item.Quantity + 1 <= prod.StockQuantity)
            {
                item.Quantity += 1;
                RecalculateTotals();
            }
            else
            {
                ValidationMessage = $"❌ Cannot add more. Available stock limit reached ({prod?.StockQuantity ?? 0}).";
            }
        });

        DecreaseQuantityCommand = new RelayCommand<CartItemViewModel>(item =>
        {
            if (item == null) return;
            if (item.Quantity > 1)
            {
                item.Quantity -= 1;
                RecalculateTotals();
            }
            else
            {
                CartItems.Remove(item);
                RecalculateTotals();
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
            else if (AvailableProducts.Count > 0)
            {
                SelectedProduct = AvailableProducts[0];
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Error loading products: {ex.Message}";
        }
    }

    public void HandleBarcodeScanned(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return;

        string code = barcode.Trim();
        BarcodeInput = string.Empty;

        var product = AvailableProducts.FirstOrDefault(p =>
            string.Equals(p.SKU, code, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.Name, code, StringComparison.OrdinalIgnoreCase));

        if (product == null)
        {
            ValidationMessage = $"❌ Scanned Barcode '{code}' not found in catalog!";
            BarcodeScannerListener.PlayBeep();
            return;
        }

        // Automatically populate and show the product in the item selection card
        SelectedProduct = product;
        QuantityInput = 1;

        int existingQty = CartItems.FirstOrDefault(c => c.ProductId == product.Id)?.Quantity ?? 0;
        if (existingQty + 1 > product.StockQuantity)
        {
            ValidationMessage = $"❌ Insufficient Stock: '{product.Name}' has only {product.StockQuantity} available.";
            BarcodeScannerListener.PlayBeep();
            return;
        }

        var existing = CartItems.FirstOrDefault(c => c.ProductId == product.Id);
        if (existing != null)
        {
            existing.Quantity += 1;
        }
        else
        {
            CartItems.Add(new CartItemViewModel(product, 1));
        }

        BarcodeScannerListener.PlayBeep();
        ClearValidation();
        SuccessMessage = $"✔ Scanned & Added: {product.Name} (Rs. {product.SellingPrice:N2})";
        RecalculateTotals();
    }

    private async Task ExecuteCustomerLookupAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomerPhoneInput))
        {
            LoyaltyStatusText = "Please enter a phone number.";
            return;
        }

        string phone = CustomerPhoneInput.Trim();
        var customer = await _customerService.GetByPhoneAsync(phone);

        if (customer != null)
        {
            CurrentCustomer = customer;
            LoyaltyStatusText = $"✔ Found: {customer.Name} | {customer.LoyaltyPoints} Points Available";
            BarcodeScannerListener.PlayBeep();
        }
        else
        {
            LoyaltyStatusText = "❌ Customer not found. Click '+ Register' to add.";
            CurrentCustomer = null;
        }
    }

    private async Task ExecuteQuickCustomerRegisterAsync()
    {
        if (OpenQuickCustomerDialog != null)
        {
            var newCustomer = await OpenQuickCustomerDialog(CustomerPhoneInput);
            if (newCustomer != null)
            {
                CurrentCustomer = newCustomer;
                CustomerPhoneInput = newCustomer.PhoneNumber;
                LoyaltyStatusText = $"✔ Registered: {newCustomer.Name} | 0 Points";
                BarcodeScannerListener.PlayBeep();
            }
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

        BarcodeScannerListener.PlayBeep();
        ClearValidation();
        SuccessMessage = $"✔ Added '{SelectedProduct.Name}' (x{QuantityInput}) to cart.";
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
        PointsToRedeem = 0;
        AmountTendered = 0;
        ClearValidation();
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalCartQuantity));
        OnPropertyChanged(nameof(PointsToEarn));
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

            var cartPayload = CartItems.Select(ci => (ci.ProductId, ci.Quantity)).ToList();
            string cashierName = _authService.CurrentUser?.FullName ?? "Cashier";

            // Execute atomic database transaction with customer loyalty
            var receipt = await _salesService.ExecuteSaleTransactionAsync(
                cartPayload, 
                Discount + LoyaltyDiscount, 
                SelectedPaymentMethod, 
                finalTendered, 
                finalChange,
                CurrentCustomer,
                PointsToRedeem,
                cashierName);

            // Trigger ESC/POS Hardware thermal print and cash drawer
            try
            {
                _printerService.PrintReceipt(receipt);
                if (SelectedPaymentMethod == "Cash")
                {
                    _printerService.KickCashDrawer();
                }
            }
            catch
            {
                // Silently continue if printer is offline
            }

            SuccessMessage = $"✔ Sale Completed!\nInvoice: #{receipt.InvoiceNumber} | Paid: Rs. {receipt.AmountTendered:N2} | Change: Rs. {receipt.ChangeDue:N2}";

            // Reset checkout state
            CartItems.Clear();
            Discount = 0;
            PointsToRedeem = 0;
            AmountTendered = 0;
            CurrentCustomer = null;
            CustomerPhoneInput = string.Empty;
            LoyaltyStatusText = "Enter phone number to look up customer";
            RecalculateTotals();

            // Refresh available product stocks
            await LoadProductsAsync();

            // Display Receipt Dialog
            OpenReceiptDialog?.Invoke(receipt);
        }
        catch (Exception ex)
        {
            ValidationMessage = $"❌ Sale failed\n{ex.Message}\nNo stock was deducted.";
            ShowMessageDialog?.Invoke($"Sale failed: {ex.Message}\n\nNo stock was deducted.", "Transaction Error");
        }
        finally
        {
            IsProcessingSale = false;
        }
    }
}
