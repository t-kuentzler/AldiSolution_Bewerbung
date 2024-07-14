namespace Shared.Models;

public class ReceivingReturnEntriesResponse
{
    public string reason { get; set; }
    public string notes { get; set; }
    public int orderEntryNumber { get; set; }
    public int quantity { get; set; }
    public string entryCode { get; set; }
    public ICollection<ReceivingReturnConsignmentsResponse> consignments { get; set; }
}