namespace Shared.Entities;

public class ReturnEntry
{
    public int Id { get; set; }
    public int ReturnId { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public int OrderEntryNumber { get; set; }
    public int Quantity { get; set; }
    public int CanceledOrReturnedQuantity { get; set; }

    public string EntryCode { get; set; }
    public string Status { get; set; }
    public string? CarrierCode { get; set; }
    public Return Return { get; set; }
    public ICollection<ReturnConsignment> ReturnConsignments { get; set; }

    public ReturnEntry()
    {
        CanceledOrReturnedQuantity = 0;

    }

}
