namespace Shared.Models;

public class ReceivingReturnAddressRequest
{
    public string countryIsoCode { get; set; }
    public string? firstName { get; set; }
    public string? lastName { get; set; }
    public string? streetName { get; set; }
    public string? streetNumber { get; set; }
    public string? postalCode { get; set; }
    public string? town { get; set; }
    public string type { get; set; }
}