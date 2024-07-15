namespace Shared.Models;

public class ReturnInProgressCustomerInfoRequest
{
    public ReturnInProgressAddressRequest address { get; set; }
    public string emailAddress { get; set; }
    public string? phoneNumber { get; set; }
}