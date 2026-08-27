namespace ResumeAnalysis.Api.DTOs.Ats;

public class AtsAnalysisDto
{
    public Guid Id { get; set; }
    public Guid ResumeId { get; set; }
    public Guid CandidateId { get; set; }
    public int OverallScore { get; set; }
    public Dictionary<string, int>? CategoryScores { get; set; }
    public List<string>? SkillsIdentified { get; set; }
    public List<string>? KeywordsIdentified { get; set; }
    public List<string>? MissingKeywords { get; set; }
    public List<string>? MissingSkills { get; set; }
    public List<string>? Issues { get; set; }
    public List<string>? Recommendations { get; set; }
    public DateTime AnalyzedAt { get; set; }
}

public class CategoryScoreDto
{
    public string Category { get; set; } = string.Empty;
    public int Score { get; set; }
}
