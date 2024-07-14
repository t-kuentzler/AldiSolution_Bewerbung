
using Shared.Entities;

namespace Shared.Models;

public class ProcessCancellationEntry
{
    public Order Order { get; set; }
    public OrderEntry OrderEntry { get; set; }
    public OrderCancellationEntry OrderCancellationEntry { get; set; }
}