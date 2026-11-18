namespace DigitalFarmManagement.Models;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Farmer = "Farmer";
}

public static class FarmTypes
{
    public static readonly string[] All =
    [
        "Dairy",
        "Buffalo Dairy",
        "Poultry",
        "Small Ruminant",
        "Pig",
        "Mixed Livestock"
    ];
}

public static class SpeciesList
{
    public static readonly string[] All = ["Cattle", "Buffalo", "Goat", "Sheep", "Pig", "Poultry"];
}

public static class Genders
{
    public static readonly string[] All = ["Female", "Male"];
}

public static class HealthStatuses
{
    public const string Healthy = "Healthy";
    public const string UnderTreatment = "Under Treatment";
    public const string Recovering = "Recovering";
    public const string Critical = "Critical";
    public static readonly string[] All = [Healthy, UnderTreatment, Recovering, Critical];
}

public static class ProductTypes
{
    public static readonly string[] All = ["Milk", "Meat", "Eggs"];
}

public static class MrlResults
{
    public const string Compliant = "Compliant";
    public const string NonCompliant = "Non-Compliant";
    public const string Pending = "Pending";
    public static readonly string[] All = [Compliant, NonCompliant, Pending];
}

public static class MrlUnits
{
    public static readonly string[] All = ["µg/kg", "mg/kg"];
}

public static class MedicineCategories
{
    public static readonly string[] All =
    [
        "Penicillin",
        "Tetracycline",
        "Fluoroquinolone",
        "Macrolide",
        "Sulfonamide",
        "Aminoglycoside"
    ];
}

public static class DoseUnits
{
    public static readonly string[] All = ["ml", "mg", "g", "bolus", "tablet"];
}

public static class AdminRoutes
{
    public static readonly string[] All =
    [
        "Intramuscular",
        "Subcutaneous",
        "Intravenous",
        "Oral",
        "Intramammary",
        "Topical"
    ];
}

public static class WithdrawalStatuses
{
    public const string Active = "Active";
    public const string EndingSoon = "Ending Soon";
    public const string Completed = "Completed";
}

public static class AlertCategories
{
    public const string Mrl = "MRL";
    public const string Withdrawal = "Withdrawal";
    public const string Inventory = "Inventory";
    public const string Treatment = "Treatment";
    public static readonly string[] All = [Mrl, Withdrawal, Inventory, Treatment];
}

public static class ReportKeys
{
    public const string Livestock = "livestock";
    public const string UsageByMedicine = "usage-medicine";
    public const string UsageByMonth = "usage-month";
    public const string Treatments = "treatments";
    public const string Mrl = "mrl";
    public const string MrlViolations = "mrl-violations";
    public const string Withdrawals = "withdrawals";
    public const string Inventory = "inventory";
}
