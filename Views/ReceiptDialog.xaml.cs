using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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
        viewModel.RequestPrint += () => HandlePrintReceipt(viewModel);
    }

    private void HandlePrintReceipt(ReceiptDialogViewModel vm)
    {
        try
        {
            var printDlg = new PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                printDlg.PrintVisual(ReceiptPrintArea, $"Receipt_{vm.InvoiceNumber}");
                MessageBox.Show(
                    $"Receipt for #{vm.InvoiceNumber} was sent to printer successfully!", 
                    "Print Receipt", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            }
        }
        catch
        {
            // Fallback: Export text receipt to disk
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("========================================");
                sb.AppendLine("             RETAILFLOW POS             ");
                sb.AppendLine("========================================");
                sb.AppendLine($"Invoice: #{vm.InvoiceNumber}");
                sb.AppendLine($"Date:    {vm.SaleDate:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine("----------------------------------------");
                foreach (var item in vm.Items)
                {
                    sb.AppendLine($"{item.ProductName} ({item.Quantity}x @ Rs.{item.UnitPrice:N2}) = Rs.{item.LineTotal:N2}");
                }
                sb.AppendLine("----------------------------------------");
                sb.AppendLine($"Subtotal:       Rs. {vm.Subtotal:N2}");
                sb.AppendLine($"Discount:     - Rs. {vm.Discount:N2}");
                sb.AppendLine($"TOTAL:          Rs. {vm.Total:N2}");
                sb.AppendLine($"Payment Method: {vm.PaymentMethod}");
                sb.AppendLine($"Amount Paid:    Rs. {vm.AmountTendered:N2}");
                sb.AppendLine($"Change Given:   Rs. {vm.ChangeDue:N2}");
                sb.AppendLine("========================================");
                sb.AppendLine("        Thank you for your visit!       ");
                sb.AppendLine("========================================");

                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Receipt_{vm.InvoiceNumber}.txt");
                File.WriteAllText(filePath, sb.ToString());

                MessageBox.Show(
                    $"Receipt exported successfully to:\n{filePath}", 
                    "Receipt Exported", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            }
            catch (Exception innerEx)
            {
                MessageBox.Show($"Could not print or export receipt: {innerEx.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
