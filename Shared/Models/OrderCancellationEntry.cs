namespace Shared.Models
{
    public class OrderCancellationEntry
    {
        public int cancelQuantity { get; set; }
        public string cancelReason { get; set; } = default!;
        public string notes { get; set; } = default!;
        public int orderEntryNumber { get; set; }
    }
}
