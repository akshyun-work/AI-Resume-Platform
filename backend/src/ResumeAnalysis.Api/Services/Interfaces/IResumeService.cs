using ResumeAnalysis.Api.DTOs.Resumes;

namespace ResumeAnalysis.Api.Services.Interfaces;

public interface IResumeService
{
    Task<ResumeDto> UploadAsync(
        Guid candidateId,
        IFormFile file,
        CancellationToken ct);

    Task<IReadOnlyList<ResumeDto>> ListAsync(
        Guid candidateId,
        CancellationToken ct);

    Task<ResumeDto> GetAsync(
        Guid candidateId,
        Guid resumeId,
        CancellationToken ct);

    Task<(Stream stream, string contentType, string fileName)> DownloadAsync(
        Guid candidateId,
        Guid resumeId,
        CancellationToken ct);

    Task DeleteAsync(
        Guid candidateId,
        Guid resumeId,
        CancellationToken ct);

    Task<ResumeDto?> GetLatestAsync(
        Guid candidateId,
        CancellationToken ct);

    Task<string> AnalyzeAsync(
        Guid candidateId,
        Guid resumeId,
        string jobDescription,
        object? jobData,
        CancellationToken ct);
}