using System.Windows;
using RetailFlow.Models;
using RetailFlow.Services;

namespace RetailFlow.Views;

public partial class ThermalTicketPreviewDialog : Window
{
    public ThermalTicketPreviewDialog(SaleReceipt receipt, string statusMessage = "Printed to Virtual ESC/POS Simulator.")
    {
        InitializeComponent();

        var printerService = new EscPosPrinterService();
        TicketContentText.Text = printerService.GenerateSimulatorText(receipt);
        PrinterStatusText.Text = statusMessage;

        Loaded += (s, e) => TicketScrollViewer.ScrollToTop();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
