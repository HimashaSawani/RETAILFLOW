using System.Windows;
using RetailFlow.ViewModels;

namespace RetailFlow.Views;

/// <summary>
/// Interaction logic for ProductFormDialog.xaml
/// </summary>
public partial class ProductFormDialog : Window
{
    public ProductFormDialog(ProductFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.RequestClose += success =>
        {
            DialogResult = success;
            Close();
        };
    }
}
