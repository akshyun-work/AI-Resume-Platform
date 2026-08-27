namespace ResumeAnalysis.Api.Services.Storage;

public interface IFileStorage
{
    Task<string> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct);

    Task<Stream> OpenReadAsync(
        string storagePath,
        CancellationToken ct);

    Task DeleteAsync(
        string storagePath,
        CancellationToken ct);

    bool Exists(string storagePath);

    string GetFullPath(string storagePath);
}