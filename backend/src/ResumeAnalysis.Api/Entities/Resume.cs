namespace ResumeAnalysis.Api.Entities;

public enum ResumeStatus
{
    Uploaded = 0,
    Processing = 1,
    Processed = 2,
    Failed = 3
}

public class Resume
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public Candidate Candidate { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public bool IsLatest { get; set; }
    public ResumeStatus Status { get; set; } = ResumeStatus.Uploaded;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AtsAnalysis> Analyses { get; set; } = new List<AtsAnalysis>();
    public ICollection<MatchResult> MatchResults { get; set; } = new List<MatchResult>();
}
