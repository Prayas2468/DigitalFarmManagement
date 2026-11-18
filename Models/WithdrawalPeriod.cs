using System.ComponentModel.DataAnnotations;

namespace DigitalFarmManagement.Models;

public class WithdrawalPeriod : IAuditable
{
    public int Id { get; set; }

    public int LivestockId { get; set; }
    public Livestock Livestock { get; set; } = null!;

    public int TreatmentId { get; set; }
    public AntimicrobialTreatment Treatment { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime ExpectedCompletionDate { get; set; }
    public int TotalDays { get; set; }

    [MaxLength(80)]
    public string AffectedProducts { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
