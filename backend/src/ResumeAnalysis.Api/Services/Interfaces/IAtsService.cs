using ResumeAnalysis.Api.DTOs.Ats;

namespace ResumeAnalysis.Api.Services.Interfaces;

public interface IAtsService
{
    Task<AtsAnalysisDto> CreateAsync(Guid candidateId, CreateAtsRequest request, CancellationToken ct);
    Task<AtsAnalysisDto> GetByIdAsync(Guid candidateId, Guid analysisId, CancellationToken ct);
    Task<AtsAnalysisDto> GetLatestForResumeAsync(Guid candidateId, Guid resumeId, CancellationToken ct);
    Task<IReadOnlyList<AtsAnalysisDto>> GetHistoryForResumeAsync(Guid candidateId, Guid resumeId, CancellationToken ct);
    Task<IReadOnlyList<AtsAnalysisDto>> ListForCandidateAsync(Guid candidateId, CancellationToken ct);
}

// Clean abstraction for future external AI/ML integration
public interface IAtsAnalysisProvider
{
    Task<AtsAnalysisDto> AnalyzeAsync(Guid candidateId, Guid resumeId, CancellationToken ct);
}
