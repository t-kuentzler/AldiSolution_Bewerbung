namespace Shared.Entities;

public class CustomerInfo
{
    public int Id { get; set; }
    public string EmailAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public int AddressId { get; set; }
    public Address? Address { get; set; }
    public ICollection<Return> Returns { get; set; }
}