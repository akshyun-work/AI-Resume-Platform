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

    public async Task<(IReadOnlyList<JobDto> items, int total)> SearchAsync(JobQueryParams query, CancellationToken ct)
    {
        var q = _db.Jobs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLowerInvariant();
            q = q.Where(j => j.Title.ToLower().Contains(s) || j.Description.ToLower().Contains(s) || j.Company.ToLower().Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var loc = query.Location.Trim().ToLowerInvariant();
            q = q.Where(j => j.Location != null && j.Location.ToLower().Contains(loc));
        }
        if (!string.IsNullOrWhiteSpace(query.EmploymentType))
        {
            var et = query.EmploymentType.Trim().ToLowerInvariant();
            q = q.Where(j => j.EmploymentType != null && j.EmploymentType.ToLower().Contains(et));
        }
        if (query.IsActive.HasValue)
            q = q.Where(j => j.IsActive == query.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(query.Skills))
        {
            var skills = query.Skills.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToLowerInvariant()).ToArray();
            if (skills.Length > 0)
            {
                // Filter in memory after fetching, because JSON search is DB-specific; do simple Contains on JSON string
                // For SQL Server we could use JSON functions, but keep simple: fetch then filter
                var all = await q.ToListAsync(ct);
                var filtered = all.Where(j =>
                {
                    var req = Deserialize(j.RequiredSkillsJson) ?? new List<string>();
                    var pref = Deserialize(j.PreferredSkillsJson) ?? new List<string>();
                    var combined = req.Concat(pref).Select(s => s.ToLowerInvariant()).ToHashSet();
                    return skills.Any(s => combined.Any(c => c.Contains(s)));
                }).ToList();
                var total = filtered.Count;
                var paged = filtered.OrderByDescending(j => j.CreatedAt).Skip(query.Skip).Take(query.Take).Select(ToDto).ToList();
                return (paged, total);
            }
        }

        var totalCount = await q.CountAsync(ct);
        var items = await q.OrderByDescending(j => j.CreatedAt).Skip(query.Skip).Take(query.Take).ToListAsync(ct);
        return (items.Select(ToDto).ToList(), totalCount);
    }
}
