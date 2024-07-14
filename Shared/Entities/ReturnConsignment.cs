namespace Shared.Entities;

public class ReturnConsignment
{
    public int Id { get; set; }
    public string ConsignmentCode { get; set; }
    public int Quantity { get; set; }
    public int CanceledQuantity { get; set; }
    public int CompletedQuantity { get; set; }
    public string Carrier { get; set; }
    public string? CarrierCode { get; set; }
    public string Status { get; set; }
    public DateTime? CompletedDate { get; set; }
    public ICollection<ReturnPackage> Packages { get; set; }
    public int ReturnEntryId { get; set; }
    public ReturnEntry ReturnEntry { get; set; }

    public ReturnConsignment()
    {
        CanceledQuantity = 0;
        CompletedQuantity = 0;
    }
}