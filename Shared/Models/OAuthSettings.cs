namespace Shared.Models
{
    public class OAuthSettings
    {
        public string? VendorId { get; set; }
        public string? Password { get; set; }
        public string? Secret { get; set; }
        public string? BaseUrl { get; set; }
        public string? GetOrdersEndpoint { get; set; }
        public string? GetReturnsEndpoint { get; set; }

    }
}
