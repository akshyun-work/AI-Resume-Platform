using ResumeAnalysis.Api.DTOs.Chat;

namespace ResumeAnalysis.Api.Services.Interfaces;

public interface IChatService
{
    Task<ChatSessionDto> CreateSessionAsync(Guid candidateId, CreateChatSessionRequest request, CancellationToken ct);
    Task<IReadOnlyList<ChatSessionDto>> ListSessionsAsync(Guid candidateId, CancellationToken ct);
    Task<ChatSessionDetailDto> GetSessionAsync(Guid candidateId, Guid sessionId, CancellationToken ct);
    Task DeleteSessionAsync(Guid candidateId, Guid sessionId, CancellationToken ct);
    Task<ChatMessageDto> AddMessageAsync(Guid candidateId, Guid sessionId, CreateChatMessageRequest request, CancellationToken ct);
    Task<IReadOnlyList<ChatMessageDto>> ListMessagesAsync(Guid candidateId, Guid sessionId, CancellationToken ct);
}

// Clean contract for future LLM/Chatbot service
public interface IChatCompletionProvider
{
    Task<string> CompleteAsync(Guid candidateId, Guid sessionId, string userMessage, CancellationToken ct);
}
