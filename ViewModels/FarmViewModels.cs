using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DigitalFarmManagement.ViewModels;

public class FarmListItem
{
    public int Id { get; set; }
    public string FarmName { get; set; } = string.Empty;
    public string FarmerName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FarmType { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public int LivestockCount { get; set; }
}

public class FarmIndexViewModel
{
    public List<FarmListItem> Items { get; set; } = [];
    public PagerViewModel Pager { get; set; } = new();
    public string? Search { get; set; }
    public string? Location { get; set; }
    public string? FarmType { get; set; }
    public string? Sort { get; set; }
    public List<string> Locations { get; set; } = [];
}

public class FarmFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required, MaxLength(120), Display(Name = "Farm name")]
    public string FarmName { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Farmer name")]
    public string FarmerName { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Location { get; set; } = string.Empty;

    [Required, MaxLength(20), Phone, Display(Name = "Contact number")]
    public string ContactNumber { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required, Display(Name = "Farm type")]
    public string FarmType { get; set; } = string.Empty;

    [Required, DataType(DataType.Date), Display(Name = "Registration date")]
    public DateTime RegistrationDate { get; set; } = DateTime.Today;

    [Display(Name = "Portal owner")]
    public string? OwnerId { get; set; }

    public bool CanAssignOwner { get; set; }
    public List<SelectListItem> OwnerOptions { get; set; } = [];
    public List<string> KnownLocations { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Models.FarmTypes.All.Contains(FarmType))
            yield return new ValidationResult("Choose a valid farm type.", [nameof(FarmType)]);
        if (RegistrationDate.Date > DateTime.Today)
            yield return new ValidationResult("Registration date cannot be in the future.", [nameof(RegistrationDate)]);
    }
}
