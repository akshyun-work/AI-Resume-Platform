using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ResumeAnalysis.Api.Configuration;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Resumes;
using ResumeAnalysis.Api.Entities;
using ResumeAnalysis.Api.Services.Interfaces;
using ResumeAnalysis.Api.Services.Storage;
using ResumeAnalysis.Api.Services.AI;

namespace ResumeAnalysis.Api.Services;

public class ResumeService : IResumeService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly StorageSettings _settings;
    private readonly ILogger<ResumeService> _logger;
    private readonly IPythonAiService _pythonAiService;
    private readonly IAtsService _atsService;

    public ResumeService(
        ApplicationDbContext db,
        IFileStorage storage,
        IOptions<StorageSettings> options,
        ILogger<ResumeService> logger,
        IPythonAiService pythonAiService,
        IAtsService atsService)
    {
        _db = db;
        _storage = storage;
        _settings = options.Value;
        _logger = logger;
        _pythonAiService = pythonAiService;
        _atsService = atsService;
    }

    public async Task<ResumeDto> UploadAsync(
        Guid candidateId,
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is required.");

        if (file.Length > _settings.MaxFileSizeBytes)
            throw new ArgumentException(
                $"File size exceeds limit of {_settings.MaxFileSizeBytes / (1024 * 1024)} MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(ext) ||
            !_settings.AllowedExtensions.Contains(
                ext,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only PDF files are allowed.");
        }

        var contentType = file.ContentType ?? string.Empty;

        if (!_settings.AllowedContentTypes.Contains(
                contentType,
                StringComparer.OrdinalIgnoreCase))
        {
            if (!string.Equals(
                    contentType,
                    "application/octet-stream",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Only PDF files are allowed.");
            }
        }

        // Validate PDF magic bytes %PDF
        using (var preview = file.OpenReadStream())
        {
            var header = new byte[4];

            var read = await preview.ReadAsync(
                header,
                0,
                4,
                ct);

            preview.Position = 0;

            if (read < 4 ||
                header[0] != 0x25 ||
                header[1] != 0x50 ||
                header[2] != 0x44 ||
                header[3] != 0x46)
            {
                throw new ArgumentException(
                    "File is not a valid PDF.");
            }
        }

        var candidateExists =
            await _db.Candidates.AnyAsync(
                c => c.Id == candidateId,
                ct);

        if (!candidateExists)
            throw new KeyNotFoundException(
                "Candidate not found.");

        // Compute next version
        var maxVersion =
            await _db.Resumes
                .Where(r => r.CandidateId == candidateId)
                .MaxAsync(
                    r => (int?)r.VersionNumber,
                    ct)
            ?? 0;

        var nextVersion = maxVersion + 1;

        // Save file via storage abstraction
        string storageKey;

        using (var stream = file.OpenReadStream())
        {
            var ctForStorage =
                file.ContentType ?? "application/pdf";

            storageKey = await _storage.SaveAsync(
                stream,
                file.FileName,
                ctForStorage,
                ct);
        }

        // Update previous latest to false
        var previousLatests =
            await _db.Resumes
                .Where(r =>
                    r.CandidateId == candidateId &&
                    r.IsLatest)
                .ToListAsync(ct);

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
        catch (DbUpdateException ex)
            when (
                ex.InnerException?.Message.Contains(
                    "duplicate",
                    StringComparison.OrdinalIgnoreCase) == true ||
                ex.InnerException?.Message.Contains(
                    "unique",
                    StringComparison.OrdinalIgnoreCase) == true)
        {
            // Cleanup orphaned file on version conflict
            try
            {
                await _storage.DeleteAsync(
                    storageKey,
                    ct);
            }
            catch
            {
                // Ignore cleanup failure
            }

            throw new InvalidOperationException(
                "Concurrent resume upload conflict, please retry.",
                ex);
        }
        catch (DbUpdateException)
        {
            try
            {
                await _storage.DeleteAsync(
                    storageKey,
                    ct);
            }
            catch
            {
                // Ignore cleanup failure
            }

            throw;
        }

        _logger.LogInformation(
            "Resume uploaded {ResumeId} candidate {CandidateId} version {Version} size {Size}",
            resume.Id,
            candidateId,
            resume.VersionNumber,
            file.Length);

        try
        {
            resume.Status = ResumeStatus.Processing;
            resume.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            // Initial resume-only ATS analysis.
            // No job is selected at this point.
            await _atsService.CreateAsync(
                candidateId,
                new ResumeAnalysis.Api.DTOs.Ats.CreateAtsRequest
                {
                    ResumeId = resume.Id
                },
                ct);

            resume.Status = ResumeStatus.Processed;
            resume.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Automatic ATS analysis completed for resume {ResumeId}",
                resume.Id);
        }
        catch (Exception ex)
        {
            resume.Status = ResumeStatus.Failed;
            resume.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(
                CancellationToken.None);

            _logger.LogError(
                ex,
                "Automatic ATS analysis failed for resume {ResumeId}",
                resume.Id);

            throw;
        }

        return ResumeDto.FromEntity(resume);
    }

    public async Task<IReadOnlyList<ResumeDto>> ListAsync(
        Guid candidateId,
        CancellationToken ct)
    {
        var list = await _db.Resumes
            .AsNoTracking()
            .Where(r => r.CandidateId == candidateId)
            .OrderByDescending(r => r.VersionNumber)
            .ToListAsync(ct);

        return list
            .Select(ResumeDto.FromEntity)
            .ToList();
    }

    public async Task<ResumeDto> GetAsync(
        Guid candidateId,
        Guid resumeId,
        CancellationToken ct)
    {
        if (resumeId == Guid.Empty)
            throw new ArgumentException(
                "Invalid id.");

        var r = await _db.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == resumeId,
                ct);

        if (r == null)
            throw new KeyNotFoundException(
                "Resume not found.");

        if (r.CandidateId != candidateId)
            throw new UnauthorizedAccessException(
                "Access denied.");

        return ResumeDto.FromEntity(r);
    }

    public async Task<(
        Stream stream,
        string contentType,
        string fileName)> DownloadAsync(
        Guid candidateId,
        Guid resumeId,
        CancellationToken ct)
    {
        if (resumeId == Guid.Empty)
            throw new ArgumentException(
                "Invalid id.");

        var r = await _db.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == resumeId,
                ct);

        if (r == null)
            throw new KeyNotFoundException(
                "Resume not found.");

        if (r.CandidateId != candidateId)
            throw new UnauthorizedAccessException(
                "Access denied.");

        var stream =
            await _storage.OpenReadAsync(
                r.StoragePath,
                ct);

        return (
            stream,
            r.ContentType,
            r.OriginalFileName);
    }

    public async Task DeleteAsync(
        Guid candidateId,
        Guid resumeId,
        CancellationToken ct)
    {
        if (resumeId == Guid.Empty)
            throw new ArgumentException("Invalid id.");

        var totalResumesCount = await _db.Resumes
            .CountAsync(x => x.CandidateId == candidateId, ct);

        if (totalResumesCount <= 1)
        {
            throw new InvalidOperationException("You must keep at least one resume on your profile. To update your resume, upload a new version instead.");
        }

        var r = await _db.Resumes
            .FirstOrDefaultAsync(
                x => x.Id == resumeId,
                ct);

        if (r == null)
            throw new KeyNotFoundException("Resume not found.");

        if (r.CandidateId != candidateId)
            throw new UnauthorizedAccessException("Access denied.");

        var wasLatest = r.IsLatest;
        var storagePath = r.StoragePath;

        // Remove linked MatchResults for this resume
        var matchResults = await _db.MatchResults
            .Where(m => m.ResumeId == resumeId)
            .ToListAsync(ct);
        if (matchResults.Count > 0)
        {
            _db.MatchResults.RemoveRange(matchResults);
        }

        // Remove linked AtsAnalyses for this resume
        var atsAnalyses = await _db.AtsAnalyses
            .Where(a => a.ResumeId == resumeId)
            .ToListAsync(ct);
        if (atsAnalyses.Count > 0)
        {
            _db.AtsAnalyses.RemoveRange(atsAnalyses);
        }

        // Find remaining candidate resumes
        var remainingResumes = await _db.Resumes
            .Where(x => x.CandidateId == candidateId && x.Id != resumeId)
            .OrderByDescending(x => x.VersionNumber)
            .ToListAsync(ct);

        var fallbackResume = remainingResumes.FirstOrDefault();

        // Reassign any JobApplications using this resume to the fallback resume
        if (fallbackResume != null)
        {
            var applications = await _db.Applications
                .Where(a => a.ResumeId == resumeId)
                .ToListAsync(ct);

            foreach (var app in applications)
            {
                app.ResumeId = fallbackResume.Id;
                app.UpdatedAt = DateTime.UtcNow;
            }

            if (wasLatest)
            {
                fallbackResume.IsLatest = true;
                fallbackResume.UpdatedAt = DateTime.UtcNow;
            }
        }

        _db.Resumes.Remove(r);

        await _db.SaveChangesAsync(ct);

        try
        {
            await _storage.DeleteAsync(storagePath, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file {Path}", storagePath);
        }

        _logger.LogInformation(
            "Resume deleted {ResumeId} candidate {CandidateId}. New latest: {NewLatestId}",
            resumeId,
            candidateId,
            fallbackResume?.Id);
    }

    public async Task<ResumeDto?> GetLatestAsync(
        Guid candidateId,
        CancellationToken ct)
    {
        var r = await _db.Resumes
            .AsNoTracking()
            .Where(x =>
                x.CandidateId == candidateId &&
                x.IsLatest)
            .FirstOrDefaultAsync(ct);

        if (r == null)
        {
            r = await _db.Resumes
                .AsNoTracking()
                .Where(x =>
                    x.CandidateId == candidateId)
                .OrderByDescending(
                    x => x.VersionNumber)
                .FirstOrDefaultAsync(ct);
        }

        return r == null
            ? null
            : ResumeDto.FromEntity(r);
    }

    public async Task<string> AnalyzeAsync(
        Guid candidateId,
        Guid resumeId,
        string jobDescription,
        object? jobData,
        CancellationToken ct)
    {
        if (resumeId == Guid.Empty)
            throw new ArgumentException(
                "Invalid resume id.");

        if (string.IsNullOrWhiteSpace(jobDescription))
            throw new ArgumentException(
                "Job description is required.");

        var resume = await _db.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == resumeId,
                ct);

        if (resume == null)
            throw new KeyNotFoundException(
                "Resume not found.");

        if (resume.CandidateId != candidateId)
            throw new UnauthorizedAccessException(
                "Access denied to resume.");

        // LocalFileStorage stores files under:
        // AppContext.BaseDirectory/Storage/Resumes
        var storageDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "Storage",
            "Resumes");

        var pdfPath = Path.Combine(
            storageDirectory,
            Path.GetFileName(resume.StoragePath));

        if (!File.Exists(pdfPath))
            throw new FileNotFoundException(
                "Stored resume file not found.",
                pdfPath);

        _logger.LogInformation(
            "Starting AI analysis for resume {ResumeId} candidate {CandidateId}",
            resumeId,
            candidateId);

        // ------------------------------------------------------------
        // 1. Run the job-specific analysis requested by the frontend.
        // ------------------------------------------------------------

        var result =
            await _pythonAiService.AnalyzeResumeAsync(
                pdfPath,
                jobDescription,
                jobData,
                ct);

        _logger.LogInformation(
            "AI job analysis completed for resume {ResumeId} candidate {CandidateId}",
            resumeId,
            candidateId);

        // ------------------------------------------------------------
        // 2. Persist a job-specific ATS analysis.
        //
        // CreateAtsRequest now accepts the selected job description
        // and job data, so AtsService can populate:
        //
        // - KeywordsIdentified
        // - MissingKeywords
        // - MissingSkills
        // - Issues
        // - Recommendations
        //
        // The initial upload analysis remains resume-only.
        // ------------------------------------------------------------

        try
        {
            await _atsService.CreateAsync(
                candidateId,
                new ResumeAnalysis.Api.DTOs.Ats.CreateAtsRequest
                {
                    ResumeId = resumeId,
                    JobDescription = jobDescription,
                    JobData = jobData
                },
                ct);

            _logger.LogInformation(
                "Job-specific ATS analysis persisted for resume {ResumeId} candidate {CandidateId}",
                resumeId,
                candidateId);
        }
        catch (Exception ex)
        {
            // Do not hide the successful job-analysis result if ATS
            // persistence fails. Log it so the backend exposes the
            // persistence problem during testing.
            _logger.LogError(
                ex,
                "Failed to persist job-specific ATS analysis for resume {ResumeId}",
                resumeId);
        }

        return result;
    }
}