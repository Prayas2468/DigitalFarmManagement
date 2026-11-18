using System.ComponentModel.DataAnnotations;

namespace DigitalFarmManagement.Models;

public class Livestock : IAuditable
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string AnimalCode { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string AnimalName { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Species { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Breed { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string Gender { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public decimal Weight { get; set; }

    [Required, MaxLength(30)]
    public string HealthStatus { get; set; } = HealthStatuses.Healthy;

    public int FarmId { get; set; }
    public Farm Farm { get; set; } = null!;

    public DateTime RegistrationDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<AntimicrobialTreatment> Treatments { get; set; } = new List<AntimicrobialTreatment>();
    public ICollection<MRLTest> MrlTests { get; set; } = new List<MRLTest>();
    public ICollection<WithdrawalPeriod> Withdrawals { get; set; } = new List<WithdrawalPeriod>();
    public ICollection<ProductClearance> Clearances { get; set; } = new List<ProductClearance>();
}
