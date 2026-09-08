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

    public event Action? RequestClose;

    public ICommand CloseCommand { get; }

    public ReceiptDialogViewModel(SaleReceipt receipt)
    {
        _receipt = receipt;
        CloseCommand = new RelayCommand(() => RequestClose?.Invoke());
    }
}
