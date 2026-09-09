using System;
using System.Collections.Generic;

namespace RetailFlow.Models;

public class SaleReceiptItem
{
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
}

public class SaleReceipt
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public List<SaleReceiptItem> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public decimal AmountTendered { get; set; }
    public decimal ChangeDue { get; set; }
}
