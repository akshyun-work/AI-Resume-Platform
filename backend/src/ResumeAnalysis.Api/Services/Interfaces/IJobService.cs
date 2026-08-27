using ResumeAnalysis.Api.DTOs.Jobs;

namespace ResumeAnalysis.Api.Services.Interfaces;

public interface IJobService
{
    Task<JobDto> CreateAsync(CreateJobRequest request, CancellationToken ct);
    Task<JobDto> UpdateAsync(Guid jobId, UpdateJobRequest request, CancellationToken ct);
    Task DeleteAsync(Guid jobId, CancellationToken ct);
    Task<JobDto> GetByIdAsync(Guid jobId, CancellationToken ct);
    Task<(IReadOnlyList<JobDto> items, int total)> SearchAsync(JobQueryParams query, CancellationToken ct);
}
