using System.ComponentModel.DataAnnotations;

namespace ResumeAnalysis.Api.DTOs.Resumes;

public class AnalyzeResumeRequest
{
	[Required]
	public string JobDescription { get; set; } = string.Empty;

	public object? JobData { get; set; }
}