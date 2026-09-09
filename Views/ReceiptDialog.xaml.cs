using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            string htmlPath = ExportHtmlInvoice(vm);
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
                var printableVisual = CreatePrintableReceiptVisual(vm, printDlg.PrintableAreaWidth);
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
                string htmlPath = ExportHtmlInvoice(vm);
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

    private FrameworkElement CreatePrintableReceiptVisual(ReceiptDialogViewModel vm, double printableWidth)
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
        metaLeft.Children.Add(new TextBlock { Text = $"Invoice: #{vm.InvoiceNumber}", FontWeight = FontWeights.Bold, FontSize = 12 });
        metaLeft.Children.Add(new TextBlock { Text = $"Date: {vm.SaleDate:yyyy-MM-dd HH:mm}", FontSize = 11, Foreground = Brushes.DarkSlateGray, Margin = new Thickness(0, 2, 0, 0) });
        metaLeft.Children.Add(new TextBlock { Text = $"Cashier: {vm.CashierName}", FontSize = 11, Foreground = Brushes.DarkSlateGray, Margin = new Thickness(0, 2, 0, 0) });
        Grid.SetColumn(metaLeft, 0);
        metaGrid.Children.Add(metaLeft);

        var metaRight = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
        metaRight.Children.Add(new TextBlock { Text = $"Payment: {vm.PaymentMethod}", FontWeight = FontWeights.SemiBold, FontSize = 11, TextAlignment = TextAlignment.Right });
        if (!string.IsNullOrWhiteSpace(vm.CustomerName))
        {
            metaRight.Children.Add(new TextBlock { Text = $"Customer: {vm.CustomerName}", FontSize = 11, Foreground = Brushes.DarkSlateGray, TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 2, 0, 0) });
            if (!string.IsNullOrWhiteSpace(vm.CustomerPhone))
            {
                metaRight.Children.Add(new TextBlock { Text = $"Phone: {vm.CustomerPhone}", FontSize = 10, Foreground = Brushes.Gray, TextAlignment = TextAlignment.Right });
            }
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

        // 4. Line Items (No scrollviewer - all printed cleanly)
        foreach (var item in vm.Items)
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

        totalsStack.Children.Add(CreateRow("Subtotal:", $"Rs. {vm.Subtotal:N2}", false));
        if (vm.Discount > 0)
        {
            totalsStack.Children.Add(CreateRow("Discount:", $"- Rs. {vm.Discount:N2}", false, Brushes.Crimson));
        }

        var netTotalGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        netTotalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        netTotalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        var lblNet = new TextBlock { Text = "TOTAL:", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)) };
        Grid.SetColumn(lblNet, 0);
        netTotalGrid.Children.Add(lblNet);
        var valNet = new TextBlock { Text = $"Rs. {vm.Total:N2}", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61)) };
        Grid.SetColumn(valNet, 1);
        netTotalGrid.Children.Add(valNet);
        totalsStack.Children.Add(netTotalGrid);

        if (vm.PaymentMethod == "Cash" && vm.AmountTendered > 0)
        {
            totalsStack.Children.Add(CreateRow("Amount Tendered:", $"Rs. {vm.AmountTendered:N2}", false));
            totalsStack.Children.Add(CreateRow("Change Due:", $"Rs. {vm.ChangeDue:N2}", true, new SolidColorBrush(Color.FromRgb(21, 128, 61))));
        }

        stack.Children.Add(totalsStack);

        // 6. Loyalty Banner (if applicable)
        if (vm.HasLoyalty)
        {
            var loyaltyBox = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 253, 244)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(134, 239, 172)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 4, 0, 6)
            };
            var lStack = new StackPanel();
            if (vm.LoyaltyPointsEarned > 0)
            {
                lStack.Children.Add(new TextBlock { Text = $"⭐ Loyalty Points Earned: +{vm.LoyaltyPointsEarned} pts", FontWeight = FontWeights.Bold, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61)) });
            }
            if (vm.LoyaltyPointsRedeemed > 0)
            {
                lStack.Children.Add(new TextBlock { Text = $"⭐ Points Redeemed: -{vm.LoyaltyPointsRedeemed} pts (Rs. {vm.LoyaltyPointsRedeemed:N2} discount)", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(15, 118, 110)) });
            }
            if (vm.CustomerPointsBalance > 0)
            {
                lStack.Children.Add(new TextBlock { Text = $"Current Balance: {vm.CustomerPointsBalance} pts", FontSize = 10, Foreground = Brushes.DarkSlateGray });
            }
            loyaltyBox.Child = lStack;
            stack.Children.Add(loyaltyBox);
        }

        // 7. Barcode of Invoice
        var barcodeSvc = new BarcodeService();
        var barcodeImg = new Image
        {
            Source = barcodeSvc.GenerateCode128Barcode(vm.InvoiceNumber, 240, 36),
            Height = 36,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 2)
        };
        stack.Children.Add(barcodeImg);

        var barcodeTxt = new TextBlock
        {
            Text = $"*{vm.InvoiceNumber}*",
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

    private static string ExportHtmlInvoice(ReceiptDialogViewModel vm)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\" />");
        sb.AppendLine($"<title>Invoice #{vm.InvoiceNumber} - RetailFlow</title>");
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
        sb.AppendLine($"      <div class=\"meta-row\"><strong>Invoice: #{vm.InvoiceNumber}</strong><span>Payment: {vm.PaymentMethod}</span></div>");
        sb.AppendLine($"      <div class=\"meta-row\"><span>Date: {vm.SaleDate:yyyy-MM-dd HH:mm}</span><span>Cashier: {vm.CashierName}</span></div>");
        if (!string.IsNullOrWhiteSpace(vm.CustomerName))
        {
            sb.AppendLine($"      <div class=\"meta-row\"><span>Customer: {vm.CustomerName}</span><span>{vm.CustomerPhone}</span></div>");
        }
        sb.AppendLine("    </div>");
        sb.AppendLine("    <table>");
        sb.AppendLine("      <thead>");
        sb.AppendLine("        <tr><th>ITEM</th><th class=\"text-center\">QTY</th><th class=\"text-right\">UNIT</th><th class=\"text-right\">TOTAL</th></tr>");
        sb.AppendLine("      </thead>");
        sb.AppendLine("      <tbody>");
        foreach (var item in vm.Items)
        {
            sb.AppendLine($"        <tr><td><strong>{item.ProductName}</strong></td><td class=\"text-center\">{item.Quantity}</td><td class=\"text-right\">Rs.{item.UnitPrice:N2}</td><td class=\"text-right\"><strong>Rs.{item.LineTotal:N2}</strong></td></tr>");
        }
        sb.AppendLine("      </tbody>");
        sb.AppendLine("    </table>");
        sb.AppendLine("    <div class=\"totals\">");
        sb.AppendLine($"      <div class=\"total-row\"><span>Subtotal:</span><span>Rs. {vm.Subtotal:N2}</span></div>");
        if (vm.Discount > 0)
        {
            sb.AppendLine($"      <div class=\"total-row\" style=\"color:#dc2626;\"><span>Discount:</span><span>- Rs. {vm.Discount:N2}</span></div>");
        }
        sb.AppendLine($"      <div class=\"total-row net-total\"><span>NET TOTAL:</span><span>Rs. {vm.Total:N2}</span></div>");
        if (vm.PaymentMethod == "Cash" && vm.AmountTendered > 0)
        {
            sb.AppendLine($"      <div class=\"total-row\"><span>Amount Tendered:</span><span>Rs. {vm.AmountTendered:N2}</span></div>");
            sb.AppendLine($"      <div class=\"total-row\" style=\"color:#15803d;font-weight:bold;\"><span>Change Due:</span><span>Rs. {vm.ChangeDue:N2}</span></div>");
        }
        sb.AppendLine("    </div>");
        if (vm.HasLoyalty)
        {
            sb.AppendLine("    <div class=\"loyalty\">");
            if (vm.LoyaltyPointsEarned > 0)
            {
                sb.AppendLine($"      <div>⭐ <strong>Points Earned: +{vm.LoyaltyPointsEarned} pts</strong></div>");
            }
            if (vm.LoyaltyPointsRedeemed > 0)
            {
                sb.AppendLine($"      <div>⭐ Points Redeemed: -{vm.LoyaltyPointsRedeemed} pts (Rs. {vm.LoyaltyPointsRedeemed:N2} discount)</div>");
            }
            if (vm.CustomerPointsBalance > 0)
            {
                sb.AppendLine($"      <div>Updated Balance: {vm.CustomerPointsBalance} pts</div>");
            }
            sb.AppendLine("    </div>");
        }
        sb.AppendLine("    <div class=\"footer\">");
        sb.AppendLine("      <p>Thank you for shopping at RetailFlow!</p>");
        sb.AppendLine("      <p style=\"font-size:10px;\">RetailFlow Retail Management System</p>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Receipt_{vm.InvoiceNumber}.html");
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        return filePath;
    }
}
