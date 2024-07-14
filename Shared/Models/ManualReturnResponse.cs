namespace Shared.Models;

public class ManualReturnResponse
{
    public string orderCode { get; set; }
    public CustomerInfoResponse? customerInfo { get; set; }
    public List<ReturnEntryResponse>? entries { get; set; }
    public DateTime initiationDate { get; set; }
    public string aldiReturnCode { get; set; }
    public string rma { get; set; }
}