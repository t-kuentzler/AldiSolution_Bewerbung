namespace Shared.Models
{
    public class EmailConfiguration
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int Port { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderUsername { get; set; } = string.Empty;
        public string? SenderPassword { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
    }
}