using System.ComponentModel.DataAnnotations;

namespace DigitalFarmManagement.Models;

/// <summary>
/// A recorded decision that a product may be sold.
/// The portal refuses to create this row while a withdrawal period is still open.
/// </summary>
public class ProductClearance : IAuditable
{
    public int Id { get; set; }

    public int LivestockId { get; set; }
    public Livestock Livestock { get; set; } = null!;

    [Required, MaxLength(20)]
    public string ProductType { get; set; } = string.Empty;

    public DateTime ClearedOn { get; set; }
    public string? ClearedById { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
