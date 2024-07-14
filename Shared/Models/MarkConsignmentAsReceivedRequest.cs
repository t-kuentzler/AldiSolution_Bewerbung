namespace Shared.Models;

public class MarkConsignmentAsReceivedRequest
{
    public string ConsignmentCode { get; set; }
    public int ReturnId { get; set; }
}