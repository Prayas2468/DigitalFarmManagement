using System.ComponentModel.DataAnnotations;

namespace DigitalFarmManagement.ViewModels;

public class MrlTestFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required, Display(Name = "Animal")]
    public int LivestockId { get; set; }

    [Required, Display(Name = "Product type")]
    public string ProductType { get; set; } = string.Empty;

    [Required, Display(Name = "Active ingredient")]
    public string ActiveIngredient { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "1000000"), Display(Name = "Measured residue")]
    public decimal MeasuredResidueLevel { get; set; }

    [Required]
    public string Unit { get; set; } = "µg/kg";

    [Required, DataType(DataType.Date), Display(Name = "Test date")]
    public DateTime TestDate { get; set; } = DateTime.Today;

    [Required, MaxLength(120), Display(Name = "Laboratory")]
    public string LaboratoryName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Remarks { get; set; }

    [Display(Name = "Hold as pending")]
    public bool HoldAsPending { get; set; }

    public List<AnimalOption> Animals { get; set; } = [];
    public List<string> Ingredients { get; set; } = [];
    public List<MrlPreviewStandard> Standards { get; set; } = [];
    public string Country { get; set; } = "India";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Models.ProductTypes.All.Contains(ProductType))
            yield return new ValidationResult("Choose milk, meat, or eggs.", [nameof(ProductType)]);
        if (!Models.MrlUnits.All.Contains(Unit))
            yield return new ValidationResult("Use mg/kg or µg/kg.", [nameof(Unit)]);
        if (TestDate.Date > DateTime.Today)
            yield return new ValidationResult("Test date cannot be in the future.", [nameof(TestDate)]);
    }
}

public class MrlPreviewStandard
{
    public string ProductType { get; set; } = string.Empty;
    public string ActiveIngredient { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public decimal LimitValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string RegulatorySource { get; set; } = string.Empty;
}

public class MrlIndexViewModel
{
    public List<Models.MRLTest> Items { get; set; } = [];
    public PagerViewModel Pager { get; set; } = new();
    public string? ProductType { get; set; }
    public string? Ingredient { get; set; }
    public string? Result { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Compliant { get; set; }
    public int NonCompliant { get; set; }
    public int Pending { get; set; }
    public List<string> Ingredients { get; set; } = [];
}

public class MrlStandardFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string Country { get; set; } = "India";

    [Required, Display(Name = "Product type")]
    public string ProductType { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Active ingredient")]
    public string ActiveIngredient { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Species { get; set; }

    [Range(typeof(decimal), "0", "1000000"), Display(Name = "MRL limit")]
    public decimal LimitValue { get; set; }

    [Required]
    public string Unit { get; set; } = "µg/kg";

    [Required, MaxLength(120), Display(Name = "Regulatory source")]
    public string RegulatorySource { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Models.ProductTypes.All.Contains(ProductType))
            yield return new ValidationResult("Choose milk, meat, or eggs.", [nameof(ProductType)]);
        if (!Models.MrlUnits.All.Contains(Unit))
            yield return new ValidationResult("Use mg/kg or µg/kg.", [nameof(Unit)]);
        if (!string.IsNullOrWhiteSpace(Species) && !Models.SpeciesList.All.Contains(Species))
            yield return new ValidationResult("Leave species blank for every species, or choose a supported species.", [nameof(Species)]);
    }
}
