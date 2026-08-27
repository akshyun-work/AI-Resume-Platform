namespace ResumeAnalysis.Api.Entities;

public class AtsAnalysis
{
    public Guid Id { get; set; }
    public Guid ResumeId { get; set; }
    public Resume Resume { get; set; } = null!;
    public Guid CandidateId { get; set; }
    public Candidate Candidate { get; set; } = null!;
    public int OverallScore { get; set; }
    public string? CategoryScoresJson { get; set; }
    public string? SkillsIdentifiedJson { get; set; }
    public string? KeywordsIdentifiedJson { get; set; }
    public string? MissingKeywordsJson { get; set; }
    public string? MissingSkillsJson { get; set; }
    public string? IssuesJson { get; set; }
    public string? RecommendationsJson { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}
