using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ResumeAnalysis.Api.Configuration;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Resumes;
using ResumeAnalysis.Api.Entities;
using ResumeAnalysis.Api.Services.Interfaces;
using ResumeAnalysis.Api.Services.Storage;

namespace ResumeAnalysis.Api.Services;

public class ResumeService : IResumeService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly StorageSettings _settings;
    private readonly ILogger<ResumeService> _logger;

    public ResumeService(ApplicationDbContext db, IFileStorage storage, IOptions<StorageSettings> options, ILogger<ResumeService> logger)
    {
        _db = db;
        _storage = storage;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<ResumeDto> UploadAsync(Guid candidateId, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is required.");

        if (file.Length > _settings.MaxFileSizeBytes)
            throw new ArgumentException($"File size exceeds limit of {_settings.MaxFileSizeBytes / (1024 * 1024)} MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext) || !_settings.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Only PDF files are allowed.");

        var contentType = file.ContentType ?? string.Empty;
        if (!_settings.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            if (!string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Only PDF files are allowed.");
        }

        // Validate PDF magic bytes %PDF
        using (var preview = file.OpenReadStream())
        {
            var header = new byte[4];
            var read = await preview.ReadAsync(header, 0, 4, ct);
            preview.Position = 0;
            if (read < 4 || header[0] != 0x25 || header[1] != 0x50 || header[2] != 0x44 || header[3] != 0x46) // %PDF
                throw new ArgumentException("File is not a valid PDF.");
        }

        var candidateExists = await _db.Candidates.AnyAsync(c => c.Id == candidateId, ct);
        if (!candidateExists) throw new KeyNotFoundException("Candidate not found.");

        // Compute next version
        var maxVersion = await _db.Resumes.Where(r => r.CandidateId == candidateId).MaxAsync(r => (int?)r.VersionNumber, ct) ?? 0;
        var nextVersion = maxVersion + 1;

        // Save file via storage abstraction
        string storageKey;
        using (var stream = file.OpenReadStream())
        {
            var ctForStorage = file.ContentType ?? "application/pdf";
            storageKey = await _storage.SaveAsync(stream, file.FileName, ctForStorage, ct);
        }

        // Update previous latest to false
        var previousLatests = await _db.Resumes.Where(r => r.CandidateId == candidateId && r.IsLatest).ToListAsync(ct);
        foreach (var prev in previousLatests)
        {
            prev.IsLatest = false;
            prev.UpdatedAt = DateTime.UtcNow;
        }

        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            FileName = Path.GetFileName(storageKey),
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = "application/pdf",
            FileSizeBytes = file.Length,
            StoragePath = storageKey,
            VersionNumber = nextVersion,
            IsLatest = true,
            Status = ResumeStatus.Uploaded,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Resumes.Add(resume);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true || ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Cleanup orphaned file on version conflict (concurrent upload)
            try { await _storage.DeleteAsync(storageKey, ct); } catch { }
            throw new InvalidOperationException("Concurrent resume upload conflict, please retry.", ex);
        }
        catch (DbUpdateException)
        {
            try { await _storage.DeleteAsync(storageKey, ct); } catch { }
            throw;
        }

        _logger.LogInformation("Resume uploaded {ResumeId} candidate {CandidateId} version {Version} size {Size}", resume.Id, candidateId, nextVersion, file.Length);

        return ResumeDto.FromEntity(resume);
    }

    public async Task<IReadOnlyList<ResumeDto>> ListAsync(Guid candidateId, CancellationToken ct)
    {
        var list = await _db.Resumes
            .AsNoTracking()
            .Where(r => r.CandidateId == candidateId)
            .OrderByDescending(r => r.VersionNumber)
            .ToListAsync(ct);
        return list.Select(ResumeDto.FromEntity).ToList();
    }

    public async Task<ResumeDto> GetAsync(Guid candidateId, Guid resumeId, CancellationToken ct)
    {
        if (resumeId == Guid.Empty) throw new ArgumentException("Invalid id.");
        var r = await _db.Resumes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == resumeId, ct);
        if (r == null) throw new KeyNotFoundException("Resume not found.");
        if (r.CandidateId != candidateId) throw new UnauthorizedAccessException("Access denied.");
        return ResumeDto.FromEntity(r);
    }

    public async Task<(Stream stream, string contentType, string fileName)> DownloadAsync(Guid candidateId, Guid resumeId, CancellationToken ct)
    {
        if (resumeId == Guid.Empty) throw new ArgumentException("Invalid id.");
        var r = await _db.Resumes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == resumeId, ct);
        if (r == null) throw new KeyNotFoundException("Resume not found.");
        if (r.CandidateId != candidateId) throw new UnauthorizedAccessException("Access denied.");
        var stream = await _storage.OpenReadAsync(r.StoragePath, ct);
        return (stream, r.ContentType, r.OriginalFileName);
    }

    public async Task DeleteAsync(Guid candidateId, Guid resumeId, CancellationToken ct)
    {
        if (resumeId == Guid.Empty) throw new ArgumentException("Invalid id.");
        var r = await _db.Resumes.FirstOrDefaultAsync(x => x.Id == resumeId, ct);
        if (r == null) throw new KeyNotFoundException("Resume not found.");
        if (r.CandidateId != candidateId) throw new UnauthorizedAccessException("Access denied.");

        var wasLatest = r.IsLatest;
        var storagePath = r.StoragePath;

        _db.Resumes.Remove(r);
        await _db.SaveChangesAsync(ct);

        try { await _storage.DeleteAsync(storagePath, ct); } catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete file {Path}", storagePath); }

        _logger.LogInformation("Resume deleted {ResumeId} candidate {CandidateId}", resumeId, candidateId);

        if (wasLatest)
        {
            var newLatest = await _db.Resumes.Where(x => x.CandidateId == candidateId).OrderByDescending(x => x.VersionNumber).FirstOrDefaultAsync(ct);
            if (newLatest != null)
            {
                newLatest.IsLatest = true;
                newLatest.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("New latest resume {ResumeId} version {Version}", newLatest.Id, newLatest.VersionNumber);
            }
        }
    }

    public async Task<ResumeDto?> GetLatestAsync(Guid candidateId, CancellationToken ct)
    {
        var r = await _db.Resumes.AsNoTracking()
            .Where(x => x.CandidateId == candidateId && x.IsLatest)
            .FirstOrDefaultAsync(ct);
        if (r == null)
        {
            r = await _db.Resumes.AsNoTracking().Where(x => x.CandidateId == candidateId).OrderByDescending(x => x.VersionNumber).FirstOrDefaultAsync(ct);
        }
        return r == null ? null : ResumeDto.FromEntity(r);
    }
}
