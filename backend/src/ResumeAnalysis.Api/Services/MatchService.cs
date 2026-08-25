using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Matches;
using ResumeAnalysis.Api.Entities;
using ResumeAnalysis.Api.Services.Interfaces;

namespace ResumeAnalysis.Api.Services;

public class MatchService : IMatchService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<MatchService> _logger;

    public MatchService(ApplicationDbContext db, ILogger<MatchService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static string? Serialize<T>(T? obj) => obj == null ? null : JsonSerializer.Serialize(obj);
    private static T? Deserialize<T>(string? json) => string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json);

    private static MatchResultDto ToDto(MatchResult m) => new()
    {
        Id = m.Id,
        CandidateId = m.CandidateId,
        ResumeId = m.ResumeId,
        JobId = m.JobId,
        JobTitle = m.Job?.Title ?? string.Empty,
        Company = m.Job?.Company ?? string.Empty,
        MatchScore = m.MatchScore,
        MatchingSkills = Deserialize<List<string>>(m.MatchingSkillsJson),
        MissingSkills = Deserialize<List<string>>(m.MissingSkillsJson),
        MatchingKeywords = Deserialize<List<string>>(m.MatchingKeywordsJson),
        MissingKeywords = Deserialize<List<string>>(m.MissingKeywordsJson),
        Reasons = Deserialize<List<string>>(m.ReasonsJson),
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt
    };

    public async Task<MatchResultDto> CreateAsync(Guid candidateId, CreateMatchRequest request, CancellationToken ct)
    {
        if (request.ResumeId == Guid.Empty) throw new ArgumentException("ResumeId is required.");
        if (request.JobId == Guid.Empty) throw new ArgumentException("JobId is required.");
        if (request.MatchScore < 0 || request.MatchScore > 100)
            throw new ArgumentException("MatchScore must be between 0 and 100.");

        var resume = await _db.Resumes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.ResumeId, ct);
        if (resume == null) throw new KeyNotFoundException("Resume not found.");
        if (resume.CandidateId != candidateId) throw new UnauthorizedAccessException("Access denied to resume.");

        var job = await _db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == request.JobId, ct);
        if (job == null) throw new KeyNotFoundException("Job not found.");

        // Optional: prevent duplicate exact match? Allow multiple but log
        var entity = new MatchResult
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            ResumeId = request.ResumeId,
            JobId = request.JobId,
            MatchScore = request.MatchScore,
            MatchingSkillsJson = Serialize(request.MatchingSkills),
            MissingSkillsJson = Serialize(request.MissingSkills),
            MatchingKeywordsJson = Serialize(request.MatchingKeywords),
            MissingKeywordsJson = Serialize(request.MissingKeywords),
            ReasonsJson = Serialize(request.Reasons),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.MatchResults.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Load job for dto
        entity.Job = job;

        _logger.LogInformation("Match created {MatchId} candidate {CandidateId} resume {ResumeId} job {JobId} score {Score}", entity.Id, candidateId, request.ResumeId, request.JobId, request.MatchScore);

        return ToDto(entity);
    }

    public async Task<MatchResultDto> GetByIdAsync(Guid candidateId, Guid matchId, CancellationToken ct)
    {
        if (matchId == Guid.Empty) throw new ArgumentException("Invalid id.");
        var m = await _db.MatchResults.Include(x => x.Job).AsNoTracking().FirstOrDefaultAsync(x => x.Id == matchId, ct);
        if (m == null) throw new KeyNotFoundException("Match not found.");
        if (m.CandidateId != candidateId) throw new UnauthorizedAccessException("Access denied.");
        return ToDto(m);
    }

    public async Task<IReadOnlyList<MatchResultDto>> ListAsync(Guid candidateId, CancellationToken ct)
    {
        var list = await _db.MatchResults.Include(x => x.Job).AsNoTracking()
            .Where(x => x.CandidateId == candidateId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return list.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<MatchResultDto>> ListForJobAsync(Guid candidateId, Guid jobId, CancellationToken ct)
    {
        var job = await _db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job == null) throw new KeyNotFoundException("Job not found.");
        var list = await _db.MatchResults.Include(x => x.Job).AsNoTracking()
            .Where(x => x.CandidateId == candidateId && x.JobId == jobId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return list.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<MatchResultDto>> ListForResumeAsync(Guid candidateId, Guid resumeId, CancellationToken ct)
    {
        var resume = await _db.Resumes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == resumeId, ct);
        if (resume == null) throw new KeyNotFoundException("Resume not found.");
        if (resume.CandidateId != candidateId) throw new UnauthorizedAccessException("Access denied to resume.");
        var list = await _db.MatchResults.Include(x => x.Job).AsNoTracking()
            .Where(x => x.CandidateId == candidateId && x.ResumeId == resumeId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return list.Select(ToDto).ToList();
    }
}
