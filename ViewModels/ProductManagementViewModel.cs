using RetailFlow.Commands;
using RetailFlow.Models;
using RetailFlow.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RetailFlow.ViewModels;

public class ProductManagementViewModel : ViewModelBase
{
    private readonly ProductService _productService;
    private string _searchText = string.Empty;
    private string _selectedCategory = "All Categories";
    private Product? _selectedProduct;
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<string> Categories { get; } = new() { "All Categories" };

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = FilterProductsAsync();
            }
        }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                _ = FilterProductsAsync();
            }
        }
    }

    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
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

    public ICommand SearchCommand { get; }
    public ICommand AddProductCommand { get; }
    public ICommand EditProductCommand { get; }
    public ICommand DeleteProductCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand PrintBarcodeCommand { get; }

    // Action delegates for UI interactions (opening dialogs, message boxes)
    public Func<Product?, Task<bool>>? OpenProductFormDialog { get; set; }
    public Action<Product>? OpenBarcodePrintDialog { get; set; }
    public Func<string, string, bool>? ShowConfirmationDialog { get; set; }
    public Action<string, string>? ShowMessageDialog { get; set; }

    public ProductManagementViewModel(ProductService productService)
    {
        _productService = productService;

        SearchCommand = new RelayCommand(async () => await FilterProductsAsync());
        RefreshCommand = new RelayCommand(async () => await LoadProductsAsync());
        AddProductCommand = new RelayCommand(async () => await ExecuteAddProductAsync());
        EditProductCommand = new RelayCommand(async () => await ExecuteEditProductAsync(), () => SelectedProduct != null);
        DeleteProductCommand = new RelayCommand(async () => await ExecuteDeleteProductAsync(), () => SelectedProduct != null);
        PrintBarcodeCommand = new RelayCommand(() =>
        {
            if (SelectedProduct != null)
            {
                OpenBarcodePrintDialog?.Invoke(SelectedProduct);
            }
        }, () => SelectedProduct != null);
    }

    public async Task LoadProductsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading products...";

            var list = await _productService.GetAllProductsAsync();
            
            // Populate distinct categories
            var currentSelectedCat = SelectedCategory;
            var distinctCats = list
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            Categories.Clear();
            Categories.Add("All Categories");
            foreach (var cat in distinctCats)
            {
                Categories.Add(cat);
            }

            if (Categories.Contains(currentSelectedCat))
            {
                _selectedCategory = currentSelectedCat;
                OnPropertyChanged(nameof(SelectedCategory));
            }
            else
            {
                _selectedCategory = "All Categories";
                OnPropertyChanged(nameof(SelectedCategory));
            }

            await FilterProductsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load products: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task FilterProductsAsync()
    {
        try
        {
            IsLoading = true;
            var list = await _productService.GetAllProductsAsync();

            var filtered = list.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string query = SearchText.Trim().ToLower();
                filtered = filtered.Where(p => 
                    p.Name.ToLower().Contains(query) || 
                    p.SKU.ToLower().Contains(query) || 
                    p.Category.ToLower().Contains(query));
            }

            if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "All Categories")
            {
                filtered = filtered.Where(p => string.Equals(p.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase));
            }

            var result = filtered.ToList();

            Products.Clear();
            foreach (var item in result)
            {
                Products.Add(item);
            }

            StatusMessage = $"{Products.Count} product(s) displayed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Filter error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteAddProductAsync()
    {
        if (OpenProductFormDialog == null)
            return;

        bool saved = await OpenProductFormDialog.Invoke(null);
        if (saved)
        {
            await LoadProductsAsync();
            StatusMessage = "Product added successfully! ✔";
        }
    }

    private async Task ExecuteEditProductAsync()
    {
        if (SelectedProduct == null || OpenProductFormDialog == null)
            return;

        bool saved = await OpenProductFormDialog.Invoke(SelectedProduct);
        if (saved)
        {
            await LoadProductsAsync();
            StatusMessage = "Product updated successfully! ✔";
        }
    }

    private async Task ExecuteDeleteProductAsync()
    {
        if (SelectedProduct == null)
            return;

        bool confirm = ShowConfirmationDialog?.Invoke(
            $"Are you sure you want to delete product '{SelectedProduct.Name}' (SKU: {SelectedProduct.SKU})?",
            "Confirm Delete") ?? true;

        if (!confirm)
            return;

        try
        {
            IsLoading = true;
            bool success = await _productService.DeleteProductAsync(SelectedProduct.Id);
            if (success)
            {
                StatusMessage = $"Product '{SelectedProduct.Name}' deleted. ✔";
                await LoadProductsAsync();
            }
            else
            {
                StatusMessage = "Product not found or could not be deleted.";
            }
        }
        catch (Exception ex)
        {
            ShowMessageDialog?.Invoke(ex.Message, "Delete Error");
            StatusMessage = $"Delete failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
