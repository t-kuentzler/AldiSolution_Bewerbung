namespace Shared.Models
{
    public class CancelOrderEntryModel
    {
        public bool IsCancelled { get; set; }
        public int CancelQuantity { get; set; }
        public int OrderEntryId { get; set; }
    }
}
