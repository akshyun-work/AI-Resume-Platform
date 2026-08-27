using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Matches;
using ResumeAnalysis.Api.Entities;
using ResumeAnalysis.Api.Services.Interfaces;
using ResumeAnalysis.Api.Services.AI;

namespace ResumeAnalysis.Api.Services;

public class MatchService : IMatchService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<MatchService> _logger;
    private readonly IPythonAiService _pythonAiService;

    public MatchService(ApplicationDbContext db, ILogger<MatchService> logger, IPythonAiService pythonAiService)
    {
        _db = db;
        _logger = logger;
        _pythonAiService = pythonAiService;
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

    public async Task<MatchResultDto> CreateAsync(
    Guid candidateId,
    CreateMatchRequest request,
    CancellationToken ct)
    {
        if (request.ResumeId == Guid.Empty)
            throw new ArgumentException("ResumeId is required.");

        if (request.JobId == Guid.Empty)
            throw new ArgumentException("JobId is required.");

        var resume = await _db.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ResumeId, ct);

        if (resume == null)
            throw new KeyNotFoundException("Resume not found.");

        if (resume.CandidateId != candidateId)
            throw new UnauthorizedAccessException(
                "Access denied to resume.");

        var job = await _db.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == request.JobId, ct);

        if (job == null)
            throw new KeyNotFoundException("Job not found.");

        // Build job data for the Python AI pipeline.
        var requiredSkills =
    Deserialize<List<string>>(job.RequiredSkillsJson)
    ?? new List<string>();

        var preferredSkills =
            Deserialize<List<string>>(job.PreferredSkillsJson)
            ?? new List<string>();

        var jobData = new
        {
            id = job.Id,
            title = job.Title,
            description = job.Description,
            company = job.Company,
            location = job.Location,
            employment_type = job.EmploymentType,

            skills = requiredSkills
                .Concat(preferredSkills)
                .Distinct()
                .ToList(),

            requirements = requiredSkills
                .Select(skill => new
                {
                    text = skill,
                    priority = "required"
                })
                .ToList()
        };

        // Run the existing Python AI pipeline.
        var pythonOutput = await _pythonAiService.AnalyzeResumeAsync(
            resume.StoragePath,
            job.Description,
            jobData,
            ct);

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(pythonOutput);
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Python AI returned invalid JSON for resume {ResumeId} and job {JobId}",
                resume.Id,
                job.Id);

            throw new InvalidOperationException(
                "Python AI returned invalid JSON.",
                ex);
        }

        using (document)
        {
            var root = document.RootElement;

            var matchScore =
                root.TryGetProperty("match_score", out var scoreElement)
                    ? scoreElement.GetInt32()
                    : 0;

            List<string>? matchingSkills = null;
            List<string>? missingSkills = null;
            List<string>? matchingKeywords = null;
            List<string>? missingKeywords = null;
            List<string>? reasons = null;

            if (root.TryGetProperty("comparison", out var comparison)
                && comparison.ValueKind == JsonValueKind.Object)
            {
                if (comparison.TryGetProperty(
                        "matched_required",
                        out var matchedRequired))
                {
                    matchingSkills =
                        JsonSerializer.Deserialize<List<string>>(
                            matchedRequired.GetRawText());
                }

                if (comparison.TryGetProperty(
                        "missing_required",
                        out var missingRequired))
                {
                    missingSkills =
                        JsonSerializer.Deserialize<List<string>>(
                            missingRequired.GetRawText());
                }

                if (comparison.TryGetProperty(
                        "matched_job_skills",
                        out var matchedJobSkills))
                {
                    matchingKeywords =
                        JsonSerializer.Deserialize<List<string>>(
                            matchedJobSkills.GetRawText());
                }

                if (comparison.TryGetProperty(
                        "missing_job_skills",
                        out var missingJobSkills))
                {
                    missingKeywords =
                        JsonSerializer.Deserialize<List<string>>(
                            missingJobSkills.GetRawText());
                }
            }

            if (root.TryGetProperty(
                    "gemini_analysis",
                    out var geminiAnalysis)
                && geminiAnalysis.ValueKind == JsonValueKind.String)
            {
                reasons = new List<string>
            {
                geminiAnalysis.GetString() ?? string.Empty
            };
            }

            var entity = new MatchResult
            {
                Id = Guid.NewGuid(),
                CandidateId = candidateId,
                ResumeId = resume.Id,
                JobId = job.Id,
                MatchScore = Math.Clamp(matchScore, 0, 100),
                MatchingSkillsJson =
                    Serialize(matchingSkills),
                MissingSkillsJson =
                    Serialize(missingSkills),
                MatchingKeywordsJson =
                    Serialize(matchingKeywords),
                MissingKeywordsJson =
                    Serialize(missingKeywords),
                ReasonsJson =
                    Serialize(reasons),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.MatchResults.Add(entity);
            await _db.SaveChangesAsync(ct);

            entity.Job = job;

            _logger.LogInformation(
                "AI match created {MatchId} candidate {CandidateId} resume {ResumeId} job {JobId} score {Score}",
                entity.Id,
                candidateId,
                resume.Id,
                job.Id,
                entity.MatchScore);

            return ToDto(entity);
        }
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
