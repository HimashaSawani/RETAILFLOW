using Microsoft.EntityFrameworkCore;
using RetailFlow.Data;
using RetailFlow.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RetailFlow.Services;

public class CustomerService
{
    private readonly AppDbContext _context;

    public CustomerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByPhoneAsync(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var cleanPhone = phone.Trim();
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.PhoneNumber == cleanPhone);
    }

    public async Task<List<Customer>> SearchCustomersAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<Customer>();

        var q = query.Trim().ToLower();
        return await _context.Customers
            .Where(c => c.PhoneNumber.Contains(q) || c.Name.ToLower().Contains(q))
            .Take(10)
            .ToListAsync();
    }

    public async Task<Customer> RegisterCustomerAsync(string phone, string name)
    {
        var cleanPhone = phone.Trim();
        var cleanName = name.Trim();

        var existing = await GetByPhoneAsync(cleanPhone);
        if (existing != null)
            return existing;

        var customer = new Customer
        {
            PhoneNumber = cleanPhone,
            Name = cleanName,
            LoyaltyPoints = 0,
            TotalSpent = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }
}
