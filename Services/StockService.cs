using Microsoft.EntityFrameworkCore;
using RetailFlow.Data;
using RetailFlow.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RetailFlow.Services;

public class StockService
{
    private readonly AppDbContext _context;

    public StockService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllStockProductsAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<bool> RestockProductAsync(int productId, int quantityToAdd)
    {
        if (quantityToAdd <= 0)
            throw new ArgumentException("Restock quantity must be greater than zero.");

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return false;

        product.StockQuantity += quantityToAdd;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStockQuantityAsync(int productId, int newQuantity)
    {
        if (newQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative.");

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return false;

        product.StockQuantity = newQuantity;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Product>> GetLowStockProductsAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.StockQuantity <= p.ReorderLevel)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync();
    }
}
