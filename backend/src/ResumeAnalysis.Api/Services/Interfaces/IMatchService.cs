using ResumeAnalysis.Api.DTOs.Matches;

namespace ResumeAnalysis.Api.Services.Interfaces;

public interface IMatchService
{
    // Protected application-level method for internal matching service to persist a result
    Task<MatchResultDto> CreateAsync(Guid candidateId, CreateMatchRequest request, CancellationToken ct);
    Task<MatchResultDto> GetByIdAsync(Guid candidateId, Guid matchId, CancellationToken ct);
    Task<IReadOnlyList<MatchResultDto>> ListAsync(Guid candidateId, CancellationToken ct);
    Task<IReadOnlyList<MatchResultDto>> ListForJobAsync(Guid candidateId, Guid jobId, CancellationToken ct);
    Task<IReadOnlyList<MatchResultDto>> ListForResumeAsync(Guid candidateId, Guid resumeId, CancellationToken ct);
}

// Abstraction for future matching algorithm integration
public interface IMatchProvider
{
    Task<MatchResultDto> GenerateMatchAsync(Guid candidateId, Guid resumeId, Guid jobId, CancellationToken ct);
}
