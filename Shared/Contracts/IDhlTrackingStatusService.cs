namespace Shared.Contracts;

public interface IDhlTrackingStatusService
{
    Task ReadAndUpdateTrackingStatusAsync();
}