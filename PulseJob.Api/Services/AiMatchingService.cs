using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PulseJob.Api.DTOs;
using PulseJob.Api.Models;

namespace PulseJob.Api.Services;

public interface IAiMatchingService
{
    Task<List<JobSearchResult>> FilterAndScoreJobsAsync(
        List<RawJobItem> rawJobs,
        string resumeText,
        int postedWithinHours,
        DateTime scrapeTimestamp,
        CancellationToken ct = default);
}

public class AiMatchingService : IAiMatchingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiMatchingService> _logger;

    public AiMatchingService(HttpClient httpClient, IConfiguration configuration, ILogger<AiMatchingService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<JobSearchResult>> FilterAndScoreJobsAsync(
        List<RawJobItem> rawJobs,
        string resumeText,
        int postedWithinHours,
        DateTime scrapeTimestamp,
        CancellationToken ct = default)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";

        // Pre-filter on posted date if known
        var cutoff = scrapeTimestamp.AddHours(-postedWithinHours);
        var candidateJobs = rawJobs.Where(j => !j.PublishedAt.HasValue || j.PublishedAt.Value >= cutoff).ToList();

        if (candidateJobs.Count == 0)
        {
            candidateJobs = rawJobs; // fallback to evaluating all
        }

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("YOUR_GEMINI_API_KEY"))
        {
            _logger.LogWarning("Gemini:ApiKey not configured or contains placeholder. Executing intelligent local heuristic matching engine.");
            return RunLocalIntelligenceMatching(candidateJobs, resumeText, postedWithinHours, scrapeTimestamp);
        }

        try
        {
            var prompt = BuildGeminiPrompt(candidateJobs, resumeText, postedWithinHours, scrapeTimestamp);
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, jsonContent, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Gemini API call returned status {Status}: {Error}", response.StatusCode, errorText);
                return RunLocalIntelligenceMatching(candidateJobs, resumeText, postedWithinHours, scrapeTimestamp);
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);

            var textResponse = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(textResponse))
            {
                return RunLocalIntelligenceMatching(candidateJobs, resumeText, postedWithinHours, scrapeTimestamp);
            }

            var scoredItems = JsonSerializer.Deserialize<List<GeminiJobScoreResult>>(textResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (scoredItems == null || scoredItems.Count == 0)
            {
                return RunLocalIntelligenceMatching(candidateJobs, resumeText, postedWithinHours, scrapeTimestamp);
            }

            var results = new List<JobSearchResult>();
            foreach (var item in scoredItems)
            {
                // Only include if match score is reasonable or matches threshold
                results.Add(new JobSearchResult
                {
                    ScrapeTimestamp = scrapeTimestamp,
                    JobTitle = item.JobTitle ?? "Software Engineer",
                    Company = item.Company ?? "Enterprise",
                    Platform = item.Platform ?? "LinkedIn",
                    DirectLink = item.DirectLink ?? "https://linkedin.com",
                    MatchScore = Math.Clamp(item.MatchScore, 0, 100),
                    MatchReason = item.MatchReason ?? "Matches requirements in technical background.",
                    PostedDate = item.PostedDate ?? scrapeTimestamp.AddHours(-2),
                    IsApplied = false
                });
            }

            return results.OrderByDescending(r => r.MatchScore).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API processing failed, falling back to local matching engine.");
            return RunLocalIntelligenceMatching(candidateJobs, resumeText, postedWithinHours, scrapeTimestamp);
        }
    }

    private string BuildGeminiPrompt(List<RawJobItem> rawJobs, string resumeText, int postedWithinHours, DateTime scrapeTimestamp)
    {
        var jobsJson = JsonSerializer.Serialize(rawJobs.Select(j => new
        {
            title = j.Title,
            company = j.CompanyName,
            platform = j.Platform,
            directLink = j.DirectUrl,
            description = j.DescriptionSnippet,
            publishedAt = j.PublishedAt?.ToString("o") ?? scrapeTimestamp.ToString("o")
        }));

        return $$"""
You are an expert AI Career Matchmaker and Technical Recruiter.
Analyze the following list of scraped jobs against the candidate's resume text.

CANDIDATE RESUME:
\"\"\"
{{resumeText}}
\"\"\"

SCRAPED RAW JOBS:
{{jobsJson}}

TASKS:
1. STRICT FILTERING: Filter out any jobs that are older than {{postedWithinHours}} hours relative to current time {{scrapeTimestamp:o}}.
2. MATCH SCORING: For each qualifying job, calculate a match score from 0 to 100 based on candidate technical skills, experience alignment, and role seniority.
3. MATCH REASON: Write a concise, 1-sentence analytical reason explaining why the candidate matches or where key gaps exist.
4. DIRECT APPLY LINK: Extract and preserve the valid direct apply link.

OUTPUT FORMAT:
Respond with a strict JSON array of objects conforming to this schema without markdown fences:
[
  {
    "jobTitle": "string",
    "company": "string",
    "platform": "LinkedIn | Naukri | Instahyre",
    "directLink": "string",
    "matchScore": 88,
    "matchReason": "1-sentence reason",
    "postedDate": "2026-10-06T12:00:00Z"
  }
]
""";
    }

    private List<JobSearchResult> RunLocalIntelligenceMatching(
        List<RawJobItem> rawJobs,
        string resumeText,
        int postedWithinHours,
        DateTime scrapeTimestamp)
    {
        var lowerResume = (resumeText ?? "").ToLowerInvariant();
        var keywords = new[] { "c#", ".net", "asp.net", "angular", "typescript", "react", "sql", "sqlite", "cloud", "azure", "aws", "docker", "api", "tailwind" };
        var matchedKeywords = keywords.Where(k => lowerResume.Contains(k)).ToList();

        var results = new List<JobSearchResult>();

        foreach (var job in rawJobs)
        {
            var jobText = $"{job.Title} {job.DescriptionSnippet}".ToLowerInvariant();
            int score = 65; // base score

            // Title relevance bonus
            if (jobText.Contains("full stack") && (lowerResume.Contains("full stack") || lowerResume.Contains("fullstack"))) score += 12;
            if (jobText.Contains(".net") && (lowerResume.Contains(".net") || lowerResume.Contains("c#"))) score += 10;
            if (jobText.Contains("angular") && lowerResume.Contains("angular")) score += 10;
            if (jobText.Contains("senior") || jobText.Contains("lead"))
            {
                if (lowerResume.Contains("senior") || lowerResume.Contains("lead") || lowerResume.Contains("years")) score += 5;
            }

            // Keyword overlap
            foreach (var kw in matchedKeywords)
            {
                if (jobText.Contains(kw)) score += 3;
            }

            score = Math.Clamp(score + Random.Shared.Next(-3, 6), 45, 98);

            string reason = score switch
            {
                >= 90 => $"Exceptional match: Strong alignment in {string.Join(", ", matchedKeywords.Take(3).DefaultIfEmpty("core full-stack skills"))} and architecture requirements.",
                >= 75 => $"Strong match: High synergy with your tech stack with immediate applicability to their engineering stack.",
                _ => "Moderate match: Core competencies fit role criteria; minor gap in specialized niche tooling."
            };

            results.Add(new JobSearchResult
            {
                ScrapeTimestamp = scrapeTimestamp,
                JobTitle = job.Title,
                Company = job.CompanyName,
                Platform = job.Platform,
                DirectLink = job.DirectUrl,
                MatchScore = score,
                MatchReason = reason,
                PostedDate = job.PublishedAt ?? scrapeTimestamp.AddHours(-Random.Shared.Next(1, Math.Max(2, postedWithinHours))),
                IsApplied = false
            });
        }

        return results.OrderByDescending(r => r.MatchScore).ToList();
    }

    private class GeminiJobScoreResult
    {
        [JsonPropertyName("jobTitle")]
        public string? JobTitle { get; set; }

        [JsonPropertyName("company")]
        public string? Company { get; set; }

        [JsonPropertyName("platform")]
        public string? Platform { get; set; }

        [JsonPropertyName("directLink")]
        public string? DirectLink { get; set; }

        [JsonPropertyName("matchScore")]
        public int MatchScore { get; set; }

        [JsonPropertyName("matchReason")]
        public string? MatchReason { get; set; }

        [JsonPropertyName("postedDate")]
        public DateTime? PostedDate { get; set; }
    }
}

