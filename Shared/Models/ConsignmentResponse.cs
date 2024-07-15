namespace Shared.Models;

public class ConsignmentResponse
{
    public string vendorConsignmentCode { get; set; }
    public string statusText { get; set; }
    public string trackingId { get; set; }
    public string? trackingLink { get; set; }
    public string carrier { get; set; }
    public string shippingDate { get; set; }
    public ICollection<ConsignmentEntriesResponse> entries { get; set; }
    public string status { get; set; }
    public string? expectedDelivery { get; set; }
    public string? receiptDelivery { get; set; }
    public string aldiConsignmentCode { get; set; }
    public string orderCode { get; set; }
    public ConsignmentShippingAddressResponse shippingAddress { get; set; }


}