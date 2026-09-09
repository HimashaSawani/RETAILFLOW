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

    public int? CustomerId { get; set; }
    [MaxLength(20)]
    public string? CustomerPhone { get; set; }
    [MaxLength(100)]
    public string? CustomerName { get; set; }
    public int LoyaltyPointsEarned { get; set; } = 0;
    public int LoyaltyPointsRedeemed { get; set; } = 0;

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
