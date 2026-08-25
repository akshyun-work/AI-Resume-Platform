using Microsoft.EntityFrameworkCore;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Applications;
using ResumeAnalysis.Api.Entities;
using ResumeAnalysis.Api.Services.Interfaces;

namespace ResumeAnalysis.Api.Services;

public class ApplicationService : IApplicationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(ApplicationDbContext db, ILogger<ApplicationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static ApplicationDto ToDto(JobApplication a) => new()
    {
        Id = a.Id,
        CandidateId = a.CandidateId,
        JobId = a.JobId,
        JobTitle = a.Job?.Title ?? string.Empty,
        Company = a.Job?.Company ?? string.Empty,
        ResumeId = a.ResumeId,
        Status = a.Status.ToString(),
        AppliedAt = a.AppliedAt,
        UpdatedAt = a.UpdatedAt
    };

    public async Task<ApplicationDto> ApplyAsync(Guid candidateId, CreateApplicationRequest request, CancellationToken ct)
    {
        if (request.JobId == Guid.Empty) throw new ArgumentException("JobId is required.");
        if (request.ResumeId.HasValue && request.ResumeId.Value == Guid.Empty) throw new ArgumentException("Invalid ResumeId.");
        var job = await _db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == request.JobId, ct);
        if (job == null) throw new KeyNotFoundException("Job not found.");
        if (!job.IsActive) throw new InvalidOperationException("Job is not active.");

        if (request.ResumeId.HasValue)
        {
            var resume = await _db.Resumes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.ResumeId.Value, ct);
            if (resume == null) throw new KeyNotFoundException("Resume not found.");
            if (resume.CandidateId != candidateId) throw new UnauthorizedAccessException("Access denied to resume.");
        }

        var exists = await _db.Applications.AnyAsync(a => a.CandidateId == candidateId && a.JobId == request.JobId, ct);
        if (exists) throw new InvalidOperationException("Already applied to this job.");

        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            JobId = request.JobId,
            ResumeId = request.ResumeId,
            Status = ApplicationStatus.Applied,
            AppliedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Applications.Add(app);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true || ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new InvalidOperationException("Already applied to this job.", ex);
        }

        // Load job for DTO
        app.Job = job;

        _logger.LogInformation("Application created {ApplicationId} candidate {CandidateId} job {JobId}", app.Id, candidateId, request.JobId);

        return ToDto(app);
    }

    public async Task<IReadOnlyList<ApplicationDto>> ListAsync(Guid candidateId, CancellationToken ct)
    {
        var list = await _db.Applications
            .AsNoTracking()
            .Include(a => a.Job)
            .Where(a => a.CandidateId == candidateId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync(ct);
        return list.Select(ToDto).ToList();
    }

    public async Task<ApplicationDto> GetByIdAsync(Guid candidateId, Guid applicationId, CancellationToken ct)
    {
        if (applicationId == Guid.Empty) throw new ArgumentException("Invalid id.");
        var app = await _db.Applications
            .AsNoTracking()
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (app == null) throw new KeyNotFoundException("Application not found.");
        if (app.CandidateId != candidateId) throw new UnauthorizedAccessException("Access denied.");
        return ToDto(app);
    }
}
