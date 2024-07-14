namespace Shared.Models;

public class CustomerInfoRequest
{
    public string? emailAddress { get; set; }
    public string? phoneNumber { get; set; }
    public AddressRequest address { get; set; }
}