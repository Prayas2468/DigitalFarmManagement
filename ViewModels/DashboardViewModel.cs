namespace DigitalFarmManagement.ViewModels;

public class DashboardViewModel
{
    public bool IsAdmin { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int TotalFarms { get; set; }
    public int TotalLivestock { get; set; }
    public int TotalTreatments { get; set; }
    public int AnimalsUnderWithdrawal { get; set; }
    public decimal ComplianceRate { get; set; }
    public int ActiveMedicines { get; set; }
    public int TreatmentsThisMonth { get; set; }
    public List<string> SpeciesLabels { get; set; } = [];
    public List<int> SpeciesCounts { get; set; } = [];
    public List<string> MonthLabels { get; set; } = [];
    public List<int> MonthlyTreatmentCounts { get; set; } = [];
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public int PendingCount { get; set; }
    public List<RecentTreatmentItem> RecentTreatments { get; set; } = [];
    public List<RecentTestItem> RecentTests { get; set; } = [];
    public List<WithdrawalAlertItem> WithdrawalAlerts { get; set; } = [];
    public List<RecentTestItem> Violations { get; set; } = [];
}

public class RecentTreatmentItem
{
    public int Id { get; set; }
    public string AnimalCode { get; set; } = string.Empty;
    public string AnimalName { get; set; } = string.Empty;
    public string MedicineName { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string FarmName { get; set; } = string.Empty;
}

public class RecentTestItem
{
    public int Id { get; set; }
    public string AnimalCode { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string ActiveIngredient { get; set; } = string.Empty;
    public decimal Measured { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public string LimitUnit { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public DateTime TestDate { get; set; }
}

public class WithdrawalAlertItem
{
    public int LivestockId { get; set; }
    public string AnimalCode { get; set; } = string.Empty;
    public string AnimalName { get; set; } = string.Empty;
    public string MedicineName { get; set; } = string.Empty;
    public DateTime EligibleOn { get; set; }
    public int RemainingDays { get; set; }
}
