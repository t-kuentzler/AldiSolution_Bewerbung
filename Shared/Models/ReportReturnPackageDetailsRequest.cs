namespace Shared.Models;

public class ReportReturnPackageDetailsRequest
{
    public string completedDate { get; set; }
    public string receiptDelivery { get; set; }
    public string status { get; set; }
    public string trackingId { get; set; }
    public string trackingLink { get; set; }
    public string vendorPackageCode { get; set; }
}