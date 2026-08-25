using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Ats;
using ResumeAnalysis.Api.Entities;
using ResumeAnalysis.Api.Services.Interfaces;

namespace ResumeAnalysis.Api.Services;

public class AtsService : IAtsService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AtsService> _logger;

    public AtsService(ApplicationDbContext db, ILogger<AtsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static string? Serialize<T>(T? obj) => obj == null ? null : JsonSerializer.Serialize(obj);
    private static T? Deserialize<T>(string? json) => string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json);

    private static AtsAnalysisDto ToDto(AtsAnalysis e) => new()
    {
        Id = e.Id,
        ResumeId = e.ResumeId,
        CandidateId = e.CandidateId,
        OverallScore = e.OverallScore,
        CategoryScores = Deserialize<Dictionary<string, int>>(e.CategoryScoresJson),
        SkillsIdentified = Deserialize<List<string>>(e.SkillsIdentifiedJson),
        KeywordsIdentified = Deserialize<List<string>>(e.KeywordsIdentifiedJson),
        MissingKeywords = Deserialize<List<string>>(e.MissingKeywordsJson),
        MissingSkills = Deserialize<List<string>>(e.MissingSkillsJson),
        Issues = Deserialize<List<string>>(e.IssuesJson),
        Recommendations = Deserialize<List<string>>(e.RecommendationsJson),
        AnalyzedAt = e.AnalyzedAt
    };

    public async Task<AtsAnalysisDto> CreateAsync(Guid candidateId, CreateAtsRequest request, CancellationToken ct)
    {
        if (request.ResumeId == Guid.Empty) throw new ArgumentException("ResumeId is required.");
        if (request.OverallScore < 0 || request.OverallScore > 100)
            throw new ArgumentException("OverallScore must be between 0 and 100.");

        if (request.CategoryScores != null)
        {
            foreach (var kv in request.CategoryScores)
            {
                if (kv.Value < 0 || kv.Value > 100)
                    throw new ArgumentException($"Category score for '{kv.Key}' must be between 0 and 100.");
                if (string.IsNullOrWhiteSpace(kv.Key) || kv.Key.Length > 100)
                    throw new ArgumentException("Category name invalid.");
            }
        }

        var resume = await _db.Resumes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.ResumeId, ct);
        if (resume == null) throw new KeyNotFoundException("Resume not found.");
        if (resume.CandidateId != candidateId) throw new UnauthorizedAccessException("Access denied to resume.");

        var entity = new AtsAnalysis
        {
            Id = Guid.NewGuid(),
            ResumeId = request.ResumeId,
            CandidateId = candidateId,
            OverallScore = request.OverallScore,
            CategoryScoresJson = Serialize(request.CategoryScores),
            SkillsIdentifiedJson = Serialize(request.SkillsIdentified),
            KeywordsIdentifiedJson = Serialize(request.KeywordsIdentified),
            MissingKeywordsJson = Serialize(request.MissingKeywords),
            MissingSkillsJson = Serialize(request.MissingSkills),
            IssuesJson = Serialize(request.Issues),
            RecommendationsJson = Serialize(request.Recommendations),
            AnalyzedAt = DateTime.UtcNow
        };

        _db.AtsAnalyses.Add(entity);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("ATS analysis created {AnalysisId} resume {ResumeId} candidate {CandidateId} score {Score}", entity.Id, request.ResumeId, candidateId, request.OverallScore);

        return ToDto(entity);
    }

    public async Task<AtsAnalysisDto> GetByIdAsync(Guid candidateId, Guid analysisId, CancellationToken ct)
    {
        if (analysisId == Guid.Empty) throw new ArgumentException("Invalid id.");
        var e = await _db.AtsAnalyses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == analysisId, ct);
        if (e == null) throw new KeyNotFoundException("Analysis not found.");
        if (e.CandidateId != candidateId) throw new UnauthorizedAccessException("Access denied.");
        return ToDto(e);
    }

    public async Task<AtsAnalysisDto> GetLatestForResumeAsync(Guid candidateId, Guid resumeId, CancellationToken ct)
    {
        if (resumeId == Guid.Empty) throw new ArgumentException("Invalid resume id.");
        var resume = await _db.Resumes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == resumeId, ct);
        if (resume == null) throw new KeyNotFoundException("Resume not found.");
        if (resume.CandidateId != candidateId) throw new UnauthorizedAccessException("Access denied to resume.");

        var e = await _db.AtsAnalyses.AsNoTracking()
            .Where(x => x.ResumeId == resumeId && x.CandidateId == candidateId)
            .OrderByDescending(x => x.AnalyzedAt)
            .FirstOrDefaultAsync(ct);
        if (e == null) throw new KeyNotFoundException("No analysis found for resume.");
        return ToDto(e);
    }

    public async Task<IReadOnlyList<AtsAnalysisDto>> GetHistoryForResumeAsync(Guid candidateId, Guid resumeId, CancellationToken ct)
    {
        if (resumeId == Guid.Empty) throw new ArgumentException("Invalid resume id.");
        var resume = await _db.Resumes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == resumeId, ct);
        if (resume == null) throw new KeyNotFoundException("Resume not found.");
        if (resume.CandidateId != candidateId) throw new UnauthorizedAccessException("Access denied to resume.");

        var list = await _db.AtsAnalyses.AsNoTracking()
            .Where(x => x.ResumeId == resumeId && x.CandidateId == candidateId)
            .OrderByDescending(x => x.AnalyzedAt)
            .ToListAsync(ct);
        return list.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<AtsAnalysisDto>> ListForCandidateAsync(Guid candidateId, CancellationToken ct)
    {
        var list = await _db.AtsAnalyses.AsNoTracking()
            .Where(x => x.CandidateId == candidateId)
            .OrderByDescending(x => x.AnalyzedAt)
            .ToListAsync(ct);
        return list.Select(ToDto).ToList();
    }
}
