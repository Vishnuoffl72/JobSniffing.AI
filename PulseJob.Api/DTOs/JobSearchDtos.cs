using System.ComponentModel.DataAnnotations;

namespace PulseJob.Api.DTOs;

public class JobSearchRequest
{
    public List<string> Keywords { get; set; } = new();

    public string JobTitle { get; set; } = string.Empty;

    [Required]
    public string Location { get; set; } = string.Empty;

    public string Experience { get; set; } = string.Empty;

    /// <summary>
    /// Filtering threshold in hours, e.g. 24, 72, 168.
    /// </summary>
    public int PostedWithinHours { get; set; } = 24;

    public string? Freshness { get; set; }

    public string? ResumeText { get; set; }
}

public class RawJobItem
{
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string DirectUrl { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string DescriptionSnippet { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
}

public class UpdateAppliedStatusRequest
{
    public bool IsApplied { get; set; }
}

