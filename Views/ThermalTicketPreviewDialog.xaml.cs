using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using RetailFlow.Models;
using RetailFlow.Services;

namespace RetailFlow.Views;

public partial class ThermalTicketPreviewDialog : Window
{
    private readonly SaleReceipt _receipt;

    public ThermalTicketPreviewDialog(SaleReceipt receipt, string statusMessage = "Printed to Virtual ESC/POS Simulator.")
    {
        InitializeComponent();
        _receipt = receipt;

        var printerService = new EscPosPrinterService();
        TicketContentText.Text = printerService.GenerateSimulatorText(receipt);
        PrinterStatusText.Text = statusMessage;

        Loaded += (s, e) => TicketScrollViewer.ScrollToTop();
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var printDlg = new PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                var visual = ReceiptPrintHelper.CreatePrintableReceiptVisual(_receipt, printDlg.PrintableAreaWidth);
                printDlg.PrintVisual(visual, $"Receipt_{_receipt.InvoiceNumber}");
                MessageBox.Show(
                    $"✔ Receipt #{_receipt.InvoiceNumber} was sent to '{printDlg.PrintQueue?.Name ?? "printer"}' successfully!",
                    "Print Receipt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            var res = MessageBox.Show(
                $"Windows Print Dialog error: {ex.Message}\n\nWould you like to open the digital PDF/HTML invoice in your browser to print or save as PDF?",
                "Print Notice",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (res == MessageBoxResult.Yes)
            {
                ExportPdf_Click(sender, e);
            }
        }
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string htmlPath = ReceiptPrintHelper.ExportHtmlInvoice(_receipt);
            Process.Start(new ProcessStartInfo(htmlPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open digital invoice: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
