namespace Shared.Models;

public class ReturnProcessingResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public ManualReturnResponse ManualReturnResponse { get; set; }
}
