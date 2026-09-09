using System;
using System.ComponentModel.DataAnnotations;

namespace RetailFlow.Models;

public enum UserRole
{
    StoreManager,
    Cashier
}

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.Cashier;

    [Required]
    [MaxLength(10)]
    public string PinCode { get; set; } = "0000";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
