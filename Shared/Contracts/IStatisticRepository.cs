namespace Shared.Contracts;

public interface IStatisticRepository
{
    Task<List<(string productName, string articleNumber, int sold, int returned, int totalSold)>>
        GetProductSalesAsync();
}