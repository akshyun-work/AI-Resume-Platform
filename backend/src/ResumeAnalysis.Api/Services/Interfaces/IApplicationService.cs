using ResumeAnalysis.Api.DTOs.Applications;

namespace ResumeAnalysis.Api.Services.Interfaces;

public interface IApplicationService
{
    Task<ApplicationDto> ApplyAsync(Guid candidateId, CreateApplicationRequest request, CancellationToken ct);
    Task<IReadOnlyList<ApplicationDto>> ListAsync(Guid candidateId, CancellationToken ct);
    Task<ApplicationDto> GetByIdAsync(Guid candidateId, Guid applicationId, CancellationToken ct);
}
