namespace Shared.Models;

public class ReportReturnPackageRequest
{
    public string aldiReturnCode { get; set; }
    public ReportReturnPackageCustomerInfoRequest customerInfo { get; set; }
    public List<ReportReturnPackageEntryRequest> entries { get; set; }
    public string initiationDate { get; set; }
    public string orderCode { get; set; }
}