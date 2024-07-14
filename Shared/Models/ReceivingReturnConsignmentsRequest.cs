namespace Shared.Models;

public class ReceivingReturnConsignmentsRequest
{
    public string carrier { get; set; }
    public ICollection<ReceivingReturnPackagesRequest> packages { get; set; }
    public int quantity { get; set; }

}