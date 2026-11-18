using System.ComponentModel.DataAnnotations;

namespace DigitalFarmManagement.Models;

public class MRLStandard : IAuditable
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string Country { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string ProductType { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string ActiveIngredient { get; set; } = string.Empty;

    /// <summary>
    /// Empty string means the limit applies to every species.
    /// A species name overrides the general limit for that animal.
    /// </summary>
    [MaxLength(30)]
    public string Species { get; set; } = string.Empty;

    public decimal LimitValue { get; set; }

    [Required, MaxLength(20)]
    public string Unit { get; set; } = "µg/kg";

    [Required, MaxLength(120)]
    public string RegulatorySource { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<MRLTest> Tests { get; set; } = new List<MRLTest>();
}
