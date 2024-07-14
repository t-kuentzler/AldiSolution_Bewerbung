namespace Shared.Entities;

public class DeliveryAddress
{
    public int Id { get; set; }
    public string Type { get; set; }
    public string SalutationCode { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? StreetName { get; set; }
    public string? StreetNumber { get; set; }
    public string? Remarks { get; set; }
    public string PostalCode { get; set; }
    public string Town { get; set; }
    public string? PackstationNumber { get; set; }
    public string? PostNumber { get; set; }
    public string? PostOfficeNumber { get; set; }
    public string CountryIsoCode { get; set; }
    public OrderEntry OrderEntry { get; set; }
}