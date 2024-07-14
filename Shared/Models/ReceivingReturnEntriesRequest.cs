namespace Shared.Models;

public class ReceivingReturnEntriesRequest
{
    public string reason { get; set; }
    public string notes { get; set; }
    public int orderEntryNumber { get; set; }
    public int quantity { get; set; }
    public string entryCode { get; set; }
    public ICollection<ReceivingReturnConsignmentsRequest> consignments { get; set; }

}