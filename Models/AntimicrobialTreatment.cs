using System.ComponentModel.DataAnnotations;

namespace DigitalFarmManagement.Models;

public class AntimicrobialTreatment : IAuditable
{
    public int Id { get; set; }

    public int LivestockId { get; set; }
    public Livestock Livestock { get; set; } = null!;

    public int MedicineId { get; set; }
    public Medicine Medicine { get; set; } = null!;

    public int MedicineInventoryId { get; set; }
    public MedicineInventory MedicineInventory { get; set; } = null!;

    [Required, MaxLength(120)]
    public string MedicineName { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string MedicineCategory { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string ActiveIngredient { get; set; } = string.Empty;

    public decimal Dosage { get; set; }

    [Required, MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string AdministrationRoute { get; set; } = string.Empty;

    public DateTime TreatmentStartDate { get; set; }
    public DateTime TreatmentEndDate { get; set; }

    [Required, MaxLength(120)]
    public string VeterinarianName { get; set; } = string.Empty;

    [Required, MaxLength(250)]
    public string ReasonForTreatment { get; set; } = string.Empty;

    public int WithdrawalPeriodDays { get; set; }

    /// <summary>
    /// First calendar day the animal's products may be sold.
    /// Calculated as treatment end date plus the medicine withdrawal period.
    /// </summary>
    public DateTime WithdrawalEndDate { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public string? RecordedById { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public WithdrawalPeriod? Withdrawal { get; set; }
}
