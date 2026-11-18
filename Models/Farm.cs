using System.ComponentModel.DataAnnotations;

namespace DigitalFarmManagement.Models;

public class Farm : IAuditable
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string FarmName { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string FarmerName { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Location { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string ContactNumber { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string FarmType { get; set; } = string.Empty;

    public DateTime RegistrationDate { get; set; }

    public string? OwnerId { get; set; }
    public ApplicationUser? Owner { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Livestock> Livestock { get; set; } = new List<Livestock>();
}
