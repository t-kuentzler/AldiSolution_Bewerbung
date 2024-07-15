namespace Shared.Models
{
    public class ConsignmentRequest
    {
        public string carrier { get; set; }

        public IEnumerable<ConsignmentEntryRequest> entries { get; set; }

        public ConsignmentShippingAddressRequest shippingAddress { get; set; } 

        public string shippingDate { get; set; } 

        public string status { get; set; } 

        public string statusText { get; set; } 

        public string trackingId { get; set; } 

        public string vendorConsignmentCode { get; set; } 
    }
}
