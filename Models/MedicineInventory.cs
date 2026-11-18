using System.ComponentModel.DataAnnotations;

namespace DigitalFarmManagement.Models;

public class MedicineInventory : IAuditable
{
    public int Id { get; set; }

    public int MedicineId { get; set; }
    public Medicine Medicine { get; set; } = null!;

    [Required, MaxLength(40)]
    public string BatchNumber { get; set; } = string.Empty;

    public decimal AvailableQuantity { get; set; }

    [Required, MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    public DateTime PurchaseDate { get; set; }
    public DateTime ExpiryDate { get; set; }

    public decimal LowStockThreshold { get; set; } = 10;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<AntimicrobialTreatment> Treatments { get; set; } = new List<AntimicrobialTreatment>();
}
