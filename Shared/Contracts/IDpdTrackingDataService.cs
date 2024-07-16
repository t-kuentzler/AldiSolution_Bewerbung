using Shared.Models;

namespace Shared.Contracts;

public interface IDpdTrackingDataService
{
    Task ProcessTrackingData(TrackingData trackingData);
}