namespace Shared.Models;

public class ReturnEntryDto
{
    public int OrderEntryNumber { get; set; }
    public string ProductCode { get; set; }
    public string VendorProductCode { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; }
    public string? Notes { get; set; }
    public string Code { get; set; }
    public string CarrierCode { get; set; }
}