using Shared.Entities;

namespace Shared.Models
{
    public class OrderResponse
    {
        public List<Order> Orders { get; set; } = new List<Order>();
        public DateTime Timestamp { get; set; }

    }
}
