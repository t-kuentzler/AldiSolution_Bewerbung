namespace Shared.Models;

public class ReceivingReturnCustomerInfoRequest
{
    public ReceivingReturnAddressRequest address { get; set; }
    public string emailAddress { get; set; }

    public string? phoneNumber { get; set; }

}