using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Contracts;
using Shared.Exceptions;

namespace Shared.Repositories;

public class StatisticRepository : IStatisticRepository
{
    private readonly ApplicationDbContext _context;

    public StatisticRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<(string productName, string articleNumber, int sold, int returned, int totalSold)>> GetProductSalesAsync()
    {
        try
        {
            var salesData = await _context.OrderEntry
                .Include(o => o.Order)
                .ThenInclude(order => order.Returns)
                .ThenInclude(r => r.ReturnEntries)
                .Where(o => o.Order.Status == SharedStatus.Delivered || o.Order.Status == SharedStatus.Shipped)
                .ToListAsync();

            var productSales = salesData.GroupBy(o => new { o.ProductName, o.VendorProductCode })
                .Select(group => new
                {
                    ProductName = group.Key.ProductName,
                    ArticleNumber = group.Key.VendorProductCode,
                    Sold = group.Sum(g => g.Quantity),
                    Returned = group.Sum(g => g.Order.Returns.SelectMany(r => r.ReturnEntries)
                        .Where(re => re.OrderEntryNumber == g.EntryNumber)
                        .Sum(re => re.Quantity)),
                })
                .Select(ps => new
                {
                    ps.ProductName,
                    ps.ArticleNumber,
                    ps.Sold,
                    ps.Returned,
                    TotalSold = ps.Sold - ps.Returned
                })
                .ToList();

            return productSales.Select(ps => (ps.ProductName, ps.ArticleNumber, ps.Sold, ps.Returned, ps.TotalSold)).ToList();
        }
        catch (Exception ex)
        {
            throw new RepositoryException("Ein unerwarteter Fehler ist aufgetreten beim abrufen der Verkaufszahlen.", ex);
        }
    }
}