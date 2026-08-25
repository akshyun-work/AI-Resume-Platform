namespace ResumeAnalysis.Api.Entities;

public enum ApplicationStatus
{
    Applied = 0,
    Reviewing = 1,
    Shortlisted = 2,
    Rejected = 3,
    Accepted = 4
}

public class JobApplication
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public Candidate Candidate { get; set; } = null!;
    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;
    public Guid? ResumeId { get; set; }
    public Resume? Resume { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
