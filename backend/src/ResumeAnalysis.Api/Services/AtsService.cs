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

    public AtsService(
        ApplicationDbContext db,
        ILogger<AtsService> logger,
        IPythonAiService pythonAiService)
    {
        _db = db;
        _logger = logger;
        _pythonAiService = pythonAiService;
    }

    private static string? Serialize<T>(T? obj) =>
        obj == null ? null : JsonSerializer.Serialize(obj);

    private static T? Deserialize<T>(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? default
            : JsonSerializer.Deserialize<T>(json);

    private static List<string>? SanitizeStringList(string? json)
    {
        var list = Deserialize<List<string>>(json);
        if (list == null || list.Count == 0) return list;

        var sanitized = new List<string>();
        foreach (var item in list)
        {
            if (string.IsNullOrWhiteSpace(item)) continue;
            var trimmed = item.Trim();
            if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
            {
                try
                {
                    using var doc = JsonDocument.Parse(trimmed);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("factors", out var factors) && factors.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var f in factors.EnumerateArray())
                        {
                            var s = f.GetString();
                            if (!string.IsNullOrWhiteSpace(s)) sanitized.Add(s);
                        }
                    }
                    if (root.TryGetProperty("resume_weaknesses", out var weaknesses) && weaknesses.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var w in weaknesses.EnumerateArray())
                        {
                            if (w.ValueKind == JsonValueKind.String)
                            {
                                var s = w.GetString();
                                if (!string.IsNullOrWhiteSpace(s)) sanitized.Add(s);
                            }
                            else if (w.ValueKind == JsonValueKind.Object && w.TryGetProperty("description", out var desc))
                            {
                                var s = desc.GetString();
                                if (!string.IsNullOrWhiteSpace(s)) sanitized.Add(s);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore malformed JSON string
                }
            }
            else
            {
                sanitized.Add(trimmed);
            }
        }
        return sanitized;
    }

    private static AtsAnalysisDto ToDto(AtsAnalysis e) => new()
    {
        Id = e.Id,
        ResumeId = e.ResumeId,
        CandidateId = e.CandidateId,
        OverallScore = e.OverallScore,

        CategoryScores =
            Deserialize<Dictionary<string, int>>(e.CategoryScoresJson),

        SkillsIdentified =
            Deserialize<List<string>>(e.SkillsIdentifiedJson),

        KeywordsIdentified =
            Deserialize<List<string>>(e.KeywordsIdentifiedJson),

        MissingKeywords =
            Deserialize<List<string>>(e.MissingKeywordsJson),

        MissingSkills =
            Deserialize<List<string>>(e.MissingSkillsJson),

        Issues =
            SanitizeStringList(e.IssuesJson),

        Recommendations =
            SanitizeStringList(e.RecommendationsJson),

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
            throw new UnauthorizedAccessException(
                "Access denied to resume.");

        if (string.IsNullOrWhiteSpace(resume.StoragePath))
            throw new InvalidOperationException(
                "Resume storage path is missing.");

        // ------------------------------------------------------------
        // Run Python AI analysis
        // ------------------------------------------------------------

        var pythonOutput = await _pythonAiService.AnalyzeResumeAsync(
        resume.StoragePath,
        request.JobDescription ?? string.Empty,
        request.JobData,
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

            // --------------------------------------------------------
            // Validate ATS result
            // --------------------------------------------------------

            if (!root.TryGetProperty(
                    "ats_result",
                    out var atsResult))
            {
                throw new InvalidOperationException(
                    "Python AI response does not contain ats_result.");
            }

            // --------------------------------------------------------
            // Overall score
            // --------------------------------------------------------

            var overallScore =
                atsResult.TryGetProperty(
                    "score",
                    out var scoreElement)
                    ? scoreElement.GetInt32()
                    : 0;

            // --------------------------------------------------------
            // Category / breakdown scores
            // --------------------------------------------------------

            Dictionary<string, int>? categoryScores = null;

            if (atsResult.TryGetProperty(
                    "breakdown",
                    out var breakdownElement)
                && breakdownElement.ValueKind == JsonValueKind.Object)
            {
                categoryScores =
                    JsonSerializer.Deserialize<
                        Dictionary<string, int>>(
                            breakdownElement.GetRawText());
            }

            // --------------------------------------------------------
            // Skills identified in the resume
            // --------------------------------------------------------

            List<string>? skillsIdentified = null;

            if (root.TryGetProperty(
                    "resume",
                    out var resumeElement)
                && resumeElement.TryGetProperty(
                    "skills",
                    out var skillsElement))
            {
                skillsIdentified =
                    JsonSerializer.Deserialize<List<string>>(
                        skillsElement.GetRawText());
            }

            // --------------------------------------------------------
            // --------------------------------------------------------
            // Additional ATS information
            //
            // Read from atsResult (standard) or root (fallback)
            // --------------------------------------------------------

            List<string>? keywordsIdentified = null;
            List<string>? missingKeywords = null;
            List<string>? missingSkills = null;
            List<string>? issues = null;
            List<string>? recommendations = null;

            // Helper to get string array from element
            static List<string>? GetStringList(JsonElement source, string propertyName)
            {
                if (source.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<string>>(element.GetRawText());
                }
                return null;
            }

            keywordsIdentified = GetStringList(atsResult, "keywords_identified") ?? GetStringList(root, "keywords_identified");
            missingKeywords = GetStringList(atsResult, "missing_keywords") ?? GetStringList(root, "missing_keywords");
            missingSkills = GetStringList(atsResult, "missing_skills") ?? GetStringList(root, "missing_skills");
            issues = GetStringList(atsResult, "issues") ?? GetStringList(root, "issues");
            recommendations = GetStringList(atsResult, "recommendations") ?? GetStringList(root, "recommendations");

            // --------------------------------------------------------
            // Fallback: support comparison structure
            //
            // This matches the structure already used by the
            // existing AI match service:
            //
            // comparison
            //   matched_required
            //   missing_required
            //   matched_job_skills
            //   missing_job_skills
            // --------------------------------------------------------

            if (root.TryGetProperty(
                    "comparison",
                    out var comparison)
                && comparison.ValueKind == JsonValueKind.Object)
            {
                // Missing required skills
                if (comparison.TryGetProperty(
                        "missing_required",
                        out var missingRequired)
                    && missingRequired.ValueKind == JsonValueKind.Array)
                {
                    missingSkills =
                        JsonSerializer.Deserialize<List<string>>(
                            missingRequired.GetRawText());
                }

                // Matching required skills
                if (comparison.TryGetProperty(
                        "matched_required",
                        out var matchedRequired)
                    && matchedRequired.ValueKind == JsonValueKind.Array)
                {
                    var matchedSkills =
                        JsonSerializer.Deserialize<List<string>>(
                            matchedRequired.GetRawText());

                    if (matchedSkills != null)
                    {
                        skillsIdentified ??= new List<string>();

                        foreach (var skill in matchedSkills)
                        {
                            if (!skillsIdentified.Contains(skill))
                                skillsIdentified.Add(skill);
                        }
                    }
                }

                // Missing job skills / keywords
                if (comparison.TryGetProperty(
                        "missing_job_skills",
                        out var missingJobSkills)
                    && missingJobSkills.ValueKind == JsonValueKind.Array)
                {
                    missingKeywords =
                        JsonSerializer.Deserialize<List<string>>(
                            missingJobSkills.GetRawText());
                }

                // Matching job skills / keywords
                if (comparison.TryGetProperty(
                        "matched_job_skills",
                        out var matchedJobSkills)
                    && matchedJobSkills.ValueKind == JsonValueKind.Array)
                {
                    keywordsIdentified =
                        JsonSerializer.Deserialize<List<string>>(
                            matchedJobSkills.GetRawText());
                }
            }

            // --------------------------------------------------------
            // Generate generic issues/recommendations when the AI
            // response does not explicitly provide them.
            // --------------------------------------------------------

            if (missingSkills != null && missingSkills.Count > 0)
            {
                issues ??= new List<string>();

                issues.Add(
                    $"Missing required skills: {string.Join(", ", missingSkills)}");
            }

            if (missingKeywords != null && missingKeywords.Count > 0)
            {
                issues ??= new List<string>();

                issues.Add(
                    $"Missing job keywords: {string.Join(", ", missingKeywords)}");
            }

            if (missingSkills != null && missingSkills.Count > 0)
            {
                recommendations ??= new List<string>();

                recommendations.Add(
                    "Consider adding relevant missing skills to the resume where they accurately reflect your experience.");
            }

            if (missingKeywords != null && missingKeywords.Count > 0)
            {
                recommendations ??= new List<string>();

                recommendations.Add(
                    "Consider incorporating relevant job-specific keywords into the resume where appropriate.");
            }

            if (overallScore < 70)
            {
                recommendations ??= new List<string>();

                recommendations.Add(
                    "Improve the resume's alignment with the target role and strengthen areas identified by the ATS analysis.");
            }

            // --------------------------------------------------------
            // Create ATS analysis entity
            // --------------------------------------------------------

            var entity = new AtsAnalysis
            {
                Id = Guid.NewGuid(),
                ResumeId = resume.Id,
                CandidateId = candidateId,

                OverallScore = Math.Clamp(
                    overallScore,
                    0,
                    100),

                CategoryScoresJson =
                    Serialize(categoryScores),

                SkillsIdentifiedJson =
                    Serialize(skillsIdentified),

                KeywordsIdentifiedJson =
                    Serialize(keywordsIdentified),

                MissingKeywordsJson =
                    Serialize(missingKeywords),

                MissingSkillsJson =
                    Serialize(missingSkills),

                IssuesJson =
                    Serialize(issues),

                RecommendationsJson =
                    Serialize(recommendations),

                AnalyzedAt = DateTime.UtcNow
            };

            _db.AtsAnalyses.Add(entity);

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "AI ATS analysis created {AnalysisId} resume {ResumeId} candidate {CandidateId} score {Score}",
                entity.Id,
                resume.Id,
                candidateId,
                entity.OverallScore);

            return ToDto(entity);
        }
    }

    public async Task<AtsAnalysisDto> GetByIdAsync(
        Guid candidateId,
        Guid analysisId,
        CancellationToken ct)
    {
        if (analysisId == Guid.Empty)
            throw new ArgumentException("Invalid id.");

        var e = await _db.AtsAnalyses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == analysisId,
                ct);

        if (e == null)
            throw new KeyNotFoundException(
                "Analysis not found.");

        if (e.CandidateId != candidateId)
            throw new UnauthorizedAccessException(
                "Access denied.");

        return ToDto(e);
    }

    public async Task<AtsAnalysisDto> GetLatestForResumeAsync(
        Guid candidateId,
        Guid resumeId,
        CancellationToken ct)
    {
        if (resumeId == Guid.Empty)
            throw new ArgumentException(
                "Invalid resume id.");

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

        var e = await _db.AtsAnalyses
            .AsNoTracking()
            .Where(x =>
                x.ResumeId == resumeId &&
                x.CandidateId == candidateId)
            .OrderByDescending(x => x.AnalyzedAt)
            .FirstOrDefaultAsync(ct);

        if (e == null)
            throw new KeyNotFoundException(
                "No analysis found for resume.");

        return ToDto(e);
    }

    public async Task<IReadOnlyList<AtsAnalysisDto>>
        GetHistoryForResumeAsync(
            Guid candidateId,
            Guid resumeId,
            CancellationToken ct)
    {
        if (resumeId == Guid.Empty)
            throw new ArgumentException(
                "Invalid resume id.");

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

        var list = await _db.AtsAnalyses
            .AsNoTracking()
            .Where(x =>
                x.ResumeId == resumeId &&
                x.CandidateId == candidateId)
            .OrderByDescending(x => x.AnalyzedAt)
            .ToListAsync(ct);

        return list.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<AtsAnalysisDto>>
        ListForCandidateAsync(
            Guid candidateId,
            CancellationToken ct)
    {
        var list = await _db.AtsAnalyses
            .AsNoTracking()
            .Where(x => x.CandidateId == candidateId)
            .OrderByDescending(x => x.AnalyzedAt)
            .ToListAsync(ct);

        return list.Select(ToDto).ToList();
    }
}