namespace Shared.Models;

public class ReceivingReturnRequest
{
    public string aldiReturnCode { get; set; }
    public ReceivingReturnCustomerInfoRequest customerInfo { get; set; }
    public ICollection<ReceivingReturnEntriesRequest> entries { get; set; }
    
    //Kein DateTime weil es für API umgewandelt werden muss
    public string initiationDate { get; set; }
    public string orderCode { get; set; }

}