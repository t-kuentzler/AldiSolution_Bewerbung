
namespace Shared.Models
{
    public class Shipment
    {
        public string Id { get; set; } = default!; // Trackingnummer
        public DhlStatus Status { get; set; } = default!; // Status der Bestellung
    }
}
