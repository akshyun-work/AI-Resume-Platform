namespace ResumeAnalysis.Api.DTOs.Matches;

public class MatchResultDto
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public Guid ResumeId { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public int MatchScore { get; set; }
    public List<string>? MatchingSkills { get; set; }
    public List<string>? MissingSkills { get; set; }
    public List<string>? MatchingKeywords { get; set; }
    public List<string>? MissingKeywords { get; set; }
    public List<string>? Reasons { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
