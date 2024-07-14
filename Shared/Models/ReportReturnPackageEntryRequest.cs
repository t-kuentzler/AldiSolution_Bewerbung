namespace Shared.Models;

public class ReportReturnPackageEntryRequest
{
    public List<ReportReturnPackageConsignmentRequest> consignments { get; set; }
    public string entryCode { get; set; }
    public string notes { get; set; }
    public int orderEntryNumber { get; set; }
    public int quantity { get; set; }
    public string reason { get; set; }
}