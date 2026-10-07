using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseJob.Api.Data;
using PulseJob.Api.DTOs;
using PulseJob.Api.Models;
using PulseJob.Api.Services;

namespace PulseJob.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IApifyService _apifyService;
    private readonly IAiMatchingService _aiMatchingService;
    private readonly PulseJobDbContext _dbContext;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        IApifyService apifyService,
        IAiMatchingService aiMatchingService,
        PulseJobDbContext dbContext,
        ILogger<JobsController> logger)
    {
        _apifyService = apifyService;
        _aiMatchingService = aiMatchingService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Executes full pipeline: Scrapes raw jobs via Apify -> AI filtering/scoring via Gemini -> Persists batch to SQLite single table -> Returns results.
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<List<JobSearchResult>>> SearchJobs([FromBody] JobSearchRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.JobTitle))
        {
            return BadRequest("Job title is required.");
        }

        var scrapeTimestamp = DateTime.UtcNow;
        _logger.LogInformation("Initiating job search for '{JobTitle}' in '{Location}' (within {Hours}h)", 
            request.JobTitle, request.Location, request.PostedWithinHours);

        // 1. Fetch raw scraped results across LinkedIn, Naukri, Instahyre using extracted keywords
        var rawJobs = await _apifyService.FetchJobsAsync(request.JobTitle, request.Location, request.PostedWithinHours, request.ResumeText, ct);

        // 2. Process via Gemini AI matching service
        var scoredJobs = await _aiMatchingService.FilterAndScoreJobsAsync(
            rawJobs,
            request.ResumeText,
            request.PostedWithinHours,
            scrapeTimestamp,
            ct);

        // 3. Persist batch into single SQLite table `JobSearchResults`
        if (scoredJobs.Count > 0)
        {
            await _dbContext.JobSearchResults.AddRangeAsync(scoredJobs, ct);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Persisted {Count} scored job records with batch timestamp {Timestamp}", 
                scoredJobs.Count, scrapeTimestamp);
        }

        return Ok(scoredJobs);
    }

    /// <summary>
    /// Returns stored jobs filtered by a specific calendar date (UTC).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<JobSearchResult>>> GetJobsByDate([FromQuery] string? date = null, [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        var query = _dbContext.JobSearchResults.AsQueryable();

        if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsedDate))
        {
            var startOfDay = parsedDate.Date;
            var endOfDay = startOfDay.AddDays(1);
            query = query.Where(j => j.ScrapeTimestamp >= startOfDay && j.ScrapeTimestamp < endOfDay);
        }

        var items = await query
            .OrderByDescending(j => j.ScrapeTimestamp)
            .ThenByDescending(j => j.MatchScore)
            .Take(limit)
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>
    /// Returns distinct dates (YYYY-MM-DD) on which scrapes have occurred.
    /// </summary>
    [HttpGet("dates")]
    public async Task<ActionResult<List<string>>> GetAvailableScrapeDates(CancellationToken ct = default)
    {
        var dates = await _dbContext.JobSearchResults
            .Select(j => j.ScrapeTimestamp)
            .ToListAsync(ct);

        var distinctDates = dates
            .Select(d => d.ToString("yyyy-MM-dd"))
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        return Ok(distinctDates);
    }

    /// <summary>
    /// Updates the application status for a specific job entry.
    /// </summary>
    [HttpPatch("{id:int}/applied")]
    public async Task<IActionResult> ToggleApplied(int id, [FromBody] UpdateAppliedStatusRequest req, CancellationToken ct)
    {
        var job = await _dbContext.JobSearchResults.FindAsync([id], ct);
        if (job == null)
        {
            return NotFound($"Job record with ID {id} not found.");
        }

        job.IsApplied = req.IsApplied;
        await _dbContext.SaveChangesAsync(ct);

        return Ok(job);
    }
}

