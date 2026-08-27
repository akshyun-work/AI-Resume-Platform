using System.ComponentModel.DataAnnotations;

namespace ResumeAnalysis.Api.DTOs.Applications;

public class CreateApplicationRequest
{
    [Required]
    public Guid JobId { get; set; }

    public Guid? ResumeId { get; set; }
}
