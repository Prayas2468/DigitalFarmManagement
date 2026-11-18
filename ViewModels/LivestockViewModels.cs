using System.ComponentModel.DataAnnotations;
using DigitalFarmManagement.Models;

namespace DigitalFarmManagement.ViewModels;

public class LivestockListItem
{
    public int Id { get; set; }
    public string AnimalCode { get; set; } = string.Empty;
    public string AnimalName { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string Breed { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public string FarmName { get; set; } = string.Empty;
    public bool UnderWithdrawal { get; set; }
}

public class LivestockIndexViewModel
{
    public List<LivestockListItem> Items { get; set; } = [];
    public PagerViewModel Pager { get; set; } = new();
    public string? Search { get; set; }
    public string? Species { get; set; }
    public string? HealthStatus { get; set; }
    public string? Sort { get; set; }
}

public class LivestockFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [MaxLength(20), Display(Name = "Animal ID")]
    public string? AnimalCode { get; set; }

    [Required, MaxLength(80), Display(Name = "Animal name")]
    public string AnimalName { get; set; } = string.Empty;

    [Required]
    public string Species { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Breed { get; set; } = string.Empty;

    [Required]
    public string Gender { get; set; } = string.Empty;

    [Required, DataType(DataType.Date), Display(Name = "Date of birth")]
    public DateTime DateOfBirth { get; set; }

    [Range(0.01, 2000)]
    public decimal Weight { get; set; }

    [Required, Display(Name = "Health status")]
    public string HealthStatus { get; set; } = Models.HealthStatuses.Healthy;

    [Required, Display(Name = "Farm")]
    public int FarmId { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Registration date")]
    public DateTime RegistrationDate { get; set; } = DateTime.Today;

    public List<FarmOption> Farms { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Models.SpeciesList.All.Contains(Species))
            yield return new ValidationResult("Choose a supported species.", [nameof(Species)]);
        if (!Models.Genders.All.Contains(Gender))
            yield return new ValidationResult("Choose a gender.", [nameof(Gender)]);
        if (!Models.HealthStatuses.All.Contains(HealthStatus))
            yield return new ValidationResult("Choose a health status.", [nameof(HealthStatus)]);
        if (DateOfBirth.Date > DateTime.Today)
            yield return new ValidationResult("Date of birth cannot be in the future.", [nameof(DateOfBirth)]);
        if (RegistrationDate.Date > DateTime.Today)
            yield return new ValidationResult("Registration date cannot be in the future.", [nameof(RegistrationDate)]);
    }
}

public class FarmOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class LivestockDetailsViewModel
{
    public Livestock Animal { get; set; } = null!;
    public List<AntimicrobialTreatment> Treatments { get; set; } = [];
    public List<MRLTest> Tests { get; set; } = [];
    public List<WithdrawalPeriod> Withdrawals { get; set; } = [];
}
