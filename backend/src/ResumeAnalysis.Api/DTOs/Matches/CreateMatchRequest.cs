using System.ComponentModel.DataAnnotations;

namespace ResumeAnalysis.Api.DTOs.Matches;

public class CreateMatchRequest
{
    [Required]
    public Guid ResumeId { get; set; }

    [Required]
    public Guid JobId { get; set; }

    [Range(0, 100)]
    public int MatchScore { get; set; }

    public List<string>? MatchingSkills { get; set; }
    public List<string>? MissingSkills { get; set; }
    public List<string>? MatchingKeywords { get; set; }
    public List<string>? MissingKeywords { get; set; }
    public List<string>? Reasons { get; set; }
}
