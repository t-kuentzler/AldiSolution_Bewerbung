namespace Shared.Entities;

public class OrderEntry
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int EntryNumber { get; set; }
    public string VendorProductCode { get; set; }
    public string AldiProductCode { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public int CanceledOrReturnedQuantity { get; set; }
    public string? CarrierCode { get; set; }
    public string? AldiSuedProductCode { get; set; }

    public int DeliveryAddressId { get; set; }
    public DeliveryAddress? DeliveryAddress { get; set; }
    public Order Order { get; set; }
    public virtual ICollection<ConsignmentEntry> ConsignmentEntries { get; set; }

    public OrderEntry()
    {
        CanceledOrReturnedQuantity = 0;
    }
}