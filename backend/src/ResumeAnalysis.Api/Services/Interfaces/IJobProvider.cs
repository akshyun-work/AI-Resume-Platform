namespace ResumeAnalysis.Api.Services.Interfaces;

public interface IJobProvider
{
	Task<IReadOnlyList<ExternalJob>> SearchJobsAsync(
		string? keywords,
		string? location,
		int limit,
		CancellationToken ct);
}

public class ExternalJob
{
	public string? ExternalId { get; set; }
	public string? Title { get; set; }
	public string? Company { get; set; }
	public string? Location { get; set; }
	public string? Description { get; set; }
	public string? Url { get; set; }
	public string? EmploymentType { get; set; }
	public decimal? SalaryMin { get; set; }
	public decimal? SalaryMax { get; set; }
	public string? SalaryCurrency { get; set; }
	public List<string> Skills { get; set; } = new();
	public List<string> Requirements { get; set; } = new();
	public string? Source { get; set; }
}