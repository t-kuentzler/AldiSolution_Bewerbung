namespace Shared.Models;

public class CustomerInfoResponse
{
    public string emailAddress { get; set; }
    public string? phoneNumber { get; set; }
    public AddressResponse? address { get; set; }
}