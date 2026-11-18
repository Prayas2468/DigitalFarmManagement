using System.ComponentModel.DataAnnotations;

namespace DigitalFarmManagement.Models;

public class MRLTest : IAuditable
{
    public int Id { get; set; }

    public int LivestockId { get; set; }
    public Livestock Livestock { get; set; } = null!;

    [Required, MaxLength(20)]
    public string ProductType { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string ActiveIngredient { get; set; } = string.Empty;

    public int? MRLStandardId { get; set; }
    public MRLStandard? Standard { get; set; }

    public decimal MeasuredResidueLevel { get; set; }

    [Required, MaxLength(20)]
    public string Unit { get; set; } = "µg/kg";

    public decimal ApplicableMrlLimit { get; set; }

    [MaxLength(20)]
    public string LimitUnit { get; set; } = "µg/kg";

    public DateTime TestDate { get; set; }

    [Required, MaxLength(120)]
    public string LaboratoryName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string TestResult { get; set; } = MrlResults.Pending;

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
