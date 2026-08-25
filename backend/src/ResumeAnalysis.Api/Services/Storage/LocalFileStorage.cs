using Microsoft.Extensions.Options;
using ResumeAnalysis.Api.Configuration;

namespace ResumeAnalysis.Api.Services.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(IOptions<StorageSettings> options, ILogger<LocalFileStorage> logger)
    {
        var settings = options.Value;
        _logger = logger;
        // Resolve relative path against content root; avoid hardcoded absolute machine paths
        var basePath = settings.Path;
        if (!Path.IsPathRooted(basePath))
        {
            // Use AppContext.BaseDirectory parent logic: storage relative to app root
            basePath = Path.Combine(AppContext.BaseDirectory, basePath);
        }
        _basePath = Path.GetFullPath(basePath);
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct)
    {
        // Sanitize filename: ignore original, generate safe name
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext)) ext = ".pdf";
        var safeName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_basePath, safeName);

        // Prevent path traversal: ensure fullPath is under _basePath
        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid storage path.");

        _logger.LogInformation("Saving file {FileName} to {StoragePath}", fileName, safeName);

        using var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(fs, ct);
        await fs.FlushAsync(ct);

        // Return relative storage key (safeName)
        return safeName;
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct)
    {
        var fullPath = GetFullPath(storagePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Resume file not found.", storagePath);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct)
    {
        var fullPath = GetFullPath(storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted file {StoragePath}", storagePath);
        }
        return Task.CompletedTask;
    }

    public bool Exists(string storagePath) => File.Exists(GetFullPath(storagePath));

    private string GetFullPath(string storagePath)
    {
        // storagePath is safeName (no directories); but handle if it contains subpath
        var fileName = Path.GetFileName(storagePath);
        var full = Path.Combine(_basePath, fileName);
        if (!full.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid storage path.");
        return full;
    }
}
