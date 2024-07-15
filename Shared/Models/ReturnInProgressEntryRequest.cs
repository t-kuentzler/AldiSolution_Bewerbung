namespace Shared.Models;

public class ReturnInProgressEntryRequest
{
    public string entryCode { get; set; }
    public string notes { get; set; }
    public int orderEntryNumber { get; set; }
    public int quantity { get; set; }
    public string reason { get; set; } 
    public string status { get; set; } 
}