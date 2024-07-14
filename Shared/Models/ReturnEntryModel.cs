namespace Shared.Models;

public class ReturnEntryModel
{
    public bool IsReturned { get; set; }
    public int ReturnQuantity { get; set; }
    public int OrderEntryNumber { get; set; }
    public string Reason { get; set; }
    public int ConsignmentEntryId { get; set; }

}