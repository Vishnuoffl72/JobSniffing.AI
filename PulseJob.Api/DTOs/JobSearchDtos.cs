using System.ComponentModel.DataAnnotations;

namespace PulseJob.Api.DTOs;

public class JobSearchRequest
{
    [Required]
    public string JobTitle { get; set; } = string.Empty;

    [Required]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Filtering threshold in hours, e.g. 12, 24, 48.
    /// </summary>
    public int PostedWithinHours { get; set; } = 24;

    [Required]
    public string ResumeText { get; set; } = string.Empty;
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

