namespace Shared.Models;

public class ReceivingReturnConsignmentsResponse
{
    public string consignmentCode { get; set; }
    public int quantity { get; set; }
    public string carrier { get; set; }
    public string? carrierCode { get; set; }
    public ICollection<ReceivingReturnPackagesResponse> packages { get; set; }
}