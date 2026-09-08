using RetailFlow.Commands;
using RetailFlow.Models;
using RetailFlow.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RetailFlow.ViewModels;

public class ProductFormViewModel : ViewModelBase
{
    private readonly ProductService _productService;
    private readonly int _productId;
    private readonly bool _isEditMode;

    private string _sku = string.Empty;
    private string _name = string.Empty;
    private string _category = string.Empty;
    private decimal _costPrice;
    private decimal _sellingPrice;
    private int _stockQuantity;
    private int _reorderLevel = 5;
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isSaving;

    public event Action<bool>? RequestClose;

    public string Title => _isEditMode ? "Edit Product" : "Add Product";
    public bool IsEditMode => _isEditMode;

    public string SKU
    {
        get => _sku;
        set { SetProperty(ref _sku, value); ClearError(); }
    }

    public string Name
    {
        get => _name;
        set { SetProperty(ref _name, value); ClearError(); }
    }

    public string Category
    {
        get => _category;
        set { SetProperty(ref _category, value); ClearError(); }
    }

    public decimal CostPrice
    {
        get => _costPrice;
        set { SetProperty(ref _costPrice, value); ClearError(); }
    }

    public decimal SellingPrice
    {
        get => _sellingPrice;
        set { SetProperty(ref _sellingPrice, value); ClearError(); }
    }

    public int StockQuantity
    {
        get => _stockQuantity;
        set { SetProperty(ref _stockQuantity, value); ClearError(); }
    }

    public int ReorderLevel
    {
        get => _reorderLevel;
        set { SetProperty(ref _reorderLevel, value); ClearError(); }
    }

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

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public ProductFormViewModel(ProductService productService, Product? productToEdit = null)
    {
        _productService = productService;

        if (productToEdit != null)
        {
            _isEditMode = true;
            _productId = productToEdit.Id;
            _sku = productToEdit.SKU;
            _name = productToEdit.Name;
            _category = productToEdit.Category;
            _costPrice = productToEdit.CostPrice;
            _sellingPrice = productToEdit.SellingPrice;
            _stockQuantity = productToEdit.StockQuantity;
            _reorderLevel = productToEdit.ReorderLevel;
        }
        else
        {
            _isEditMode = false;
            _productId = 0;
            _reorderLevel = 5;
        }

        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => !IsSaving);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false));
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
    }

    public async Task<bool> ValidateAsync()
    {
        if (string.IsNullOrWhiteSpace(SKU))
        {
            ErrorMessage = "SKU cannot be empty. ❌";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Product Name cannot be empty. ❌";
            return false;
        }

        if (CostPrice < 0)
        {
            ErrorMessage = "Cost Price cannot be negative. ❌";
            return false;
        }

        if (SellingPrice < 0)
        {
            ErrorMessage = "Selling Price cannot be negative. ❌";
            return false;
        }

        if (StockQuantity < 0)
        {
            ErrorMessage = "Stock Quantity cannot be negative. ❌";
            return false;
        }

        if (ReorderLevel < 0)
        {
            ErrorMessage = "Reorder Level cannot be negative. ❌";
            return false;
        }

        bool isDuplicate = await _productService.IsSkuExistsAsync(SKU, _productId);
        if (isDuplicate)
        {
            ErrorMessage = $"Duplicate SKU: '{SKU.Trim()}' already exists. ❌";
            return false;
        }

        ErrorMessage = string.Empty;
        return true;
    }

    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;

            if (!await ValidateAsync())
                return;

            if (_isEditMode)
            {
                var product = new Product
                {
                    Id = _productId,
                    SKU = SKU,
                    Name = Name,
                    Category = Category,
                    CostPrice = CostPrice,
                    SellingPrice = SellingPrice,
                    StockQuantity = StockQuantity,
                    ReorderLevel = ReorderLevel
                };
                await _productService.UpdateProductAsync(product);
            }
            else
            {
                var product = new Product
                {
                    SKU = SKU,
                    Name = Name,
                    Category = Category,
                    CostPrice = CostPrice,
                    SellingPrice = SellingPrice,
                    StockQuantity = StockQuantity,
                    ReorderLevel = ReorderLevel
                };
                await _productService.AddProductAsync(product);
            }

            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving product: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }
}
