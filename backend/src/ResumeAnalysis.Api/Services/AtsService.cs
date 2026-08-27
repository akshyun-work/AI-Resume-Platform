using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResumeAnalysis.Api.Data;
using ResumeAnalysis.Api.DTOs.Ats;
using ResumeAnalysis.Api.Entities;
using ResumeAnalysis.Api.Services.Interfaces;
using ResumeAnalysis.Api.Services.AI;


namespace ResumeAnalysis.Api.Services;

public class AtsService : IAtsService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AtsService> _logger;
    private readonly IPythonAiService _pythonAiService;
    

    public AtsService(ApplicationDbContext db, ILogger<AtsService> logger, IPythonAiService pythonAiService)
    {
        _db = db;
        _logger = logger;
        _pythonAiService = pythonAiService;
        
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

    public async Task<AtsAnalysisDto> CreateAsync(
    Guid candidateId,
    CreateAtsRequest request,
    CancellationToken ct)
    {
        if (request.ResumeId == Guid.Empty)
            throw new ArgumentException("ResumeId is required.");

        var resume = await _db.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ResumeId, ct);

        if (resume == null)
            throw new KeyNotFoundException("Resume not found.");

        if (resume.CandidateId != candidateId)
            throw new UnauthorizedAccessException("Access denied to resume.");

        if (string.IsNullOrWhiteSpace(resume.StoragePath))
            throw new InvalidOperationException(
                "Resume storage path is missing.");

        var pythonOutput = await _pythonAiService.AnalyzeResumeAsync(
            resume.StoragePath,
            string.Empty,
            null,
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
                "Python AI returned invalid JSON for resume {ResumeId}",
                resume.Id);

            throw new InvalidOperationException(
                "Python AI returned invalid JSON.",
                ex);
        }

        using (document)
        {
            var root = document.RootElement;

            if (!root.TryGetProperty("ats_result", out var atsResult))
                throw new InvalidOperationException(
                    "Python AI response does not contain ats_result.");

            var overallScore = atsResult.TryGetProperty("score", out var scoreElement)
                ? scoreElement.GetInt32()
                : 0;

            Dictionary<string, int>? categoryScores = null;

            if (atsResult.TryGetProperty("breakdown", out var breakdownElement)
                && breakdownElement.ValueKind == JsonValueKind.Object)
            {
                categoryScores =
                    JsonSerializer.Deserialize<Dictionary<string, int>>(
                        breakdownElement.GetRawText());
            }

            List<string>? skillsIdentified = null;

            if (root.TryGetProperty("resume", out var resumeElement)
                && resumeElement.TryGetProperty("skills", out var skillsElement))
            {
                skillsIdentified =
                    JsonSerializer.Deserialize<List<string>>(
                        skillsElement.GetRawText());
            }

            var entity = new AtsAnalysis
            {
                Id = Guid.NewGuid(),
                ResumeId = resume.Id,
                CandidateId = candidateId,
                OverallScore = overallScore,
                CategoryScoresJson = Serialize<Dictionary<string, int>>(categoryScores),
                SkillsIdentifiedJson = Serialize<List<string>>(skillsIdentified),
                KeywordsIdentifiedJson = Serialize<List<string>>(null),
                MissingKeywordsJson = Serialize<List<string>>(null),
                MissingSkillsJson = Serialize<List<string>>(null),
                IssuesJson = Serialize<List<string>>(null),
                RecommendationsJson = Serialize<List<string>>(null),
                AnalyzedAt = DateTime.UtcNow
            };

            _db.AtsAnalyses.Add(entity);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "AI ATS analysis created {AnalysisId} resume {ResumeId} candidate {CandidateId} score {Score}",
                entity.Id,
                resume.Id,
                candidateId,
                overallScore);

            return ToDto(entity);
        }
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
