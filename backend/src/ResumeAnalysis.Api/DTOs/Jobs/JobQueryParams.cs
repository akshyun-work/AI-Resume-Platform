namespace ResumeAnalysis.Api.DTOs.Jobs;

public class JobQueryParams
{
    public string? Search { get; set; }
    public string? Location { get; set; }
    public string? EmploymentType { get; set; }
    public bool? IsActive { get; set; }
    public string? Skills { get; set; } // comma-separated
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public int Skip => (Page - 1) * PageSize;
    public int Take => Math.Clamp(PageSize, 1, 100);
}
