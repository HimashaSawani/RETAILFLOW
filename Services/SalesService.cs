using Microsoft.EntityFrameworkCore;
using RetailFlow.Data;
using RetailFlow.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RetailFlow.Services;

public class SalesService
{
    private readonly AppDbContext _context;

    public SalesService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Sale> ProcessSaleAsync(Sale sale)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (string.IsNullOrWhiteSpace(sale.TransactionNumber))
            {
                sale.TransactionNumber = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            }

            decimal subtotal = 0;
            foreach (var item in sale.SaleItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                    throw new InvalidOperationException($"Product with ID {item.ProductId} not found.");

                if (product.StockQuantity < item.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for '{product.Name}'. Available: {product.StockQuantity}, Requested: {item.Quantity}");

                product.StockQuantity -= item.Quantity;

                item.UnitPrice = product.SellingPrice;
                item.Subtotal = item.Quantity * item.UnitPrice;
                subtotal += item.Subtotal;
            }

            sale.SaleDate = DateTime.UtcNow;
            sale.Subtotal = subtotal;
            sale.Total = subtotal - sale.Discount;

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return sale;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<Sale>> GetAllSalesAsync()
    {
        return await _context.Sales
            .Include(s => s.SaleItems)
            .ThenInclude(si => si.Product)
            .AsNoTracking()
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
    }

    public async Task<Sale?> GetSaleByIdAsync(int id)
    {
        return await _context.Sales
            .Include(s => s.SaleItems)
            .ThenInclude(si => si.Product)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<decimal> GetTotalSalesRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Sales.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(s => s.SaleDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(s => s.SaleDate <= toDate.Value);

        return await query.SumAsync(s => s.Total);
    }
}
