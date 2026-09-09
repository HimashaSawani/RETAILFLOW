using RetailFlow.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RetailFlow.Services;

public class BarcodeService
{
    // Code 128 Character Patterns (0 to 106)
    // 0: space, 1: bar. Each symbol is 11 modules long, except Stop (106) which is 13 modules.
    private static readonly string[] Code128Patterns = new string[]
    {
        "11011001100", "11001101100", "11001100110", "10010011000", "10010001100", // 0-4
        "10001001100", "10011001000", "10011000100", "10001100100", "11001001000", // 5-9
        "11001000100", "11000100100", "10110011100", "10011011100", "10011001110", // 10-14
        "10111001100", "10011101100", "10011100110", "11001110010", "11001011100", // 15-19
        "11001001110", "11011100100", "11001110100", "11101101110", "11101001100", // 20-24
        "11100101100", "11100100110", "11101100100", "11100110100", "11100110010", // 25-29
        "11011011000", "11011000110", "11000110110", "10100011000", "10001011000", // 30-34
        "10001000110", "10110001000", "10001101000", "10001100010", "11010001000", // 35-39
        "11000101000", "11000100010", "10110111000", "10110001110", "10001101110", // 40-44
        "10111011000", "10111000110", "10001110110", "11101110110", "11010001110", // 45-49
        "11000101110", "11011101000", "11011100010", "11011101110", "11101011000", // 50-54
        "11101000110", "11100010110", "11101101000", "11101100010", "11100011010", // 55-59
        "11101111010", "11001000010", "11110001010", "10100110000", "10100001100", // 60-64
        "10010110000", "10010000110", "10000101100", "10000100110", "10110010000", // 65-69
        "10110000100", "10011010000", "10011000010", "10000110100", "10000110010", // 70-74
        "11000010010", "11001010000", "11110111010", "11000010100", "10001111010", // 75-79
        "10100111100", "10010111100", "10010011110", "10111100100", "10011110100", // 80-84
        "10011110010", "11110100100", "11110010100", "11110010010", "11011011110", // 85-89
        "11011110110", "11110110110", "10101111000", "10100011110", "10001011110", // 90-94
        "10111101000", "10111100010", "11110101000", "11110100010", "10111011110", // 95-99
        "10111101110", "11101011110", "11110101110", "11010000100", "11010010000", // 100-104 (104: Start B)
        "11010011100", "1100011101011" // 105: Start C, 106: STOP pattern
    };

    public DrawingImage GenerateCode128Barcode(string text, double width = 240, double height = 65)
    {
        if (string.IsNullOrWhiteSpace(text))
            text = "000000";

        var bitPattern = EncodeCode128B(text);

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            // Background - pure white
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

            // Module bar width
            double totalModules = bitPattern.Length;
            double quietZoneModules = 10;
            double scale = width / (totalModules + (quietZoneModules * 2));
            double startX = quietZoneModules * scale;

            for (int i = 0; i < bitPattern.Length; i++)
            {
                if (bitPattern[i] == '1')
                {
                    dc.DrawRectangle(Brushes.Black, null, new Rect(startX + (i * scale), 0, Math.Ceiling(scale), height));
                }
            }
        }

        return new DrawingImage(visual.Drawing);
    }

    private static string EncodeCode128B(string content)
    {
        var codeValues = new List<int> { 104 }; // Start Code B = 104
        int checksum = 104;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            int ascii = (int)c;
            int codeValue = (ascii >= 32 && ascii <= 126) ? ascii - 32 : 0;
            codeValues.Add(codeValue);
            checksum += codeValue * (i + 1);
        }

        int checkDigit = checksum % 103;
        codeValues.Add(checkDigit);
        codeValues.Add(106); // Stop Code = 106

        var pattern = new System.Text.StringBuilder();
        foreach (int code in codeValues)
        {
            if (code >= 0 && code < Code128Patterns.Length)
            {
                pattern.Append(Code128Patterns[code]);
            }
        }

        return pattern.ToString();
    }

    public Border CreateLabelStickerElement(Product product, double stickerWidth = 240, double stickerHeight = 150)
    {
        var border = new Border
        {
            Width = stickerWidth,
            Height = stickerHeight,
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(6)
        };

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Header: Store Name
        var header = new TextBlock
        {
            Text = "RETAILFLOW STORE",
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        };
        stack.Children.Add(header);

        // Product Name
        var nameText = new TextBlock
        {
            Text = product.Name,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = stickerWidth - 20,
            Margin = new Thickness(0, 0, 0, 4)
        };
        stack.Children.Add(nameText);

        // Barcode Image
        var barcodeImage = new Image
        {
            Source = GenerateCode128Barcode(product.SKU, stickerWidth - 30, 45),
            Height = 45,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 2)
        };
        stack.Children.Add(barcodeImage);

        // SKU code readable text
        var skuText = new TextBlock
        {
            Text = product.SKU,
            FontSize = 10,
            FontFamily = new FontFamily("Consolas, Courier New"),
            Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        };
        stack.Children.Add(skuText);

        // Price Text in Bold
        var priceText = new TextBlock
        {
            Text = $"Rs. {product.SellingPrice:N2}",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(15, 118, 110)),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stack.Children.Add(priceText);

        border.Child = stack;
        return border;
    }

    public bool PrintLabels(Product product, int quantity)
    {
        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true)
            return false;

        var wrapPanel = new WrapPanel
        {
            Width = printDialog.PrintableAreaWidth,
            Margin = new Thickness(10)
        };

        for (int i = 0; i < quantity; i++)
        {
            var sticker = CreateLabelStickerElement(product, 220, 140);
            wrapPanel.Children.Add(sticker);
        }

        wrapPanel.Measure(new Size(printDialog.PrintableAreaWidth, double.PositiveInfinity));
        wrapPanel.Arrange(new Rect(new Point(0, 0), wrapPanel.DesiredSize));

        printDialog.PrintVisual(wrapPanel, $"Barcodes_{product.SKU}_{quantity}pcs");
        return true;
    }
}
