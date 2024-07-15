namespace Shared.Contracts;

public interface IDhlApiService
{
    Task<string> GetTrackingStatusFromApiAsync(string trackingNumber);
}