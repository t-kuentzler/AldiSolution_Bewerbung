namespace Shared.Models;

public class DhlStatus
{
    public DateTime Timestamp { get; set; }
    public string StatusCode { get; set; } = default!;
    public string Description { get; set; } = default!;
}