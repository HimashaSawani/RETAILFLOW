using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using RetailFlow.Services;
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
        viewModel.RequestPrintEscPos += () => HandlePrintEscPos(viewModel);
        viewModel.RequestExportHtml += () => HandleExportHtmlInvoice(viewModel);
    }

    private void HandlePrintEscPos(ReceiptDialogViewModel vm)
    {
        try
        {
            var printerService = new EscPosPrinterService();
            var result = printerService.PrintReceipt(vm.ReceiptModel);
            if (result.Success)
            {
                var preview = new ThermalTicketPreviewDialog(vm.ReceiptModel, result.Message)
                {
                    Owner = this
                };
                preview.ShowDialog();
            }
            else
            {
                MessageBox.Show(result.Message, "Thermal Printer Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Thermal print error: {ex.Message}", "Printer Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void HandleExportHtmlInvoice(ReceiptDialogViewModel vm)
    {
        try
        {
            string htmlPath = ReceiptPrintHelper.ExportHtmlInvoice(vm.ReceiptModel);
            Process.Start(new ProcessStartInfo(htmlPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open digital invoice: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void HandlePrintReceipt(ReceiptDialogViewModel vm)
    {
        try
        {
            var printDlg = new PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                // Render off-screen printable visual specifically measured for paper/PDF dimensions (eliminates ScrollViewer viewport clipping)
                var printableVisual = ReceiptPrintHelper.CreatePrintableReceiptVisual(vm.ReceiptModel, printDlg.PrintableAreaWidth);
                printDlg.PrintVisual(printableVisual, $"Receipt_{vm.InvoiceNumber}");

                MessageBox.Show(
                    $"✔ Receipt for #{vm.InvoiceNumber} was sent to '{printDlg.PrintQueue?.Name ?? "printer"}' successfully!", 
                    "Print Receipt", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            // If the Windows print queue encountered a spooler or driver issue, offer immediate open in browser as digital PDF
            try
            {
                string htmlPath = ReceiptPrintHelper.ExportHtmlInvoice(vm.ReceiptModel);
                var res = MessageBox.Show(
                    $"Windows Print Dialog encountered an issue: {ex.Message}\n\nWould you like to open the digital PDF/HTML invoice in your browser now to print or save it as PDF?",
                    "Print Notice",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (res == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(htmlPath) { UseShellExecute = true });
                }
            }
            catch (Exception innerEx)
            {
                MessageBox.Show($"Could not complete print or export: {innerEx.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
