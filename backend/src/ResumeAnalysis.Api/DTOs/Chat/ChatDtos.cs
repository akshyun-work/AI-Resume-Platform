using System.ComponentModel.DataAnnotations;
using ResumeAnalysis.Api.Entities;

namespace ResumeAnalysis.Api.DTOs.Chat;

public class ChatSessionDto
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
}

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid ChatSessionId { get; set; }
    public string Sender { get; set; } = string.Empty; // "User" / "Assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public static ChatMessageDto FromEntity(ChatMessage m) => new()
    {
        Id = m.Id,
        ChatSessionId = m.ChatSessionId,
        Sender = m.Sender.ToString(),
        Content = m.Content,
        CreatedAt = m.CreatedAt
    };
}

public class ChatSessionDetailDto
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = new();
}

public class CreateChatMessageRequest
{
    [Required, MinLength(1), MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    // Optional: let caller specify role, default User. For future LLM, service will add Assistant messages.
    public string? Sender { get; set; } // "User" or "Assistant"
}
