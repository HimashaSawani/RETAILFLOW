using Microsoft.EntityFrameworkCore;
using RetailFlow.Data;
using RetailFlow.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RetailFlow.Services;

public class ProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<Product?> GetProductBySKUAsync(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return null;

        return await _context.Products
            .FirstOrDefaultAsync(p => p.SKU.ToLower() == sku.Trim().ToLower());
    }

    public async Task<bool> IsSkuExistsAsync(string sku, int excludeProductId = 0)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return false;

        string trimmedSku = sku.Trim().ToLower();
        return await _context.Products
            .AnyAsync(p => p.Id != excludeProductId && p.SKU.ToLower() == trimmedSku);
    }

    public async Task<Product> AddProductAsync(Product product)
    {
        product.SKU = product.SKU.Trim();
        product.Name = product.Name.Trim();
        product.Category = product.Category?.Trim() ?? string.Empty;
        product.CreatedAt = DateTime.UtcNow;

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateProductAsync(Product product)
    {
        var existing = await _context.Products.FindAsync(product.Id);
        if (existing == null)
            throw new InvalidOperationException($"Product with ID {product.Id} not found.");

        existing.SKU = product.SKU.Trim();
        existing.Name = product.Name.Trim();
        existing.Category = product.Category?.Trim() ?? string.Empty;
        existing.CostPrice = product.CostPrice;
        existing.SellingPrice = product.SellingPrice;
        existing.StockQuantity = product.StockQuantity;
        existing.ReorderLevel = product.ReorderLevel;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return false;

        // Check if product is referenced in sale items
        bool isUsedInSales = await _context.SaleItems.AnyAsync(si => si.ProductId == id);
        if (isUsedInSales)
        {
            throw new InvalidOperationException("Cannot delete product because it has associated sales records.");
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Product>> SearchProductsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllProductsAsync();

        string trimmedQuery = query.Trim().ToLower();
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Name.ToLower().Contains(trimmedQuery) ||
                        p.SKU.ToLower().Contains(trimmedQuery) ||
                        p.Category.ToLower().Contains(trimmedQuery))
            .OrderBy(p => p.Id)
            .ToListAsync();
    }
}
