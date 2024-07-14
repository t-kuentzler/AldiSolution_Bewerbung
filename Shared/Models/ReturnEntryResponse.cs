namespace Shared.Models;

public class ReturnEntryResponse
{
    public string? reason { get; set; }
    public string? notes { get; set; }
    public int orderEntryNumber { get; set; }
    public int quantity { get; set; }
    public string? entryCode { get; set; } 
    public List<ReturnConsignmentResponse>? consignments { get; set; } 
    public string status { get; set; }
    public string? carrierCode { get; set; } 
}