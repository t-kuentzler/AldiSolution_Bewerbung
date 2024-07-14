namespace Shared.Entities;

public class Order
{
    public int Id { get; set; }
    public string Code { get; set; }
    public string Status { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public string AldiCustomerNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? Phone { get; set; }
    public string? Language { get; set; }
    public string? OrderDeliveryArea { get; set; }
    public bool Exported { get; set; }

    public ICollection<OrderEntry> Entries { get; set; }
    public ICollection<Consignment> Consignments { get; set; }

    public ICollection<Return> Returns { get; set; }
}