using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Jobs;
using ResumeAnalysis.Api.Entities;
using ResumeAnalysis.Api.Services.Interfaces;

namespace ResumeAnalysis.Api.Services;

public class JobService : IJobService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<JobService> _logger;

    public JobService(ApplicationDbContext db, ILogger<JobService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static string? Serialize(List<string>? list) => list == null ? null : JsonSerializer.Serialize(list);
    private static List<string>? Deserialize(string? json) => string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<List<string>>(json);

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
        IsActive = j.IsActive,
        CreatedAt = j.CreatedAt,
        UpdatedAt = j.UpdatedAt
    };

    public async Task<JobDto> CreateAsync(CreateJobRequest request, CancellationToken ct)
    {
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Company = request.Company.Trim(),
            Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
            EmploymentType = string.IsNullOrWhiteSpace(request.EmploymentType) ? null : request.EmploymentType.Trim(),
            RequiredSkillsJson = Serialize(request.RequiredSkills),
            PreferredSkillsJson = Serialize(request.PreferredSkills),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Jobs.Add(job);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Job created {JobId} {Title} {Company}", job.Id, job.Title, job.Company);
        return ToDto(job);
    }

    public async Task<JobDto> UpdateAsync(Guid jobId, UpdateJobRequest request, CancellationToken ct)
    {
        if (jobId == Guid.Empty) throw new ArgumentException("Invalid id.");
        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job == null) throw new KeyNotFoundException("Job not found.");

        job.Title = request.Title.Trim();
        job.Description = request.Description.Trim();
        job.Company = request.Company.Trim();
        job.Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();
        job.EmploymentType = string.IsNullOrWhiteSpace(request.EmploymentType) ? null : request.EmploymentType.Trim();
        job.RequiredSkillsJson = Serialize(request.RequiredSkills);
        job.PreferredSkillsJson = Serialize(request.PreferredSkills);
        job.IsActive = request.IsActive;
        job.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Job updated {JobId}", jobId);
        return ToDto(job);
    }

    public async Task DeleteAsync(Guid jobId, CancellationToken ct)
    {
        if (jobId == Guid.Empty) throw new ArgumentException("Invalid id.");
        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job == null) throw new KeyNotFoundException("Job not found.");
        _db.Jobs.Remove(job);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Job deleted {JobId}", jobId);
    }

    public async Task<JobDto> GetByIdAsync(Guid jobId, CancellationToken ct)
    {
        if (jobId == Guid.Empty) throw new ArgumentException("Invalid id.");
        var job = await _db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job == null) throw new KeyNotFoundException("Job not found.");
        return ToDto(job);
    }

    public async Task<(IReadOnlyList<JobDto> items, int total)> SearchAsync(
    JobQueryParams query,
    CancellationToken ct)
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

        // --------------------------------------------------
        // TOTAL BEFORE PAGINATION
        // --------------------------------------------------

        var totalCount = await q.CountAsync(ct);

        // --------------------------------------------------
        // PAGINATION
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
}
