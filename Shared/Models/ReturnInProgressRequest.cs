namespace Shared.Models;

public class ReturnInProgressRequest
{
    public string aldiReturnCode { get; set; }
    public ReturnInProgressCustomerInfoRequest customerInfo { get; set; }
    public List<ReturnInProgressEntryRequest> entries { get; set; }
    public string initiationDate { get; set; }
    public string orderCode { get; set; }
    public string rma { get; set; }
}