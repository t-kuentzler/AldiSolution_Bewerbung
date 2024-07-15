namespace Shared.Contracts;

public interface IConsignmentProcessingService
{
    Task ReadAndSaveConsignmentsAsync();
}