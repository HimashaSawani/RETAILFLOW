using System.Windows;
using RetailFlow.ViewModels;

namespace RetailFlow.Views;

/// <summary>
/// Interaction logic for ReceiptDialog.xaml
/// </summary>
public partial class ReceiptDialog : Window
{
    public ReceiptDialog(ReceiptDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.RequestClose += () => Close();
    }
}
