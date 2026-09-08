using Microsoft.EntityFrameworkCore;
using RetailFlow.Data;
using RetailFlow.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RetailFlow.Services;

public class TopProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal Revenue { get; set; }
}

public class DailySalesDto
{
    public string DayName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal TotalSales { get; set; }
    public int TransactionCount { get; set; }
    public double BarHeightRatio { get; set; } // For charting
}

public class DashboardMetricsDto
{
    public decimal TodaySales { get; set; }
    public int TodayTransactions { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<DailySalesDto> WeeklySales { get; set; } = new();
}

public class SalesService
{
    private readonly AppDbContext _context;

    public SalesService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the entire sale within a single database transaction.
    /// Step 1: Validate cart
    /// Step 2: Check stock
    /// Step 3: Create Sale & Invoice #INV-XXXX
    /// Step 4: Create SaleItems
    /// Step 5: Reduce Product.StockQuantity
    /// Step 6: Save everything atomically
    /// Step 7: Return comprehensive receipt
    /// </summary>
    public async Task<SaleReceipt> ExecuteSaleTransactionAsync(List<(int ProductId, int Quantity)> cartItems, decimal discount)
    {
        if (cartItems == null || !cartItems.Any())
        {
            throw new InvalidOperationException("Sale failed: Cart cannot be empty.");
        }

        // Begin EF Core Database Transaction (Phase 12)
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var receiptItems = new List<SaleReceiptItem>();
            var saleItems = new List<SaleItem>();
            decimal subtotal = 0;

            int existingSalesCount = await _context.Sales.CountAsync();
            string invoiceNumber = $"INV-{(existingSalesCount + 1):D4}";

            var sale = new Sale
            {
                TransactionNumber = invoiceNumber,
                SaleDate = DateTime.UtcNow,
                Discount = discount
            };

            foreach (var (productId, quantity) in cartItems)
            {
                if (quantity <= 0)
                {
                    throw new InvalidOperationException($"Sale failed: Invalid quantity '{quantity}' requested.");
                }

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    throw new InvalidOperationException($"Sale failed: Product with ID {productId} was not found.");
                }

                if (product.StockQuantity < quantity)
                {
                    throw new InvalidOperationException(
                        $"Sale failed: Insufficient stock for '{product.Name}'. Available: {product.StockQuantity}, Requested: {quantity}.");
                }

                int prevStock = product.StockQuantity;
                product.StockQuantity -= quantity;
                int newStock = product.StockQuantity;

                decimal itemTotal = quantity * product.SellingPrice;
                subtotal += itemTotal;

                var saleItem = new SaleItem
                {
                    Sale = sale,
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = product.SellingPrice,
                    Subtotal = itemTotal
                };
                saleItems.Add(saleItem);

                receiptItems.Add(new SaleReceiptItem
                {
                    ProductName = product.Name,
                    SKU = product.SKU,
                    Quantity = quantity,
                    UnitPrice = product.SellingPrice,
                    LineTotal = itemTotal,
                    PreviousStock = prevStock,
                    NewStock = newStock
                });
            }

            sale.Subtotal = subtotal;
            sale.Total = Math.Max(0, subtotal - discount);
            sale.SaleItems = saleItems;

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new SaleReceipt
            {
                InvoiceNumber = invoiceNumber,
                SaleDate = sale.SaleDate,
                Items = receiptItems,
                Subtotal = sale.Subtotal,
                Discount = sale.Discount,
                Total = sale.Total
            };
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

        var sales = await query.ToListAsync();
        return sales.Sum(s => s.Total);
    }

    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // Today's Sales
        var todaySalesList = await _context.Sales
            .Where(s => s.SaleDate >= today && s.SaleDate < tomorrow)
            .ToListAsync();

        decimal todaySalesTotal = todaySalesList.Sum(s => s.Total);
        int todayTransactions = todaySalesList.Count;

        // Total products & low stock
        int totalProducts = await _context.Products.CountAsync();
        int lowStockCount = await _context.Products
            .CountAsync(p => p.StockQuantity <= p.ReorderLevel);

        // Top Selling Products (Client-side evaluation to support SQLite decimal operations)
        var allSaleItems = await _context.SaleItems
            .Include(si => si.Product)
            .AsNoTracking()
            .ToListAsync();

        var topProductsRaw = allSaleItems
            .Where(si => si.Product != null)
            .GroupBy(si => new { si.ProductId, si.Product.Name, si.Product.SKU })
            .Select(g => new TopProductDto
            {
                ProductName = g.Key.Name,
                SKU = g.Key.SKU,
                UnitsSold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Subtotal)
            })
            .OrderByDescending(t => t.UnitsSold)
            .Take(5)
            .ToList();

        // 7-day Weekly Sales for Chart
        var weekSales = new List<DailySalesDto>();
        for (int i = 6; i >= 0; i--)
        {
            var dayStart = today.AddDays(-i);
            var dayEnd = dayStart.AddDays(1);

            var daySales = await _context.Sales
                .Where(s => s.SaleDate >= dayStart && s.SaleDate < dayEnd)
                .ToListAsync();

            weekSales.Add(new DailySalesDto
            {
                DayName = dayStart.ToString("ddd"),
                Date = dayStart,
                TotalSales = daySales.Sum(s => s.Total),
                TransactionCount = daySales.Count
            });
        }

        // Calculate chart bar scaling
        decimal maxSale = weekSales.Max(w => w.TotalSales);
        if (maxSale <= 0) maxSale = 1;

        foreach (var day in weekSales)
        {
            day.BarHeightRatio = Math.Max(0.08, (double)(day.TotalSales / maxSale));
        }

        return new DashboardMetricsDto
        {
            TodaySales = todaySalesTotal,
            TodayTransactions = todayTransactions,
            TotalProducts = totalProducts,
            LowStockProducts = lowStockCount,
            TopProducts = topProductsRaw,
            WeeklySales = weekSales
        };
    }
}
