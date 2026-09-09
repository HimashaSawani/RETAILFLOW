using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailFlow.Models;

public class Sale
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string TransactionNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Discount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Cash";

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountTendered { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ChangeDue { get; set; }

    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
