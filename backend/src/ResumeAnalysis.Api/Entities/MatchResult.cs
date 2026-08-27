namespace ResumeAnalysis.Api.Entities;

public class MatchResult
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public Candidate Candidate { get; set; } = null!;
    public Guid ResumeId { get; set; }
    public Resume Resume { get; set; } = null!;
    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;
    public int MatchScore { get; set; }
    public string? MatchingSkillsJson { get; set; }
    public string? MissingSkillsJson { get; set; }
    public string? MatchingKeywordsJson { get; set; }
    public string? MissingKeywordsJson { get; set; }
    public string? ReasonsJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
