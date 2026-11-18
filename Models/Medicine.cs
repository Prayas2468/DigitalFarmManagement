using System.ComponentModel.DataAnnotations;

namespace DigitalFarmManagement.Models;

public class Medicine : IAuditable
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string MedicineName { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string ActiveIngredient { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Category { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Number of days after the last dose before a product may be sold.
    /// Stored on the medicine so each country or product policy can be configured.
    /// </summary>
    public int WithdrawalPeriodDays { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<MedicineInventory> Batches { get; set; } = new List<MedicineInventory>();
    public ICollection<AntimicrobialTreatment> Treatments { get; set; } = new List<AntimicrobialTreatment>();
}
