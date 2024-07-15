namespace Shared.Contracts;

public interface IReturnProcessingService
{
    Task ReadAndSaveReturnsAsync();
}