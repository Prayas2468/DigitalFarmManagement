using DigitalFarmManagement.Data;
using DigitalFarmManagement.Infrastructure;
using DigitalFarmManagement.Models;
using DigitalFarmManagement.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DigitalFarmManagement.Services;

public class ReportService
{
    private readonly ApplicationDbContext _db;
    private readonly FarmAccessService _access;

    public ReportService(ApplicationDbContext db, FarmAccessService access)
    {
        _db = db;
        _access = access;
    }

    public static IReadOnlyList<ReportHubItem> Hub { get; } =
    [
        new() { Key = ReportKeys.Livestock, Title = "Livestock", Description = "Animals on the farms you can see, grouped for a head-count check.", Icon = "fa-cow" },
        new() { Key = ReportKeys.UsageByMedicine, Title = "Usage by medicine", Description = "How much of each medicine has been recorded, kept in its own unit.", Icon = "fa-pills" },
        new() { Key = ReportKeys.UsageByMonth, Title = "Usage by month", Description = "Treatments started in each month of the selected range.", Icon = "fa-chart-column" },
        new() { Key = ReportKeys.Treatments, Title = "Treatment history", Description = "Every antimicrobial treatment with animal, dose, and withdrawal date.", Icon = "fa-syringe" },
        new() { Key = ReportKeys.Mrl, Title = "MRL compliance", Description = "Residue tests compared with the configured limit.", Icon = "fa-flask" },
        new() { Key = ReportKeys.MrlViolations, Title = "MRL violations", Description = "Only the tests that exceeded the applicable limit.", Icon = "fa-triangle-exclamation" },
        new() { Key = ReportKeys.Withdrawals, Title = "Withdrawal status", Description = "Animals waiting out a withdrawal period, and those already clear.", Icon = "fa-hourglass-half" },
        new() { Key = ReportKeys.Inventory, Title = "Medicine inventory", Description = "Batches on hand, including low stock and expired rows.", Icon = "fa-boxes-stacked" }
    ];

    public async Task<ReportTableViewModel?> BuildAsync(string key, DateTime? from, DateTime? to, string? search)
    {
        var today = DateTime.Today;
        var report = new ReportTableViewModel
        {
            ReportKey = key,
            From = from,
            To = to,
            Search = search
        };

        switch (key)
        {
            case ReportKeys.Livestock:
            {
                var animals = (await _access.LivestockAsync()).Include(a => a.Farm).AsQueryable();
                if (from.HasValue) animals = animals.Where(a => a.RegistrationDate >= from.Value.Date);
                if (to.HasValue) animals = animals.Where(a => a.RegistrationDate <= to.Value.Date);
                if (!string.IsNullOrWhiteSpace(search))
                    animals = animals.Where(a => a.AnimalCode.Contains(search) || a.AnimalName.Contains(search) || a.Species.Contains(search));
                var rows = await animals.OrderBy(a => a.Species).ThenBy(a => a.AnimalCode).ToListAsync();
                report.Title = "Livestock report";
                report.Description = "Registration date is used for the date filter.";
                report.Headers = ["Animal ID", "Name", "Species", "Breed", "Gender", "Weight (kg)", "Health", "Farm", "Registered"];
                report.Rows = rows.Select(a => new List<string>
                {
                    a.AnimalCode, a.AnimalName, a.Species, a.Breed, a.Gender, Ui.Qty(a.Weight), a.HealthStatus, a.Farm.FarmName, Ui.When(a.RegistrationDate)
                }).ToList();
                report.ChartType = "bar";
                foreach (var species in SpeciesList.All)
                {
                    report.ChartLabels.Add(species);
                    report.ChartValues.Add(rows.Count(a => a.Species == species));
                }
                return report;
            }
            case ReportKeys.UsageByMedicine:
            {
                var query = ApplyTreatmentFilters(await _access.TreatmentsAsync(), from, to, search);
                var grouped = await query
                    .GroupBy(t => new { t.MedicineName, t.ActiveIngredient, t.Unit })
                    .Select(g => new { g.Key.MedicineName, g.Key.ActiveIngredient, g.Key.Unit, Count = g.Count(), Total = g.Sum(t => t.Dosage) })
                    .OrderByDescending(g => g.Count)
                    .ToListAsync();
                report.Title = "Antimicrobial usage by medicine";
                report.Description = "Totals stay inside one unit. Millilitres are not added to boluses.";
                report.Headers = ["Medicine", "Active ingredient", "Treatments", "Total used", "Unit"];
                report.Rows = grouped.Select(g => new List<string> { g.MedicineName, g.ActiveIngredient, g.Count.ToString(), Ui.Qty(g.Total), g.Unit }).ToList();
                report.ChartType = "bar";
                report.ChartLabels = grouped.Select(g => g.MedicineName).ToList();
                report.ChartValues = grouped.Select(g => (decimal)g.Count).ToList();
                return report;
            }
            case ReportKeys.UsageByMonth:
            {
                var query = ApplyTreatmentFilters(await _access.TreatmentsAsync(), from, to, search);
                var dates = await query.Select(t => t.TreatmentStartDate).ToListAsync();
                var start = from?.Date ?? (dates.Count == 0 ? today.AddMonths(-5) : new DateTime(dates.Min().Year, dates.Min().Month, 1));
                var end = to?.Date ?? today;
                var cursor = new DateTime(start.Year, start.Month, 1);
                var last = new DateTime(end.Year, end.Month, 1);
                report.Title = "Antimicrobial usage by month";
                report.Description = "Each row is the number of treatments that started in that month.";
                report.Headers = ["Month", "Treatments"];
                report.ChartType = "bar";
                while (cursor <= last && report.Rows.Count < 24)
                {
                    var count = dates.Count(d => d.Year == cursor.Year && d.Month == cursor.Month);
                    report.Rows.Add([cursor.ToString("MMMM yyyy"), count.ToString()]);
                    report.ChartLabels.Add(cursor.ToString("MMM yyyy"));
                    report.ChartValues.Add(count);
                    cursor = cursor.AddMonths(1);
                }
                return report;
            }
            case ReportKeys.Treatments:
            {
                var rows = await ApplyTreatmentFilters(await _access.TreatmentsAsync(), from, to, search)
                    .Include(t => t.Livestock)
                    .OrderByDescending(t => t.TreatmentStartDate)
                    .ToListAsync();
                report.Title = "Treatment history";
                report.Description = "Withdrawal end is the first day the product may be sold.";
                report.Headers = ["Animal", "Medicine", "Ingredient", "Dose", "Route", "Start", "End", "Eligible on", "Veterinarian", "Reason"];
                report.Rows = rows.Select(t => new List<string>
                {
                    $"{t.Livestock.AnimalCode} {t.Livestock.AnimalName}",
                    t.MedicineName,
                    t.ActiveIngredient,
                    $"{Ui.Qty(t.Dosage)} {t.Unit}",
                    t.AdministrationRoute,
                    Ui.When(t.TreatmentStartDate),
                    Ui.When(t.TreatmentEndDate),
                    Ui.When(t.WithdrawalEndDate),
                    t.VeterinarianName,
                    t.ReasonForTreatment
                }).ToList();
                return report;
            }
            case ReportKeys.Mrl:
            case ReportKeys.MrlViolations:
            {
                var query = (await _access.TestsAsync()).Include(t => t.Livestock).AsQueryable();
                if (from.HasValue) query = query.Where(t => t.TestDate >= from.Value.Date);
                if (to.HasValue) query = query.Where(t => t.TestDate <= to.Value.Date);
                if (key == ReportKeys.MrlViolations)
                    query = query.Where(t => t.TestResult == MrlResults.NonCompliant);
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(t => t.ActiveIngredient.Contains(search) || t.Livestock.AnimalCode.Contains(search) || t.ProductType.Contains(search));
                var rows = await query.OrderByDescending(t => t.TestDate).ToListAsync();
                report.Title = key == ReportKeys.MrlViolations ? "MRL violations" : "MRL compliance";
                report.Description = "Measured values were compared with the limit configured for that product and ingredient.";
                report.Headers = ["Date", "Animal", "Product", "Ingredient", "Measured", "Limit", "Result", "Laboratory"];
                report.Rows = rows.Select(t => new List<string>
                {
                    Ui.When(t.TestDate),
                    t.Livestock.AnimalCode,
                    t.ProductType,
                    t.ActiveIngredient,
                    $"{Ui.Measure(t.MeasuredResidueLevel)} {t.Unit}",
                    $"{Ui.Measure(t.ApplicableMrlLimit)} {t.LimitUnit}",
                    t.TestResult,
                    t.LaboratoryName
                }).ToList();
                report.ChartType = "pie";
                report.ChartLabels = [MrlResults.Compliant, MrlResults.NonCompliant, MrlResults.Pending];
                report.ChartValues =
                [
                    rows.Count(t => t.TestResult == MrlResults.Compliant),
                    rows.Count(t => t.TestResult == MrlResults.NonCompliant),
                    rows.Count(t => t.TestResult == MrlResults.Pending)
                ];
                return report;
            }
            case ReportKeys.Withdrawals:
            {
                var query = (await _access.WithdrawalsAsync()).Include(w => w.Livestock).Include(w => w.Treatment).AsQueryable();
                if (from.HasValue) query = query.Where(w => w.ExpectedCompletionDate >= from.Value.Date);
                if (to.HasValue) query = query.Where(w => w.StartDate <= to.Value.Date);
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(w => w.Livestock.AnimalCode.Contains(search) || w.Treatment.MedicineName.Contains(search));
                var rows = await query.OrderBy(w => w.ExpectedCompletionDate).ToListAsync();
                report.Title = "Withdrawal-period status";
                report.Description = "Red means the withdrawal is still open. Yellow means three days or fewer remain. Green means the animal is clear.";
                report.Headers = ["Animal", "Species", "Medicine", "Treatment ended", "Eligible on", "Days left", "Status", "Products held"];
                report.Rows = rows.Select(w => new List<string>
                {
                    $"{w.Livestock.AnimalCode} {w.Livestock.AnimalName}",
                    w.Livestock.Species,
                    w.Treatment.MedicineName,
                    Ui.When(w.StartDate),
                    Ui.When(w.ExpectedCompletionDate),
                    WithdrawalMath.RemainingDays(w.ExpectedCompletionDate, today).ToString(),
                    WithdrawalMath.Status(w.ExpectedCompletionDate, today),
                    w.AffectedProducts
                }).ToList();
                report.ChartType = "pie";
                report.ChartLabels = [WithdrawalStatuses.Active, WithdrawalStatuses.EndingSoon, WithdrawalStatuses.Completed];
                report.ChartValues = report.ChartLabels.Select(label => (decimal)rows.Count(w => WithdrawalMath.Status(w.ExpectedCompletionDate, today) == label)).ToList();
                return report;
            }
            case ReportKeys.Inventory:
            {
                var query = _db.MedicineInventories.Include(i => i.Medicine).AsQueryable();
                if (from.HasValue) query = query.Where(i => i.PurchaseDate >= from.Value.Date);
                if (to.HasValue) query = query.Where(i => i.PurchaseDate <= to.Value.Date);
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(i => i.Medicine.MedicineName.Contains(search) || i.BatchNumber.Contains(search) || i.Medicine.ActiveIngredient.Contains(search));
                var rows = await query.OrderBy(i => i.ExpiryDate).ToListAsync();
                report.Title = "Medicine inventory";
                report.Description = "Low means the quantity is at or under the batch threshold.";
                report.Headers = ["Medicine", "Ingredient", "Batch", "Quantity", "Purchased", "Expires", "Withdrawal days", "Status"];
                report.Rows = rows.Select(i => new List<string>
                {
                    i.Medicine.MedicineName,
                    i.Medicine.ActiveIngredient,
                    i.BatchNumber,
                    $"{Ui.Qty(i.AvailableQuantity)} {i.Unit}",
                    Ui.When(i.PurchaseDate),
                    Ui.When(i.ExpiryDate),
                    i.Medicine.WithdrawalPeriodDays.ToString(),
                    i.ExpiryDate.Date < today ? "Expired" : i.AvailableQuantity <= i.LowStockThreshold ? "Low stock" : "In stock"
                }).ToList();
                return report;
            }
            default:
                return null;
        }
    }

    private static IQueryable<AntimicrobialTreatment> ApplyTreatmentFilters(IQueryable<AntimicrobialTreatment> query, DateTime? from, DateTime? to, string? search)
    {
        if (from.HasValue) query = query.Where(t => t.TreatmentStartDate >= from.Value.Date);
        if (to.HasValue) query = query.Where(t => t.TreatmentStartDate <= to.Value.Date);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.MedicineName.Contains(search) || t.ActiveIngredient.Contains(search) || t.Livestock.AnimalCode.Contains(search));
        return query;
    }
}
