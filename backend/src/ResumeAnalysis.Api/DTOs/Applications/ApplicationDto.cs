using ResumeAnalysis.Api.Entities;

namespace ResumeAnalysis.Api.DTOs.Applications;

public class ApplicationDto
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public Guid? ResumeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
