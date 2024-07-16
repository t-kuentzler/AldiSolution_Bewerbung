using Microsoft.AspNetCore.Mvc;

namespace Shared.Models
{
    public class TrackingData
    {
        public string pushid { get; set; } = default!;
        [FromQuery(Name = "ref")]
        public string? reference { get; set; }

        public string? pnr { get; set; } = default!;
        public string? depot { get; set; }
        public string? status { get; set; } = default!;
        public string? statusdate { get; set; }
        public string? weight { get; set; }
        public string? receiver { get; set; }
        public string? pod { get; set; }
        public string? scaninfo { get; set; }
        public string? services { get; set; }
    }
}
