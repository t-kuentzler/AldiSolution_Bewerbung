using Shared.Models;

namespace Shared.Contracts;

public interface IStatisticService
{
    Task<List<ProductSalesViewModel>> GetProductSalesAsync();
}