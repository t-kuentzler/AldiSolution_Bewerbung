namespace Shared.Models;

public class ReportReturnPackageConsignmentRequest
{
    public string carrier { get; set; }
    public string consignmentCode { get; set; }
    public List<ReportReturnPackageDetailsRequest> packages { get; set; }
    public int quantity { get; set; }
}