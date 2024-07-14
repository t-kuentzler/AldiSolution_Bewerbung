namespace Shared.Models;

public class ReceivingReturnResponse
{
    public string aldiReturnCode { get; set; }
    public ReceivingReturnCustomerInfoResponse customerInfo { get; set; }
    public ICollection<ReceivingReturnEntriesResponse> entries { get; set; }
    public DateTime initiationDate { get; set; }
    public string orderCode { get; set; }
    public string rma { get; set; }
}