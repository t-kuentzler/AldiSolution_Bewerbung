namespace Shared.Models;

public class ReceivingReturnPackagesRequest
{
    public string status { get; set; }
    public string trackingId { get; set; }
    public string trackingLink { get; set; }
    public string vendorPackageCode { get; set; }

}