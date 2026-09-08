using RetailFlow.Models;

namespace RetailFlow.ViewModels;

public class CartItemViewModel : ViewModelBase
{
    private readonly Product _product;
    private int _quantity;

    public Product Product => _product;
    public int ProductId => _product.Id;
    public string SKU => _product.SKU;
    public string ProductName => _product.Name;
    public decimal UnitPrice => _product.SellingPrice;
    public int AvailableStock => _product.StockQuantity;

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                OnPropertyChanged(nameof(Total));
            }
        }
    }

    public decimal Total => Quantity * UnitPrice;

    public CartItemViewModel(Product product, int quantity)
    {
        _product = product;
        _quantity = quantity;
    }
}
