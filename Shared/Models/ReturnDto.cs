namespace Shared.Models;

public class ReturnDto
{
    public string Code { get; set; }
    public string CustomerEmailAddress { get; set; }
    public string CustomerPhoneNumber { get; set; }
    public DateTime InitiationDate { get; set; }
    public List<ReturnEntryDto> Entries { get; set; } = new List<ReturnEntryDto>();
    public string OrderCode { get; set; }
    public AddressDto Address { get; set; }
}