namespace Shared.Models;

public class ReportReturnPackageCustomerInfoRequest
{
    public ReportReturnPackageAddressRequest address { get; set; }
    public string emailAddress { get; set; }
    public string phoneNumber { get; set; }
}