namespace Shared.Models;

public class ReturnConsignmentResponse
{
    public string consignmentCode { get; set; }
    public int quantity { get; set; }
    public string? carrier { get; set; }
    public DateTime completedDate { get; set; }
    public ICollection<ReturnPackageResponse>? packages { get; set; }
}