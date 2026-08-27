using System.ComponentModel.DataAnnotations;

namespace ResumeAnalysis.Api.DTOs.Chat;

public class CreateChatSessionRequest
{
    [MaxLength(200)]
    public string? Title { get; set; }
}
