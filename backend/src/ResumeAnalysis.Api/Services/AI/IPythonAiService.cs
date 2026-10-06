namespace ResumeAnalysis.Api.Services.AI;

public interface IPythonAiService
{
    Task<string> AnalyzeResumeAsync(
        string pdfPath,
        string jobDescription,
        object? jobData,
        CancellationToken ct);

    Task<string> StructureJobAsync(
        string rawJobDescription,
        CancellationToken ct);

    Task<string> ChatAsync(
        string pdfPath,
        string message,
        object? conversation,
        CancellationToken ct);
}