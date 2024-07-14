namespace Shared.Models;

public class ShipmentInfoAndReturnIdRequest
{
    public List<ShipmentInfo> ShipmentInfo { get; set; }
    public int ReturnId { get; set; }
}