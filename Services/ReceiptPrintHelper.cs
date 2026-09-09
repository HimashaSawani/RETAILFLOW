using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RetailFlow.Models;

namespace RetailFlow.Services;

public static class ReceiptPrintHelper
{
    public static FrameworkElement CreatePrintableReceiptVisual(SaleReceipt receipt, double printableWidth)
    {
        double contentWidth = Math.Min(printableWidth > 0 ? printableWidth : 400, 400);

        var border = new Border
        {
            Width = contentWidth,
            Background = Brushes.White,
            Padding = new Thickness(20, 18, 20, 18),
            BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6)
        };

        var stack = new StackPanel();

        // 1. Store Header
        stack.Children.Add(new TextBlock
        {
            Text = "RETAILFLOW STORE",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        stack.Children.Add(new TextBlock
        {
            Text = "Retail Management & POS • Official Sales Receipt",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 10)
        });

        stack.Children.Add(CreateDivider());

        // 2. Metadata Grid
        var metaGrid = new Grid { Margin = new Thickness(0, 6, 0, 6) };
        metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var metaLeft = new StackPanel();
        metaLeft.Children.Add(new TextBlock { Text = $"Invoice: #{receipt.InvoiceNumber}", FontWeight = FontWeights.Bold, FontSize = 12 });
        metaLeft.Children.Add(new TextBlock { Text = $"Date: {receipt.SaleDate.ToLocalTime():yyyy-MM-dd HH:mm}", FontSize = 11, Foreground = Brushes.DarkSlateGray, Margin = new Thickness(0, 2, 0, 0) });
        metaLeft.Children.Add(new TextBlock { Text = $"Cashier: {receipt.CashierName}", FontSize = 11, Foreground = Brushes.DarkSlateGray, Margin = new Thickness(0, 2, 0, 0) });
        Grid.SetColumn(metaLeft, 0);
        metaGrid.Children.Add(metaLeft);

        var metaRight = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
        metaRight.Children.Add(new TextBlock { Text = $"Payment: {receipt.PaymentMethod}", FontWeight = FontWeights.SemiBold, FontSize = 11, TextAlignment = TextAlignment.Right });
        if (!string.IsNullOrWhiteSpace(receipt.CustomerName))
        {
            metaRight.Children.Add(new TextBlock { Text = $"Customer: {receipt.CustomerName}", FontSize = 11, Foreground = Brushes.DarkSlateGray, TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 2, 0, 0) });
        }
        if (!string.IsNullOrWhiteSpace(receipt.CustomerPhone))
        {
            metaRight.Children.Add(new TextBlock { Text = $"Phone: {receipt.CustomerPhone}", FontSize = 10, Foreground = Brushes.Gray, TextAlignment = TextAlignment.Right });
        }
        Grid.SetColumn(metaRight, 1);
        metaGrid.Children.Add(metaRight);

        stack.Children.Add(metaGrid);
        stack.Children.Add(CreateDivider());

        // 3. Items Table Header
        var itemsHeader = new Grid { Margin = new Thickness(0, 6, 0, 4) };
        itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });
        itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        var thItem = new TextBlock { Text = "ITEM", FontWeight = FontWeights.Bold, FontSize = 11, Foreground = Brushes.DimGray };
        Grid.SetColumn(thItem, 0);
        itemsHeader.Children.Add(thItem);

        var thQty = new TextBlock { Text = "QTY", FontWeight = FontWeights.Bold, FontSize = 11, Foreground = Brushes.DimGray, TextAlignment = TextAlignment.Center };
        Grid.SetColumn(thQty, 1);
        itemsHeader.Children.Add(thQty);

        var thPrice = new TextBlock { Text = "PRICE", FontWeight = FontWeights.Bold, FontSize = 11, Foreground = Brushes.DimGray, TextAlignment = TextAlignment.Right };
        Grid.SetColumn(thPrice, 2);
        itemsHeader.Children.Add(thPrice);

        var thTotal = new TextBlock { Text = "TOTAL", FontWeight = FontWeights.Bold, FontSize = 11, Foreground = Brushes.DimGray, TextAlignment = TextAlignment.Right };
        Grid.SetColumn(thTotal, 3);
        itemsHeader.Children.Add(thTotal);

        stack.Children.Add(itemsHeader);
        stack.Children.Add(CreateDivider());

        // 4. Line Items
        foreach (var item in receipt.Items)
        {
            var itemGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var nameBlock = new TextBlock { Text = item.ProductName, FontSize = 12, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
            Grid.SetColumn(nameBlock, 0);
            itemGrid.Children.Add(nameBlock);

            var qtyBlock = new TextBlock { Text = item.Quantity.ToString(), FontSize = 12, TextAlignment = TextAlignment.Center };
            Grid.SetColumn(qtyBlock, 1);
            itemGrid.Children.Add(qtyBlock);

            var priceBlock = new TextBlock { Text = $"Rs.{item.UnitPrice:N2}", FontSize = 11, TextAlignment = TextAlignment.Right, Foreground = Brushes.DarkSlateGray };
            Grid.SetColumn(priceBlock, 2);
            itemGrid.Children.Add(priceBlock);

            var totalBlock = new TextBlock { Text = $"Rs.{item.LineTotal:N2}", FontSize = 12, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right, Foreground = new SolidColorBrush(Color.FromRgb(15, 118, 110)) };
            Grid.SetColumn(totalBlock, 3);
            itemGrid.Children.Add(totalBlock);

            stack.Children.Add(itemGrid);
        }

        stack.Children.Add(CreateDivider());

        // 5. Totals
        var totalsStack = new StackPanel { Margin = new Thickness(0, 6, 0, 6) };

        totalsStack.Children.Add(CreateRow("Subtotal:", $"Rs. {receipt.Subtotal:N2}", false));
        if (receipt.Discount > 0)
        {
            totalsStack.Children.Add(CreateRow("Discount:", $"- Rs. {receipt.Discount:N2}", false, Brushes.Crimson));
        }

        var netTotalGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        netTotalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        netTotalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        var lblNet = new TextBlock { Text = "TOTAL:", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)) };
        Grid.SetColumn(lblNet, 0);
        netTotalGrid.Children.Add(lblNet);
        var valNet = new TextBlock { Text = $"Rs. {receipt.Total:N2}", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61)) };
        Grid.SetColumn(valNet, 1);
        netTotalGrid.Children.Add(valNet);
        totalsStack.Children.Add(netTotalGrid);

        if (receipt.PaymentMethod == "Cash" && receipt.AmountTendered > 0)
        {
            totalsStack.Children.Add(CreateRow("Cash Tendered:", $"Rs. {receipt.AmountTendered:N2}", false));
            totalsStack.Children.Add(CreateRow("Change Returned:", $"Rs. {receipt.ChangeDue:N2}", true, new SolidColorBrush(Color.FromRgb(21, 128, 61))));
        }

        stack.Children.Add(totalsStack);

        // 6. Loyalty Rewards
        if (receipt.LoyaltyPointsEarned > 0 || receipt.LoyaltyPointsRedeemed > 0)
        {
            stack.Children.Add(CreateDivider());
            var loyaltyBox = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 253, 244)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(187, 247, 208)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 4, 0, 4)
            };
            var loyaltyStack = new StackPanel();
            loyaltyStack.Children.Add(new TextBlock { Text = "★ LOYALTY REWARDS SUMMARY", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52)), Margin = new Thickness(0, 0, 0, 4) });

            if (receipt.LoyaltyPointsRedeemed > 0)
            {
                loyaltyStack.Children.Add(CreateRow("Points Redeemed:", $"-{receipt.LoyaltyPointsRedeemed} pts", false, Brushes.Crimson));
            }
            if (receipt.LoyaltyPointsEarned > 0)
            {
                loyaltyStack.Children.Add(CreateRow("Points Earned:", $"+{receipt.LoyaltyPointsEarned} pts", false, new SolidColorBrush(Color.FromRgb(22, 101, 52))));
            }
            loyaltyStack.Children.Add(CreateRow("Current Balance:", $"{receipt.CustomerPointsBalance} pts", true, new SolidColorBrush(Color.FromRgb(37, 99, 235))));
            loyaltyBox.Child = loyaltyStack;
            stack.Children.Add(loyaltyBox);
        }

        // 7. Barcode Code 128
        stack.Children.Add(CreateDivider());
        var barcodeBorder = new Border
        {
            Margin = new Thickness(0, 8, 0, 2),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        try
        {
            var barcodeService = new BarcodeService();
            var barcodeImg = new Image
            {
                Source = barcodeService.GenerateCode128Barcode(receipt.InvoiceNumber, 240, 45),
                Width = 240,
                Height = 45,
                Stretch = Stretch.Uniform
            };
            barcodeBorder.Child = barcodeImg;
        }
        catch
        {
            barcodeBorder.Child = new TextBlock
            {
                Text = $"||||| | |||| ||| ||||| {receipt.InvoiceNumber} |||||",
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }
        stack.Children.Add(barcodeBorder);

        var barcodeTxt = new TextBlock
        {
            Text = $"*{receipt.InvoiceNumber}*",
            FontFamily = new FontFamily("Consolas, Courier New"),
            FontSize = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.DarkSlateGray,
            Margin = new Thickness(0, 0, 0, 8)
        };
        stack.Children.Add(barcodeTxt);

        // 8. Footer
        stack.Children.Add(CreateDivider());
        var footer = new TextBlock
        {
            Text = "Thank you for shopping at RetailFlow!\nPlease retain this receipt for warranty and returns.",
            FontSize = 10,
            Foreground = Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0)
        };
        stack.Children.Add(footer);

        border.Child = stack;

        // Perform measure and arrange pass
        border.Measure(new Size(contentWidth, double.PositiveInfinity));
        border.Arrange(new Rect(new Point(Math.Max(0, (printableWidth - contentWidth) / 2), 10), border.DesiredSize));
        border.UpdateLayout();

        return border;
    }

    private static Border CreateDivider()
    {
        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Margin = new Thickness(0, 4, 0, 4)
        };
    }

    private static Grid CreateRow(string label, string value, bool bold, Brush? valBrush = null)
    {
        var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

        var lbl = new TextBlock { Text = label, FontSize = 12, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal, Foreground = Brushes.DarkSlateGray };
        Grid.SetColumn(lbl, 0);
        grid.Children.Add(lbl);

        var val = new TextBlock { Text = value, FontSize = 12, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal, Foreground = valBrush ?? Brushes.Black };
        Grid.SetColumn(val, 1);
        grid.Children.Add(val);

        return grid;
    }

    public static string ExportHtmlInvoice(SaleReceipt receipt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\" />");
        sb.AppendLine($"<title>Invoice #{receipt.InvoiceNumber} - RetailFlow</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("  * { box-sizing: border-box; }");
        sb.AppendLine("  body { font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, Arial, sans-serif; background: #f8fafc; margin: 0; padding: 25px; display: flex; flex-direction: column; align-items: center; color: #1e293b; }");
        sb.AppendLine("  .toolbar { margin-bottom: 16px; display: flex; gap: 10px; }");
        sb.AppendLine("  .btn { padding: 10px 20px; font-size: 14px; font-weight: bold; border-radius: 6px; border: none; cursor: pointer; }");
        sb.AppendLine("  .btn-print { background: #0f766e; color: white; }");
        sb.AppendLine("  .btn-print:hover { background: #0d9488; }");
        sb.AppendLine("  .receipt { width: 420px; background: white; border: 1px solid #e2e8f0; border-radius: 8px; padding: 24px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); }");
        sb.AppendLine("  .header { text-align: center; border-bottom: 2px dashed #cbd5e1; padding-bottom: 12px; margin-bottom: 12px; }");
        sb.AppendLine("  .header h1 { margin: 0; font-size: 20px; color: #0f172a; }");
        sb.AppendLine("  .header p { margin: 4px 0 0 0; font-size: 12px; color: #64748b; }");
        sb.AppendLine("  .meta { font-size: 12px; border-bottom: 1px solid #f1f5f9; padding-bottom: 8px; margin-bottom: 12px; }");
        sb.AppendLine("  .meta-row { display: flex; justify-content: space-between; margin: 3px 0; }");
        sb.AppendLine("  table { width: 100%; border-collapse: collapse; font-size: 12px; margin-bottom: 12px; }");
        sb.AppendLine("  th { text-align: left; padding: 6px 0; border-bottom: 2px solid #e2e8f0; color: #475569; font-size: 11px; }");
        sb.AppendLine("  td { padding: 6px 0; border-bottom: 1px solid #f1f5f9; }");
        sb.AppendLine("  .text-right { text-align: right; }");
        sb.AppendLine("  .text-center { text-align: center; }");
        sb.AppendLine("  .totals { border-top: 1px solid #e2e8f0; padding-top: 10px; font-size: 12px; }");
        sb.AppendLine("  .total-row { display: flex; justify-content: space-between; margin: 4px 0; }");
        sb.AppendLine("  .net-total { font-size: 16px; font-weight: bold; color: #15803d; border-top: 2px dashed #cbd5e1; padding-top: 8px; margin-top: 6px; }");
        sb.AppendLine("  .loyalty { background: #f0fdf4; border: 1px solid #86efac; border-radius: 6px; padding: 8px 10px; margin: 12px 0; font-size: 11px; }");
        sb.AppendLine("  .footer { text-align: center; font-size: 11px; color: #64748b; border-top: 2px dashed #cbd5e1; padding-top: 12px; margin-top: 14px; }");
        sb.AppendLine("  @media print {");
        sb.AppendLine("    body { background: white; padding: 0; }");
        sb.AppendLine("    .toolbar { display: none; }");
        sb.AppendLine("    .receipt { border: none; box-shadow: none; width: 100%; padding: 0; }");
        sb.AppendLine("  }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"toolbar\">");
        sb.AppendLine("    <button class=\"btn btn-print\" onclick=\"window.print()\">🖨️ Print / Save as PDF (Ctrl+P)</button>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <div class=\"receipt\">");
        sb.AppendLine("    <div class=\"header\">");
        sb.AppendLine("      <h1>RETAILFLOW STORE</h1>");
        sb.AppendLine("      <p>Retail Management & POS • Official Sales Receipt</p>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div class=\"meta\">");
        sb.AppendLine($"      <div class=\"meta-row\"><strong>Invoice: #{receipt.InvoiceNumber}</strong><span>Payment: {receipt.PaymentMethod}</span></div>");
        sb.AppendLine($"      <div class=\"meta-row\"><span>Date: {receipt.SaleDate.ToLocalTime():yyyy-MM-dd HH:mm}</span><span>Cashier: {receipt.CashierName}</span></div>");
        if (!string.IsNullOrWhiteSpace(receipt.CustomerName))
        {
            sb.AppendLine($"      <div class=\"meta-row\"><span>Customer: {receipt.CustomerName}</span><span>Phone: {receipt.CustomerPhone ?? "-"}</span></div>");
        }
        sb.AppendLine("    </div>");
        sb.AppendLine("    <table>");
        sb.AppendLine("      <thead><tr><th>Item</th><th class=\"text-center\">Qty</th><th class=\"text-right\">Price</th><th class=\"text-right\">Total</th></tr></thead>");
        sb.AppendLine("      <tbody>");
        foreach (var itm in receipt.Items)
        {
            sb.AppendLine($"        <tr><td>{itm.ProductName}</td><td class=\"text-center\">{itm.Quantity}</td><td class=\"text-right\">Rs. {itm.UnitPrice:N2}</td><td class=\"text-right\">Rs. {itm.LineTotal:N2}</td></tr>");
        }
        sb.AppendLine("      </tbody>");
        sb.AppendLine("    </table>");
        sb.AppendLine("    <div class=\"totals\">");
        sb.AppendLine($"      <div class=\"total-row\"><span>Subtotal:</span><span>Rs. {receipt.Subtotal:N2}</span></div>");
        if (receipt.Discount > 0)
        {
            sb.AppendLine($"      <div class=\"total-row\" style=\"color: #dc2626;\"><span>Discount:</span><span>- Rs. {receipt.Discount:N2}</span></div>");
        }
        sb.AppendLine($"      <div class=\"total-row net-total\"><span>TOTAL:</span><span>Rs. {receipt.Total:N2}</span></div>");
        if (receipt.PaymentMethod == "Cash" && receipt.AmountTendered > 0)
        {
            sb.AppendLine($"      <div class=\"total-row\"><span>Cash Tendered:</span><span>Rs. {receipt.AmountTendered:N2}</span></div>");
            sb.AppendLine($"      <div class=\"total-row\" style=\"color: #15803d; font-weight: bold;\"><span>Change Returned:</span><span>Rs. {receipt.ChangeDue:N2}</span></div>");
        }
        sb.AppendLine("    </div>");

        if (receipt.LoyaltyPointsEarned > 0 || receipt.LoyaltyPointsRedeemed > 0)
        {
            sb.AppendLine("    <div class=\"loyalty\">");
            sb.AppendLine("      <strong>★ Customer Loyalty Rewards</strong>");
            if (receipt.LoyaltyPointsRedeemed > 0)
            {
                sb.AppendLine($"      <div>Points Redeemed: <strong>-{receipt.LoyaltyPointsRedeemed} pts</strong></div>");
            }
            if (receipt.LoyaltyPointsEarned > 0)
            {
                sb.AppendLine($"      <div>Points Earned: <strong>+{receipt.LoyaltyPointsEarned} pts</strong></div>");
            }
            sb.AppendLine($"      <div>Updated Balance: <strong>{receipt.CustomerPointsBalance} pts</strong></div>");
            sb.AppendLine("    </div>");
        }

        sb.AppendLine("    <div class=\"footer\">");
        sb.AppendLine($"      <p style=\"font-family: monospace; font-size: 14px; letter-spacing: 2px;\">*{receipt.InvoiceNumber}*</p>");
        sb.AppendLine("      <p>Thank you for shopping with us! Please come again.</p>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        string tempPath = Path.Combine(Path.GetTempPath(), $"Receipt_{receipt.InvoiceNumber}.html");
        File.WriteAllText(tempPath, sb.ToString(), Encoding.UTF8);
        return tempPath;
    }
}
