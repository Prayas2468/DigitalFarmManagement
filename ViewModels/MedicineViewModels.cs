using System.ComponentModel.DataAnnotations;

namespace DigitalFarmManagement.ViewModels;

public class MedicineFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required, MaxLength(120), Display(Name = "Medicine name")]
    public string MedicineName { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Active ingredient")]
    public string ActiveIngredient { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Manufacturer { get; set; } = string.Empty;

    [Range(0, 365), Display(Name = "Withdrawal period (days)")]
    public int WithdrawalPeriodDays { get; set; }

    [Required, MaxLength(40), Display(Name = "Batch number")]
    public string BatchNumber { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "1000000"), Display(Name = "Available quantity")]
    public decimal AvailableQuantity { get; set; }

    [Required]
    public string Unit { get; set; } = "ml";

    [Required, DataType(DataType.Date), Display(Name = "Purchase date")]
    public DateTime PurchaseDate { get; set; } = DateTime.Today;

    [Required, DataType(DataType.Date), Display(Name = "Expiry date")]
    public DateTime ExpiryDate { get; set; } = DateTime.Today.AddYears(1);

    [Range(typeof(decimal), "0", "1000000"), Display(Name = "Low-stock threshold")]
    public decimal LowStockThreshold { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Models.MedicineCategories.All.Contains(Category))
            yield return new ValidationResult("Choose a medicine category.", [nameof(Category)]);
        if (!Models.DoseUnits.All.Contains(Unit))
            yield return new ValidationResult("Choose a unit.", [nameof(Unit)]);
        if (ExpiryDate.Date < PurchaseDate.Date)
            yield return new ValidationResult("Expiry date cannot be before the purchase date.", [nameof(ExpiryDate)]);
    }
}

public class MedicineListItem
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string ActiveIngredient { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public decimal AvailableQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal LowStockThreshold { get; set; }
    public int WithdrawalPeriodDays { get; set; }
    public bool IsLow => AvailableQuantity <= LowStockThreshold;
    public bool IsExpired => ExpiryDate.Date < DateTime.Today;
}

public class MedicineIndexViewModel
{
    public List<MedicineListItem> Items { get; set; } = [];
    public PagerViewModel Pager { get; set; } = new();
    public string? Search { get; set; }
    public string? Category { get; set; }
    public int LowStock { get; set; }
    public int Expired { get; set; }
}
