using ResumeAnalysis.Api.DTOs.Chat;

namespace ResumeAnalysis.Api.Services.Interfaces;

public interface IResumeChatService
{
    Task<ResumeChatResponse> AskAsync(
        Guid candidateId,
        Guid resumeId,
        ResumeChatRequest request,
        CancellationToken ct);
}