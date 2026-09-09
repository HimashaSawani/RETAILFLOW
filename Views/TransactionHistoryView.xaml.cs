using RetailFlow.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace RetailFlow.Views;

public partial class TransactionHistoryView : UserControl
{
    public TransactionHistoryView()
    {
        InitializeComponent();
    }

    private void TransactionsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is TransactionHistoryViewModel vm && vm.SelectedTransaction != null)
        {
            vm.ShowReceiptDetails(vm.SelectedTransaction);
        }
    }
}
