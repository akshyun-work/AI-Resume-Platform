using System.ComponentModel.DataAnnotations;

namespace ResumeAnalysis.Api.DTOs.Jobs;

public class CreateJobRequest
{
    [Required, MinLength(2), MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MinLength(10)]
    public string Description { get; set; } = string.Empty;

    [Required, MinLength(2), MaxLength(200)]
    public string Company { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Location { get; set; }

    [MaxLength(50)]
    public string? EmploymentType { get; set; }

    public List<string>? RequiredSkills { get; set; }

    public List<string>? PreferredSkills { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateJobRequest
{
    [Required, MinLength(2), MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MinLength(10)]
    public string Description { get; set; } = string.Empty;

    [Required, MinLength(2), MaxLength(200)]
    public string Company { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Location { get; set; }

    [MaxLength(50)]
    public string? EmploymentType { get; set; }

    public List<string>? RequiredSkills { get; set; }

    public List<string>? PreferredSkills { get; set; }

    public bool IsActive { get; set; } = true;
}
