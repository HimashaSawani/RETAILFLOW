using RetailFlow.Models;
using RetailFlow.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RetailFlow.Views;

public partial class BarcodePrintDialog : Window
{
    private readonly BarcodeService _barcodeService;
    private readonly Product _product;

    public BarcodePrintDialog(BarcodeService barcodeService, Product product)
    {
        InitializeComponent();
        _barcodeService = barcodeService;
        _product = product;

        ProductNameText.Text = product.Name;
        ProductSkuText.Text = $"SKU: {product.SKU} | Category: {product.Category}";
        ProductPriceText.Text = $"Rs. {product.SellingPrice:N2}";
        StockQtyBtn.Content = $"Match Stock ({product.StockQuantity})";

        // Render preview sticker
        StickerPreviewContainer.Content = _barcodeService.CreateLabelStickerElement(_product, 240, 150);
    }

    private void PresetQty_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content is string qty)
        {
            QuantityTextBox.Text = qty;
        }
    }

    private void MatchStock_Click(object sender, RoutedEventArgs e)
    {
        QuantityTextBox.Text = Math.Max(1, _product.StockQuantity).ToString();
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(QuantityTextBox.Text, out int qty) || qty <= 0)
        {
            MessageBox.Show("Please enter a valid label quantity greater than zero.", "Invalid Quantity", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            bool printed = _barcodeService.PrintLabels(_product, qty);
            if (printed)
            {
                MessageBox.Show($"Successfully sent {qty} barcode label(s) to printer.", "Labels Printed", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error printing labels: {ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
