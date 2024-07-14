namespace Shared.Models;

public class ManualReturnRequest
{
    public CustomerInfoRequest customerInfo { get; set; }
    public ICollection<ReturnEntryRequest> entries { get; set; }
    public DateTime initiationDate { get; set; }
    public string orderCode { get; set; }
    public string rma { get; set; }
}