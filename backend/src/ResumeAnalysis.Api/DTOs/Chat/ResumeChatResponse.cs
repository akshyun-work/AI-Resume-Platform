namespace ResumeAnalysis.Api.DTOs.Chat;

public class ResumeChatResponse
{
    public string Answer { get; set; } = string.Empty;

    public List<ChatMessageDto> Conversation { get; set; } = new();
}