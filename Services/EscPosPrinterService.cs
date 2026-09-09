using RetailFlow.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace RetailFlow.Services;

public class EscPosPrinterService
{
    // Win32 Spooler P/Invoke for direct raw byte printing
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)] public string? pDocName;
        [MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)] public string? pDataType;
    }

    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

    [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    public string SelectedPrinterName { get; set; } = "Virtual ESC/POS Simulator";

    public List<string> GetAvailablePrinters()
    {
        var printers = new List<string> { "Virtual ESC/POS Simulator" };

        try
        {
            var printServer = new System.Printing.LocalPrintServer();
            foreach (var queue in printServer.GetPrintQueues())
            {
                printers.Add(queue.Name);
            }
        }
        catch
        {
            // Fallback if local print server is restricted
        }

        return printers;
    }

    public byte[] BuildEscPosBytes(SaleReceipt receipt)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // ESC @ : Initialize printer
        writer.Write(new byte[] { 0x1B, 0x40 });

        // Center Align
        writer.Write(new byte[] { 0x1B, 0x61, 0x01 });

        // Double Height + Double Width for Title
        writer.Write(new byte[] { 0x1B, 0x21, 0x30 });
        WriteAscii(writer, "RETAILFLOW STORE\n");

        // Normal text size
        writer.Write(new byte[] { 0x1B, 0x21, 0x00 });
        WriteAscii(writer, "Modern Retail & POS System\n");
        WriteAscii(writer, "Tel: +94 11 234 5678\n");
        WriteAscii(writer, "------------------------------------------\n");

        // Left Align
        writer.Write(new byte[] { 0x1B, 0x61, 0x00 });
        WriteAscii(writer, $"Invoice : #{receipt.InvoiceNumber}\n");
        WriteAscii(writer, $"Date    : {receipt.SaleDate.ToLocalTime():yyyy-MM-dd hh:mm:ss tt}\n");
        WriteAscii(writer, $"Cashier : {receipt.CashierName}\n");

        if (!string.IsNullOrWhiteSpace(receipt.CustomerName))
        {
            WriteAscii(writer, $"Customer: {receipt.CustomerName} ({receipt.CustomerPhone})\n");
        }

        WriteAscii(writer, "==========================================\n");
        WriteAscii(writer, string.Format("{0,-18} {1,4} {2,8} {3,9}\n", "ITEM", "QTY", "PRICE", "TOTAL"));
        WriteAscii(writer, "------------------------------------------\n");

        foreach (var item in receipt.Items)
        {
            string name = item.ProductName.Length > 18 ? item.ProductName.Substring(0, 18) : item.ProductName;
            WriteAscii(writer, string.Format("{0,-18} {1,4} {2,8:N2} {3,9:N2}\n", name, item.Quantity, item.UnitPrice, item.LineTotal));
        }

        WriteAscii(writer, "------------------------------------------\n");

        // Right Align for Totals
        writer.Write(new byte[] { 0x1B, 0x61, 0x02 });
        WriteAscii(writer, $"Subtotal: Rs. {receipt.Subtotal:N2}\n");

        if (receipt.Discount > 0)
        {
            WriteAscii(writer, $"Discount: -Rs. {receipt.Discount:N2}\n");
        }

        // Bold Total
        writer.Write(new byte[] { 0x1B, 0x45, 0x01 });
        WriteAscii(writer, $"NET TOTAL: Rs. {receipt.Total:N2}\n");
        writer.Write(new byte[] { 0x1B, 0x45, 0x00 });

        WriteAscii(writer, $"Payment ({receipt.PaymentMethod}): Rs. {receipt.AmountTendered:N2}\n");
        if (receipt.ChangeDue > 0)
        {
            WriteAscii(writer, $"Change Due: Rs. {receipt.ChangeDue:N2}\n");
        }

        // Customer Loyalty points section if applicable
        if (receipt.LoyaltyPointsEarned > 0 || receipt.LoyaltyPointsRedeemed > 0)
        {
            writer.Write(new byte[] { 0x1B, 0x61, 0x01 });
            WriteAscii(writer, "------------------------------------------\n");
            WriteAscii(writer, "*** LOYALTY REWARDS ***\n");
            if (receipt.LoyaltyPointsRedeemed > 0)
                WriteAscii(writer, $"Points Redeemed : {receipt.LoyaltyPointsRedeemed} pts (-Rs. {receipt.LoyaltyPointsRedeemed:N2})\n");
            if (receipt.LoyaltyPointsEarned > 0)
                WriteAscii(writer, $"Points Earned   : +{receipt.LoyaltyPointsEarned} pts\n");
            WriteAscii(writer, $"Updated Balance : {receipt.CustomerPointsBalance} pts\n");
        }

        // Footer
        writer.Write(new byte[] { 0x1B, 0x61, 0x01 });
        WriteAscii(writer, "==========================================\n");
        WriteAscii(writer, "Thank You for Shopping With Us!\n");
        WriteAscii(writer, "Please come again.\n\n\n\n");

        // GS V 66 0 : Partial Paper Cut
        writer.Write(new byte[] { 0x1D, 0x56, 0x42, 0x00 });

        return ms.ToArray();
    }

    private static void WriteAscii(BinaryWriter writer, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        writer.Write(bytes);
    }

    public (bool Success, string Message) PrintReceipt(SaleReceipt receipt)
    {
        if (SelectedPrinterName == "Virtual ESC/POS Simulator" || string.IsNullOrWhiteSpace(SelectedPrinterName))
        {
            return (true, "Printed to Virtual ESC/POS Simulator. Ticket generated.");
        }

        byte[] rawBytes = BuildEscPosBytes(receipt);
        return SendBytesToPrinter(SelectedPrinterName, rawBytes, $"Receipt-{receipt.InvoiceNumber}");
    }

    public (bool Success, string Message) TestPrint()
    {
        if (SelectedPrinterName == "Virtual ESC/POS Simulator" || string.IsNullOrWhiteSpace(SelectedPrinterName))
        {
            return (true, "Virtual ESC/POS Thermal Printer test passed. Ready.");
        }

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(new byte[] { 0x1B, 0x40 }); // Init
        writer.Write(new byte[] { 0x1B, 0x61, 0x01 }); // Center
        WriteAscii(writer, "RETAILFLOW ESC/POS TEST\n");
        WriteAscii(writer, "Printer connection OK!\n");
        WriteAscii(writer, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n\n\n");
        writer.Write(new byte[] { 0x1D, 0x56, 0x42, 0x00 }); // Cut

        return SendBytesToPrinter(SelectedPrinterName, ms.ToArray(), "RetailFlow-TestPrint");
    }

    public (bool Success, string Message) KickCashDrawer()
    {
        if (SelectedPrinterName == "Virtual ESC/POS Simulator" || string.IsNullOrWhiteSpace(SelectedPrinterName))
        {
            return (true, "Virtual Cash Drawer Kick pulse simulated.");
        }

        // ESC p m t1 t2 : Generate pulse to open cash drawer
        byte[] drawerBytes = new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA };
        return SendBytesToPrinter(SelectedPrinterName, drawerBytes, "RetailFlow-OpenDrawer");
    }

    public string GenerateSimulatorText(SaleReceipt receipt)
    {
        var sb = new StringBuilder();
        string divDouble = "==========================================";
        string divSingle = "------------------------------------------";

        sb.AppendLine(divDouble);
        sb.AppendLine("             RETAILFLOW STORE             ");
        sb.AppendLine("        Modern Retail & POS System        ");
        sb.AppendLine("           Tel: +94 11 234 5678           ");
        sb.AppendLine(divDouble);
        sb.AppendLine($" Invoice : #{receipt.InvoiceNumber}");
        sb.AppendLine($" Date    : {receipt.SaleDate.ToLocalTime():yyyy-MM-dd hh:mm tt}");
        sb.AppendLine($" Cashier : {receipt.CashierName}");
        if (!string.IsNullOrWhiteSpace(receipt.CustomerName))
        {
            sb.AppendLine($" Customer: {receipt.CustomerName}");
            if (!string.IsNullOrWhiteSpace(receipt.CustomerPhone))
            {
                sb.AppendLine($" Phone   : {receipt.CustomerPhone}");
            }
        }
        sb.AppendLine(divSingle);
        sb.AppendLine(" ITEM                QTY    PRICE     TOTAL ");
        sb.AppendLine(divSingle);
        foreach (var item in receipt.Items)
        {
            string name = item.ProductName.Length > 18 ? item.ProductName.Substring(0, 18) : item.ProductName;
            sb.AppendLine($" {name,-18} {item.Quantity,3} {item.UnitPrice,8:N2} {item.LineTotal,9:N2}");
        }
        sb.AppendLine(divSingle);

        // Perfectly aligned two-column financial summary
        sb.AppendLine(FormatTwoColumns(" Subtotal:", $"Rs. {receipt.Subtotal:N2}"));
        if (receipt.Discount > 0)
        {
            sb.AppendLine(FormatTwoColumns(" Discount:", $"- Rs. {receipt.Discount:N2}"));
        }
        sb.AppendLine(FormatTwoColumns(" NET TOTAL:", $"Rs. {receipt.Total:N2}"));

        string paymentLabel = string.IsNullOrWhiteSpace(receipt.PaymentMethod) ? "Cash" : receipt.PaymentMethod.Trim();
        sb.AppendLine(FormatTwoColumns($" Paid ({paymentLabel}):", $"Rs. {receipt.AmountTendered:N2}"));

        if (receipt.ChangeDue > 0)
        {
            sb.AppendLine(FormatTwoColumns(" Change Due:", $"Rs. {receipt.ChangeDue:N2}"));
        }

        if (receipt.LoyaltyPointsEarned > 0 || receipt.LoyaltyPointsRedeemed > 0)
        {
            sb.AppendLine(divSingle);
            sb.AppendLine("             LOYALTY REWARDS              ");
            if (receipt.LoyaltyPointsRedeemed > 0)
                sb.AppendLine(FormatTwoColumns(" Points Redeemed:", $"-{receipt.LoyaltyPointsRedeemed} pts"));
            if (receipt.LoyaltyPointsEarned > 0)
                sb.AppendLine(FormatTwoColumns(" Points Earned:", $"+{receipt.LoyaltyPointsEarned} pts"));
            sb.AppendLine(FormatTwoColumns(" Current Balance:", $"{receipt.CustomerPointsBalance} pts"));
        }

        sb.AppendLine(divDouble);
        sb.AppendLine("     Thank You for Shopping With Us!      ");
        sb.AppendLine("           Please Come Again!             ");
        sb.AppendLine(divDouble);
        sb.AppendLine("             ✂ [PAPER CUT] ✂              ");

        return sb.ToString();
    }

    private static string FormatTwoColumns(string left, string right, int totalWidth = 42)
    {
        int spaces = totalWidth - left.Length - right.Length;
        if (spaces < 1) spaces = 1;
        return left + new string(' ', spaces) + right;
    }

    private static (bool Success, string Message) SendBytesToPrinter(string szPrinterName, byte[] pBytes, string docName)
    {
        IntPtr pUnmanagedBytes = Marshal.AllocCoTaskMem(pBytes.Length);
        Marshal.Copy(pBytes, 0, pUnmanagedBytes, pBytes.Length);

        DOCINFOA di = new()
        {
            pDocName = docName,
            pDataType = "RAW"
        };

        bool success = false;
        string message = "Unknown printer error";

        if (OpenPrinter(szPrinterName, out IntPtr hPrinter, IntPtr.Zero))
        {
            if (StartDocPrinter(hPrinter, 1, di))
            {
                if (StartPagePrinter(hPrinter))
                {
                    success = WritePrinter(hPrinter, pUnmanagedBytes, pBytes.Length, out int dwWritten);
                    EndPagePrinter(hPrinter);

                    if (success)
                        message = $"Receipt sent directly to thermal printer '{szPrinterName}'.";
                    else
                        message = $"Write error on printer '{szPrinterName}'. Code: {Marshal.GetLastWin32Error()}";
                }
                EndDocPrinter(hPrinter);
            }
            ClosePrinter(hPrinter);
        }
        else
        {
            message = $"Cannot open printer '{szPrinterName}'. Verify it is online and connected.";
        }

        Marshal.FreeCoTaskMem(pUnmanagedBytes);
        return (success, message);
    }
}
