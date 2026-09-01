using System.ComponentModel.DataAnnotations;

namespace ResumeAnalysis.Api.DTOs.Ats;

public class CreateAtsRequest
{
    [Required]
    public Guid ResumeId { get; set; }

    public string? JobDescription { get; set; }

    public object? JobData { get; set; }
}