namespace ResumeAnalysis.Api.Configuration;

public class StorageSettings
{
    public string Path { get; set; } = "Storage/Resumes";
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5 MB
    public string[] AllowedExtensions { get; set; } = new[] { ".pdf" };
    public string[] AllowedContentTypes { get; set; } = new[] { "application/pdf" };
}
