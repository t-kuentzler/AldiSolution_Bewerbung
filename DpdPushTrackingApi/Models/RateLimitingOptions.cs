namespace DpdPushTrackingApi.Models;

public class RateLimitingOptions
{
    public int TokenLimit { get; set; }
    public int TokensPerPeriod { get; set; }
    public int ReplenishmentPeriodInSeconds { get; set; }
}