using System.Text.Json;
using ResumeAnalysis.Api.Services.Interfaces;

namespace ResumeAnalysis.Api.Services.JobProviders;

public class FreeHireJobProvider : IJobProvider
{
    private const string BaseUrl = "https://freehire.me/api/v1";

    private readonly HttpClient _httpClient;
    private readonly ILogger<FreeHireJobProvider> _logger;

    public FreeHireJobProvider(
        HttpClient httpClient,
        ILogger<FreeHireJobProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExternalJob>> SearchJobsAsync(
        string? keywords,
        string? location,
        int limit,
        CancellationToken ct)
    {

        limit = Math.Clamp(limit, 1, 100);

        var queryParts = new List<string>
        {
        "countries=IN",
        $"limit={limit}",
        "offset=0",
        "include_description=true",
        "description_format=markdown"
        };

        if (!string.IsNullOrWhiteSpace(keywords))
        {
            queryParts.Insert(
                0,
                $"q={Uri.EscapeDataString(keywords.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(location) &&
    !location.Equals("India", StringComparison.OrdinalIgnoreCase))
        {
            var locationSearch = Uri.EscapeDataString(location.Trim());

            if (string.IsNullOrWhiteSpace(keywords))
            {
                queryParts.Insert(0, $"q={locationSearch}");
            }
            else
            {
                var combinedSearch = Uri.EscapeDataString(
                    $"{keywords.Trim()} {location.Trim()}");

                queryParts.Insert(0, $"q={combinedSearch}");
            }
        }
        else if (!string.IsNullOrWhiteSpace(keywords))
        {
            queryParts.Insert(
                0,
                $"q={Uri.EscapeDataString(keywords.Trim())}");
        }

        var url =
            $"{BaseUrl}/agent/jobs/search?{string.Join("&", queryParts)}";

        _logger.LogInformation(
            "Searching FreeHire jobs. Keywords={Keywords}, Location={Location}, Limit={Limit}",
            keywords,
            location,
            limit);

        using var response = await _httpClient.GetAsync(url, ct);

        var responseText = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "FreeHire job search failed. StatusCode={StatusCode}, Response={Response}",
                (int)response.StatusCode,
                responseText);

            throw new HttpRequestException(
                $"FreeHire job search failed with status code {(int)response.StatusCode}.");
        }

        if (string.IsNullOrWhiteSpace(responseText))
        {
            _logger.LogWarning(
                "FreeHire returned an empty response.");

            return Array.Empty<ExternalJob>();
        }

        FreeHireResponse? data;

        try
        {
            data = JsonSerializer.Deserialize<FreeHireResponse>(
                responseText,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Failed to deserialize FreeHire response.");

            throw new InvalidOperationException(
                "FreeHire returned an invalid JSON response.",
                ex);
        }

        if (data?.Data == null || data.Data.Count == 0)
        {
            _logger.LogInformation(
                "FreeHire returned no jobs for Keywords={Keywords}, Location={Location}",
                keywords,
                location);

            return Array.Empty<ExternalJob>();
        }

        var jobs = data.Data
            .Where(job =>
                !string.IsNullOrWhiteSpace(job.Title) &&
                !string.IsNullOrWhiteSpace(job.Company))
            .Select(MapJob)
            .ToList();

        _logger.LogInformation(
            "FreeHire returned {JobCount} usable jobs.",
            jobs.Count);

        return jobs;
    }

    private static ExternalJob MapJob(FreeHireJob job)
    {
        var requirements = new List<string>();

        if (job.Enrichment?.Requirements != null)
        {
            foreach (var requirement in job.Enrichment.Requirements)
            {
                if (!string.IsNullOrWhiteSpace(requirement.Text))
                {
                    requirements.Add(requirement.Text.Trim());
                }
            }
        }

        var skills = job.Skills?
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Select(skill => skill.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            ?? new List<string>();

        return new ExternalJob
        {
            ExternalId = job.PublicSlug,
            Title = job.Title?.Trim(),
            Company = job.Company?.Trim(),
            Location = job.Location?.Trim(),
            Description = job.Description?.Trim(),
            Url = job.Url,
            EmploymentType = job.EmploymentType,
            SalaryMin = job.SalaryMin,
            SalaryMax = job.SalaryMax,
            SalaryCurrency = job.SalaryCurrency,
            Skills = skills,
            Requirements = requirements,
            Source = job.Source
        };
    }

    private sealed class FreeHireResponse
    {
        public List<FreeHireJob> Data { get; set; } = new();
    }

    private sealed class FreeHireJob
    {
        public string? PublicSlug { get; set; }
        public string? Source { get; set; }
        public string? Url { get; set; }
        public string? Title { get; set; }
        public string? Company { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public string? EmploymentType { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string? SalaryCurrency { get; set; }
        public List<string>? Skills { get; set; }
        public FreeHireEnrichment? Enrichment { get; set; }
    }

    private sealed class FreeHireEnrichment
    {
        public List<FreeHireRequirement>? Requirements { get; set; }
    }

    private sealed class FreeHireRequirement
    {
        public string? Text { get; set; }
        public string? Priority { get; set; }
    }
}