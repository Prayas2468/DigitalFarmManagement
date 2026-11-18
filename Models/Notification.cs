using System.ComponentModel.DataAnnotations;

namespace DigitalFarmManagement.Models;

public class Notification
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Category { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Severity { get; set; } = "Info";

    public bool IsRead { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [MaxLength(200)]
    public string? LinkUrl { get; set; }

    /// <summary>
    /// Stable key such as "mrl:4" so the alert sync can update a row instead of duplicating it.
    /// </summary>
    [MaxLength(60)]
    public string? SourceKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
