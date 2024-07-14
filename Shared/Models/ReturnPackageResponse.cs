namespace Shared.Models;

public class ReturnPackageResponse
{
    public string vendorPackageCode { get; set; }
    public string trackingId { get; set; }
    public string trackingLink { get; set; }
    public string status { get; set; }
    public DateTime receiptDelivery { get; set; }
}