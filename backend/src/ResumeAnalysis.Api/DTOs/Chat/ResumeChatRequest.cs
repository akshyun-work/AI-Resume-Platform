namespace ResumeAnalysis.Api.DTOs.Chat;

public class ResumeChatRequest
{
    public Guid ChatSessionId { get; set; }

    public string Message { get; set; } = string.Empty;
}