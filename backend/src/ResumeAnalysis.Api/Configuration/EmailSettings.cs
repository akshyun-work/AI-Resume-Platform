namespace ResumeAnalysis.Api.Configuration;

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string SenderEmail { get; set; } = "noreply@resumesmart.ai";
    public string SenderName { get; set; } = "Resume Smart AI";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool DevMode { get; set; } = true;
}
