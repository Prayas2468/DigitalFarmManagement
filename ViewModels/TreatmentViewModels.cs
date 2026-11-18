using System.ComponentModel.DataAnnotations;

namespace DigitalFarmManagement.ViewModels;

public class TreatmentFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required, Display(Name = "Animal")]
    public int LivestockId { get; set; }

    [Required, Display(Name = "Medicine batch")]
    public int MedicineInventoryId { get; set; }

    [Range(typeof(decimal), "0.001", "100000")]
    public decimal Dosage { get; set; }

    [Required, Display(Name = "Administration route")]
    public string AdministrationRoute { get; set; } = string.Empty;

    [Required, DataType(DataType.Date), Display(Name = "Treatment start")]
    public DateTime TreatmentStartDate { get; set; } = DateTime.Today;

    [Required, DataType(DataType.Date), Display(Name = "Treatment end")]
    public DateTime TreatmentEndDate { get; set; } = DateTime.Today;

    [Required, MaxLength(120), Display(Name = "Veterinarian")]
    public string VeterinarianName { get; set; } = string.Empty;

    [Required, MaxLength(250), Display(Name = "Reason for treatment")]
    public string ReasonForTreatment { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public List<AnimalOption> Animals { get; set; } = [];
    public List<BatchOption> Batches { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TreatmentEndDate.Date < TreatmentStartDate.Date)
            yield return new ValidationResult("Treatment end date cannot be before the start date.", [nameof(TreatmentEndDate)]);
        if (!string.IsNullOrWhiteSpace(AdministrationRoute) && !Models.AdminRoutes.All.Contains(AdministrationRoute))
            yield return new ValidationResult("Choose a valid administration route.", [nameof(AdministrationRoute)]);
    }
}

public class AnimalOption
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class BatchOption
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string MedicineName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ActiveIngredient { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int WithdrawalDays { get; set; }
    public decimal Stock { get; set; }
    public bool Expired { get; set; }
}

public class UsageTotal
{
    public string Unit { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

public class MedicineFrequency
{
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalDosage { get; set; }
}

public class TreatmentIndexViewModel
{
    public List<Models.AntimicrobialTreatment> Items { get; set; } = [];
    public PagerViewModel Pager { get; set; } = new();
    public string? Search { get; set; }
    public int? AnimalId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int ActiveAnimals { get; set; }
    public List<UsageTotal> UsageTotals { get; set; } = [];
    public List<MedicineFrequency> Frequent { get; set; } = [];
    public List<AnimalOption> Animals { get; set; } = [];
}
