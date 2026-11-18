using DigitalFarmManagement.Models;

namespace DigitalFarmManagement.ViewModels;

public class WithdrawalListItem
{
    public int Id { get; set; }
    public int LivestockId { get; set; }
    public string AnimalCode { get; set; } = string.Empty;
    public string AnimalName { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string MedicineName { get; set; } = string.Empty;
    public DateTime TreatmentEnd { get; set; }
    public DateTime EligibleOn { get; set; }
    public int RemainingDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AffectedProducts { get; set; } = string.Empty;
}

public class WithdrawalIndexViewModel
{
    public List<WithdrawalListItem> Items { get; set; } = [];
    public PagerViewModel Pager { get; set; } = new();
    public string? Status { get; set; }
    public string? Search { get; set; }
    public int Active { get; set; }
    public int EndingSoon { get; set; }
    public int Completed { get; set; }
}

public class WithdrawalDetailsViewModel
{
    public Livestock Animal { get; set; } = null!;
    public List<WithdrawalListItem> Periods { get; set; } = [];
    public List<ProductClearance> Clearances { get; set; } = [];
    public IReadOnlyList<string> Products { get; set; } = [];
}
