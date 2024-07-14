
using Shared.Entities;

namespace Shared.Models;

public class ReturnDetailsViewModel
{
    public Return returnObj { get; set; }
    public List<ShipmentInfo?> ShipmentInfos { get; set; }
}