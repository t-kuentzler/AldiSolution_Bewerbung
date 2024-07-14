namespace Shared.Entities;

public class Consignment
{
    public int Id { get; set; }
    public string VendorConsignmentCode { get; set; }
    public string StatusText { get; set; }

    public string TrackingId { get; set; }
    public string? TrackingLink { get; set; }
    public string Carrier { get; set; }
    public DateTime ShippingDate { get; set; }
    public string Status { get; set; }
    public DateTime? ExpectedDelivery { get; set; }
    public DateTime? ReceiptDelivery { get; set; }
    public string? AldiConsignmentCode { get; set; }
    public string OrderCode { get; set; }
    public int ShippingAddressId { get; set; }
    public ICollection<ConsignmentEntry> ConsignmentEntries { get; set; }
    public virtual Order Order { get; set; }
    public virtual ShippingAddress ShippingAddress { get; set; }

    public Consignment()
    {
        ConsignmentEntries = new List<ConsignmentEntry>();
    }
}