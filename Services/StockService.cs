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

    public async Task<bool> UpdateStockQuantityAsync(int productId, int newQuantity)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return false;

        product.StockQuantity = newQuantity;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AdjustStockAsync(int productId, int quantityDelta)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return false;

        if (product.StockQuantity + quantityDelta < 0)
            return false;

        product.StockQuantity += quantityDelta;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Product>> GetLowStockProductsAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.StockQuantity <= p.ReorderLevel)
            .ToListAsync();
    }
}
