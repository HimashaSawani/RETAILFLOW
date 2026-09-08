using RetailFlow.Models;

namespace RetailFlow.ViewModels;

public class StockItemViewModel : ViewModelBase
{
    private Product _product;

    public Product Product => _product;

    public int Id => _product.Id;
    public string SKU => _product.SKU;
    public string Name => _product.Name;
    public string Category => _product.Category;
    public int StockQuantity => _product.StockQuantity;
    public int ReorderLevel => _product.ReorderLevel;

    public string Status
    {
        get
        {
            if (StockQuantity == 0)
                return "OUT";
            if (StockQuantity <= ReorderLevel)
                return "LOW";
            return "OK";
        }
    }

    public string StatusColor
    {
        get
        {
            return Status switch
            {
                "OUT" => "#DC2626", // Red
                "LOW" => "#D97706", // Amber
                _ => "#16A34A"      // Green
            };
        }
    }

    public string StatusBackground
    {
        get
        {
            return Status switch
            {
                "OUT" => "#FEE2E2",
                "LOW" => "#FEF3C7",
                _ => "#DCFCE7"
            };
        }
    }

    public StockItemViewModel(Product product)
    {
        _product = product;
    }

    public void UpdateProduct(Product product)
    {
        _product = product;
        OnPropertyChanged(nameof(StockQuantity));
        OnPropertyChanged(nameof(ReorderLevel));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusColor));
        OnPropertyChanged(nameof(StatusBackground));
    }
}
