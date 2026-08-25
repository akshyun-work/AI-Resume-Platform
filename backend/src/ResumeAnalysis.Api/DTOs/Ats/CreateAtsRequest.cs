using System.ComponentModel.DataAnnotations;

namespace ResumeAnalysis.Api.DTOs.Ats;

public class CreateAtsRequest
{
    [Required]
    public Guid ResumeId { get; set; }

    [Range(0, 100)]
    public int OverallScore { get; set; }

    public Dictionary<string, int>? CategoryScores { get; set; }

    public List<string>? SkillsIdentified { get; set; }

    public List<string>? KeywordsIdentified { get; set; }

    public List<string>? MissingKeywords { get; set; }

    public List<string>? MissingSkills { get; set; }

    public List<string>? Issues { get; set; }

    public List<string>? Recommendations { get; set; }
}
