using ResumeAnalysis.Api.Entities;

namespace ResumeAnalysis.Api.DTOs.Resumes;

public class ResumeDto
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int VersionNumber { get; set; }
    public bool IsLatest { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ResumeDto FromEntity(Resume r) => new()
    {
        Id = r.Id,
        CandidateId = r.CandidateId,
        FileName = r.FileName,
        OriginalFileName = r.OriginalFileName,
        ContentType = r.ContentType,
        FileSizeBytes = r.FileSizeBytes,
        VersionNumber = r.VersionNumber,
        IsLatest = r.IsLatest,
        Status = r.Status.ToString(),
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
}
