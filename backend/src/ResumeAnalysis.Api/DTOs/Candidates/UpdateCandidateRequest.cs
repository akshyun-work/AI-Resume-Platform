using System.ComponentModel.DataAnnotations;

namespace ResumeAnalysis.Api.DTOs.Candidates;

public class UpdateCandidateRequest
{
    [Required, MinLength(2), MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }
}
