using RetailFlow.Commands;
using RetailFlow.Models;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace RetailFlow.ViewModels;

public class ReceiptDialogViewModel : ViewModelBase
{
    private readonly SaleReceipt _receipt;

    public string InvoiceNumber => _receipt.InvoiceNumber;
    public DateTime SaleDate => _receipt.SaleDate;
    public List<SaleReceiptItem> Items => _receipt.Items;
    public decimal Subtotal => _receipt.Subtotal;
    public decimal Discount => _receipt.Discount;
    public decimal Total => _receipt.Total;
    public string PaymentMethod => _receipt.PaymentMethod;
    public decimal AmountTendered => _receipt.AmountTendered;
    public decimal ChangeDue => _receipt.ChangeDue;

    public string CashierName => _receipt.CashierName;
    public string? CustomerName => _receipt.CustomerName;
    public string? CustomerPhone => _receipt.CustomerPhone;
    public int LoyaltyPointsEarned => _receipt.LoyaltyPointsEarned;
    public int LoyaltyPointsRedeemed => _receipt.LoyaltyPointsRedeemed;
    public int CustomerPointsBalance => _receipt.CustomerPointsBalance;
    public bool HasLoyalty => !string.IsNullOrWhiteSpace(CustomerName) || LoyaltyPointsEarned > 0 || LoyaltyPointsRedeemed > 0;
    public SaleReceipt ReceiptModel => _receipt;

    public event Action? RequestClose;
    public event Action? RequestPrint;
    public event Action? RequestPrintEscPos;
    public event Action? RequestExportHtml;

    public ICommand CloseCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand PrintEscPosCommand { get; }
    public ICommand ExportHtmlCommand { get; }

    public ReceiptDialogViewModel(SaleReceipt receipt)
    {
        _receipt = receipt;
        CloseCommand = new RelayCommand(() => RequestClose?.Invoke());
        PrintCommand = new RelayCommand(() => RequestPrint?.Invoke());
        PrintEscPosCommand = new RelayCommand(() => RequestPrintEscPos?.Invoke());
        ExportHtmlCommand = new RelayCommand(() => RequestExportHtml?.Invoke());
    }
}
