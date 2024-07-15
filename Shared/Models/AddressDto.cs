namespace Shared.Models;

public class AddressDto
{
    public string Type { get; set; }
    public string? SalutationCode { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string StreetName { get; set; }
    public string StreetNumber { get; set; }
    public string? Remarks { get; set; }
    public string PostalCode { get; set; }
    public string Town { get; set; }
    public string? PackstationNumber { get; set; }
    public string? PostNumber { get; set; }
    public string? PostOfficeNumber { get; set; }
    public string CountryIsoCode { get; set; }
}