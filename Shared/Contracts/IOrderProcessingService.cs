namespace Shared.Contracts;

public interface IOrderProcessingService
{
    Task ProcessOpenOrdersAsync();
}