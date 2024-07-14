namespace Shared.Models;

public class ShipmentInfo
{
    public string ProductCode { get; set; }
    public string Reason { get; set; }
    public string TrackingNumber { get; set; }
    public string Carrier { get; set; }
    public int Quantity { get; set; }
    public int ReturnEntryId { get; set; }
}