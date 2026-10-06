using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Jobs;
using ResumeAnalysis.Api.Entities;
using ResumeAnalysis.Api.Services.Interfaces;
using ResumeAnalysis.Api.Services.AI;

namespace ResumeAnalysis.Api.Services;

public class JobService : IJobService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<JobService> _logger;
    private readonly IJobProvider _jobProvider;
    private readonly IPythonAiService _pythonAiService;

    public JobService(
        ApplicationDbContext db,
        ILogger<JobService> logger,
        IJobProvider jobProvider,
        IPythonAiService pythonAiService)
    {
        _db = db;
        _logger = logger;
        _jobProvider = jobProvider;
        _pythonAiService = pythonAiService;
    }

    private static string? Serialize(List<string>? list) =>
        list == null ? null : JsonSerializer.Serialize(list);

    private static List<string>? Deserialize(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<List<string>>(json);

    private static JsonElement? DeserializeStructured(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch
        {
            return null;
        }
    }

    private static JobDto ToDto(Job j) => new()
    {
        Id = j.Id,
        Title = j.Title,
        Description = j.Description,
        Company = j.Company,
        Location = j.Location,
        EmploymentType = j.EmploymentType,
        RequiredSkills = Deserialize(j.RequiredSkillsJson),
        PreferredSkills = Deserialize(j.PreferredSkillsJson),
        Structured = DeserializeStructured(j.StructuredJson),
        IsActive = j.IsActive,
        CreatedAt = j.CreatedAt,
        UpdatedAt = j.UpdatedAt
    };

    public async Task<JobDto> CreateAsync(
        CreateJobRequest request,
        CancellationToken ct)
    {
        string? structuredJson = null;
        try
        {
            structuredJson = await _pythonAiService.StructureJobAsync(request.Description, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to structure job description via AI during creation.");
        }

        var requiredSkills = request.RequiredSkills;
        var preferredSkills = request.PreferredSkills;

        // If skills were not provided, extract from structured JSON if available
        if ((requiredSkills == null || requiredSkills.Count == 0) && !string.IsNullOrWhiteSpace(structuredJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(structuredJson);
                if (doc.RootElement.TryGetProperty("requiredSkills", out var reqEl) && reqEl.ValueKind == JsonValueKind.Array)
                {
                    requiredSkills = JsonSerializer.Deserialize<List<string>>(reqEl.GetRawText());
                }
                if (doc.RootElement.TryGetProperty("preferredSkills", out var prefEl) && prefEl.ValueKind == JsonValueKind.Array)
                {
                    preferredSkills = JsonSerializer.Deserialize<List<string>>(prefEl.GetRawText());
                }
            }
            catch { }
        }

        var job = new Job
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Company = request.Company.Trim(),
            Location = string.IsNullOrWhiteSpace(request.Location)
                ? null
                : request.Location.Trim(),
            EmploymentType = string.IsNullOrWhiteSpace(request.EmploymentType)
                ? null
                : request.EmploymentType.Trim(),
            RequiredSkillsJson = Serialize(requiredSkills),
            PreferredSkillsJson = Serialize(preferredSkills),
            StructuredJson = structuredJson,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Jobs.Add(job);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Job created {JobId} {Title} {Company}",
            job.Id,
            job.Title,
            job.Company);

        return ToDto(job);
    }

    public async Task<JobDto> UpdateAsync(
        Guid jobId,
        UpdateJobRequest request,
        CancellationToken ct)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Invalid id.");

        var job = await _db.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);

        if (job == null)
            throw new KeyNotFoundException("Job not found.");

        job.Title = request.Title.Trim();
        job.Description = request.Description.Trim();
        job.Company = request.Company.Trim();

        job.Location = string.IsNullOrWhiteSpace(request.Location)
            ? null
            : request.Location.Trim();

        job.EmploymentType = string.IsNullOrWhiteSpace(request.EmploymentType)
            ? null
            : request.EmploymentType.Trim();

        job.RequiredSkillsJson = Serialize(request.RequiredSkills);
        job.PreferredSkillsJson = Serialize(request.PreferredSkills);
        job.IsActive = request.IsActive;
        job.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Job updated {JobId}",
            jobId);

        return ToDto(job);
    }

    public async Task DeleteAsync(
        Guid jobId,
        CancellationToken ct)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Invalid id.");

        var job = await _db.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);

        if (job == null)
            throw new KeyNotFoundException("Job not found.");

        _db.Jobs.Remove(job);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Job deleted {JobId}",
            jobId);
    }

    public async Task<JobDto> GetByIdAsync(
        Guid jobId,
        CancellationToken ct)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Invalid id.");

        var job = await _db.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);

        if (job == null)
            throw new KeyNotFoundException("Job not found.");

        if (string.IsNullOrWhiteSpace(job.StructuredJson))
        {
            try
            {
                var structured = await _pythonAiService.StructureJobAsync(job.Description, ct);
                if (!string.IsNullOrWhiteSpace(structured) && structured != "{}")
                {
                    job.StructuredJson = structured;
                    job.UpdatedAt = DateTime.UtcNow;

                    // Also sync skills if they were missing
                    if (string.IsNullOrWhiteSpace(job.RequiredSkillsJson))
                    {
                        using var doc = JsonDocument.Parse(structured);
                        if (doc.RootElement.TryGetProperty("requiredSkills", out var reqEl) && reqEl.ValueKind == JsonValueKind.Array)
                        {
                            job.RequiredSkillsJson = reqEl.GetRawText();
                        }
                    }

                    await _db.SaveChangesAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to lazily structure job {JobId}", jobId);
            }
        }

        return ToDto(job);
    }

    public async Task<(IReadOnlyList<JobDto> items, int total)> SearchAsync(
    JobQueryParams query,
    CancellationToken ct)
    {
        // --------------------------------------------------
        // 1. Search existing local jobs
        // --------------------------------------------------

        var q = BuildSearchQuery(query);

        var totalCount = await q.CountAsync(ct);

        // --------------------------------------------------
        // 2. If a search keyword was provided, also query
        //    the external job provider.
        //
        //    We do this even when local jobs already exist.
        //    This allows new external jobs to appear.
        // --------------------------------------------------

        if (!string.IsNullOrWhiteSpace(query.Search) ||!string.IsNullOrWhiteSpace(query.Location))
        {
            _logger.LogInformation(
                "Searching external provider for '{Search}' with location '{Location}'.",
                query.Search,
                query.Location);

            var externalJobs = await _jobProvider.SearchJobsAsync(
                query.Search,
                query.Location,
                query.Take,
                ct);

            if (externalJobs.Count > 0)
            {
                var importedCount = 0;

                foreach (var externalJob in externalJobs)
                {
                    // ------------------------------------------
                    // Ignore incomplete external records.
                    // ------------------------------------------

                    if (string.IsNullOrWhiteSpace(externalJob.Title) ||
                        string.IsNullOrWhiteSpace(externalJob.Company) ||
                        string.IsNullOrWhiteSpace(externalJob.Description))
                    {
                        continue;
                    }

                    var title = externalJob.Title.Trim();
                    var company = externalJob.Company.Trim();

                    var location = string.IsNullOrWhiteSpace(externalJob.Location)
                        ? null
                        : externalJob.Location.Trim();

                    // ------------------------------------------
                    // Avoid importing obvious duplicates.
                    // ------------------------------------------

                    var alreadyExists = await _db.Jobs.AnyAsync(
                        j =>
                            j.Title == title &&
                            j.Company == company &&
                            j.Location == location,
                        ct);

                    if (alreadyExists)
                    {
                        continue;
                    }

                    // ------------------------------------------
                    // Convert external job into our existing
                    // Job entity.
                    // ------------------------------------------

                    var job = new Job
                    {
                        Id = Guid.NewGuid(),

                        Title = title,

                        Description = externalJob.Description.Trim(),

                        Company = company,

                        Location = location,

                        EmploymentType =
                            string.IsNullOrWhiteSpace(externalJob.EmploymentType)
                                ? null
                                : externalJob.EmploymentType.Trim(),

                        RequiredSkillsJson =
                            Serialize(
                                externalJob.Skills.Count > 0
                                    ? externalJob.Skills
                                    : null),

                        PreferredSkillsJson = null,

                        IsActive = true,

                        CreatedAt = DateTime.UtcNow,

                        UpdatedAt = DateTime.UtcNow
                    };

                    _db.Jobs.Add(job);

                    importedCount++;
                }

                if (importedCount > 0)
                {
                    await _db.SaveChangesAsync(ct);

                    _logger.LogInformation(
                        "Imported {ImportedCount} new external jobs.",
                        importedCount);
                }
                else
                {
                    _logger.LogInformation(
                        "External jobs were returned, but no new jobs were imported.");
                }
            }
            else
            {
                _logger.LogInformation(
                    "External provider returned no jobs for '{Search}'.",
                    query.Search);
            }

            // --------------------------------------------------
            // 3. Rebuild the local search after importing.
            // --------------------------------------------------

            q = BuildSearchQuery(query);

            totalCount = await q.CountAsync(ct);
        }

        // --------------------------------------------------
        // 4. Existing pagination
        // --------------------------------------------------

        var items = await q
            .OrderByDescending(j => j.CreatedAt)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync(ct);

        return (
            items.Select(ToDto).ToList(),
            totalCount);
    }

    // ============================================================
    // Build the existing database search query
    // ============================================================

    private IQueryable<Job> BuildSearchQuery(
        JobQueryParams query)
    {
        var q = _db.Jobs
            .AsNoTracking()
            .AsQueryable();

        // --------------------------------------------------
        // TEXT SEARCH
        // Searches:
        // Title
        // Description
        // Company
        // Required skills
        // Preferred skills
        // --------------------------------------------------

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchTerms = query.Search
                .Trim()
                .ToLowerInvariant()
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (var term in searchTerms)
            {
                q = q.Where(j =>
                    j.Title.ToLower().Contains(term) ||
                    j.Description.ToLower().Contains(term) ||
                    j.Company.ToLower().Contains(term) ||
                    (j.RequiredSkillsJson != null &&
                     j.RequiredSkillsJson.ToLower().Contains(term)) ||
                    (j.PreferredSkillsJson != null &&
                     j.PreferredSkillsJson.ToLower().Contains(term)));
            }
        }

        // --------------------------------------------------
        // LOCATION SEARCH
        // --------------------------------------------------

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var location = query.Location
                .Trim()
                .ToLowerInvariant();

            q = q.Where(j =>
                j.Location != null &&
                j.Location.ToLower().Contains(location));
        }

        // --------------------------------------------------
        // EMPLOYMENT TYPE
        // --------------------------------------------------

        if (!string.IsNullOrWhiteSpace(query.EmploymentType))
        {
            var employmentType = query.EmploymentType
                .Trim()
                .ToLowerInvariant();

            q = q.Where(j =>
                j.EmploymentType != null &&
                j.EmploymentType.ToLower().Contains(employmentType));
        }

        // --------------------------------------------------
        // ACTIVE FILTER
        // Only apply this when explicitly supplied.
        // --------------------------------------------------

        if (query.IsActive.HasValue)
        {
            q = q.Where(j =>
                j.IsActive == query.IsActive.Value);
        }

        return q;
    }
}