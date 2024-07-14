using Shared.Models;

namespace Shared.Contracts;

public interface IOAuthClientService
{
    Task<OrderResponse> GetApiOrdersAsync();
}