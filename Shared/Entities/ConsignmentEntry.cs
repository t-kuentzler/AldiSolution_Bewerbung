namespace Shared.Entities;

public class ConsignmentEntry
{
    public int Id { get; set; }
    public int ConsignmentId { get; set; }
    public int OrderEntryNumber { get; set; }
    public int OrderEntryId { get; set; }
    public int Quantity { get; set; }
    public int CancelledOrReturnedQuantity { get; set; }
    public virtual Consignment Consignment { get; set; }
    public virtual OrderEntry OrderEntry { get; set; }

    public ConsignmentEntry()
    {
        CancelledOrReturnedQuantity = 0;
    }
}