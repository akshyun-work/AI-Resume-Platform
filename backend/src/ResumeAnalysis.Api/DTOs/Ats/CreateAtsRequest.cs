using System.ComponentModel.DataAnnotations;

namespace ResumeAnalysis.Api.DTOs.Ats;

public class CreateAtsRequest
{
    [Required]
    public Guid ResumeId { get; set; }
}