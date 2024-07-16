using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Exceptions;
using Shared.Models;

namespace Shared.Services;

public class StatisticService : IStatisticService
{
    private readonly IStatisticRepository _statisticRepository;
    private readonly ILogger<StatisticService> _logger;

    public StatisticService(IStatisticRepository statisticRepository, ILogger<StatisticService> logger)
    {
        _statisticRepository = statisticRepository;
        _logger = logger;
    }

    public async Task<List<ProductSalesViewModel>> GetProductSalesAsync()
    {
        try
        {
            var productSalesData = await _statisticRepository.GetProductSalesAsync();

            var sortedData = productSalesData.OrderBy(data => data.articleNumber).ToList();

            return MapToViewModel(sortedData);
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim abrufen der Verkaufszahlen.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim abrufen der Verkaufszahlen.");
            throw new StatisticServiceException(
                $"Unerwarteter Fehler beim abrufen der Verkaufszahlen.",
                ex);
        }
    }

    private List<ProductSalesViewModel> MapToViewModel(List<(string productName, string articleNumber, int sold, int returned, int totalSold)> productSalesData)
    {
        return productSalesData.Select(ps => new ProductSalesViewModel
        {
            ProductName = ps.productName,
            ArticleNumber = ps.articleNumber,
            Sold = ps.sold,
            Returned = ps.returned,
            TotalSold = ps.totalSold
        }).ToList();
    }
}