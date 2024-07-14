namespace Shared.Entities;

public class Return
{
    public int Id { get; set; }
    public string OrderCode { get; set; }
    public DateTime InitiationDate { get; set; }
    public string AldiReturnCode { get; set; }
    public string Rma { get; set; }
    public string Status { get; set; }
    public int CustomerInfoId { get; set; }
    public CustomerInfo CustomerInfo { get; set; }
    public ICollection<ReturnEntry> ReturnEntries { get; set; }
    public Order Order { get; set; }
}