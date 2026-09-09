using RetailFlow.Helpers;
using RetailFlow.Services;
using System;
using System.Windows;
using System.Windows.Input;

namespace RetailFlow.Views;

public partial class HardwareSettingsDialog : Window
{
    private readonly EscPosPrinterService _printerService;

    public HardwareSettingsDialog(EscPosPrinterService printerService)
    {
        InitializeComponent();
        _printerService = printerService;

        var printers = _printerService.GetAvailablePrinters();
        PrintersComboBox.ItemsSource = printers;
        PrintersComboBox.SelectedItem = _printerService.SelectedPrinterName;

        if (PrintersComboBox.SelectedIndex < 0 && printers.Count > 0)
        {
            PrintersComboBox.SelectedIndex = 0;
        }

        PrintersComboBox.SelectionChanged += (s, e) =>
        {
            if (PrintersComboBox.SelectedItem is string name)
            {
                _printerService.SelectedPrinterName = name;
                StatusOutputText.Text = $"Active printer set to: {name}";
            }
        };

        Loaded += (s, e) => ScannerTestBox.Focus();
    }

    private void TestPrint_Click(object sender, RoutedEventArgs e)
    {
        var result = _printerService.TestPrint();
        StatusOutputText.Text = (result.Success ? "✔ " : "❌ ") + result.Message;
    }

    private void KickDrawer_Click(object sender, RoutedEventArgs e)
    {
        var result = _printerService.KickCashDrawer();
        StatusOutputText.Text = (result.Success ? "✔ " : "❌ ") + result.Message;
    }

    private void TestBeep_Click(object sender, RoutedEventArgs e)
    {
        BarcodeScannerListener.PlayBeep();
        StatusOutputText.Text = "✔ Barcode Scanner confirmation tone sounded.";
    }

    private void ScannerTestBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            string scanned = ScannerTestBox.Text.Trim();
            if (!string.IsNullOrEmpty(scanned))
            {
                BarcodeScannerListener.PlayBeep();
                ScanResultText.Text = $"✔ Captured Barcode: '{scanned}' at {DateTime.Now:HH:mm:ss}";
                ScannerTestBox.SelectAll();
            }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
