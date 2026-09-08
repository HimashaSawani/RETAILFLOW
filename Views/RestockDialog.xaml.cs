using System.Windows;
using RetailFlow.ViewModels;

namespace RetailFlow.Views;

/// <summary>
/// Interaction logic for RestockDialog.xaml
/// </summary>
public partial class RestockDialog : Window
{
    public RestockDialog(RestockDialogViewModel viewModel)
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
