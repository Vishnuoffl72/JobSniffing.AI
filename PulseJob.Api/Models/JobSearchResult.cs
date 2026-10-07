using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PulseJob.Api.Models;

/// <summary>
/// Single Table Design storing both scraped and AI-processed job outcomes.
/// </summary>
[Table("JobSearchResults")]
public class JobSearchResult
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Acts as the batch identity / timestamp for when this search execution occurred.
    /// </summary>
    public DateTime ScrapeTimestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Company { get; set; } = string.Empty;

    /// <summary>
    /// Platform: "LinkedIn", "Naukri", or "Instahyre"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Platform { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string DirectLink { get; set; } = string.Empty;

    /// <summary>
    /// AI match score from 0 to 100 based on resume relevance.
    /// </summary>
    [Range(0, 100)]
    public int MatchScore { get; set; }

    [Required]
    [MaxLength(1000)]
    public string MatchReason { get; set; } = string.Empty;

    public DateTime PostedDate { get; set; } = DateTime.UtcNow;

    public bool IsApplied { get; set; } = false;
}

