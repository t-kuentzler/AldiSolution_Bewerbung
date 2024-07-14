namespace Shared.Entities;

public class ReturnPackage
{
    public int Id { get; set; }
    public string VendorPackageCode { get; set; }
    public string TrackingId { get; set; }
    public string TrackingLink { get; set; }
    public string Status { get; set; }
    public DateTime? ReceiptDelivery { get; set; }
    public int ReturnConsignmentId { get; set; }
    public ReturnConsignment ReturnConsignment { get; set; } 
}