namespace Shared.Models;

public class ReceivingReturnCustomerInfoResponse
{
    public ReceivingReturnAddressResponse address { get; set; }
    public string emailAddress { get; set; }

    public string? phoneNumber { get; set; }
}