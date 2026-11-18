using DigitalFarmManagement.Data;
using DigitalFarmManagement.Models;
using DigitalFarmManagement.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DigitalFarmManagement.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly FarmAccessService _access;

    public DashboardService(ApplicationDbContext db, FarmAccessService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<DashboardViewModel> BuildAsync()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var user = await _access.CurrentUserAsync();
        var farms = await _access.FarmsAsync();
        var animals = await _access.LivestockAsync();
        var treatments = await _access.TreatmentsAsync();
        var tests = await _access.TestsAsync();

        var resultCounts = await tests
            .GroupBy(t => t.TestResult)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();
        int CountOf(string key) => resultCounts.FirstOrDefault(r => r.Key == key)?.Count ?? 0;
        var compliant = CountOf(MrlResults.Compliant);
        var violations = CountOf(MrlResults.NonCompliant);
        var decided = compliant + violations;

        var speciesRows = await animals
            .GroupBy(a => a.Species)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var chartStart = monthStart.AddMonths(-5);
        var starts = await treatments
            .Where(t => t.TreatmentStartDate >= chartStart)
            .Select(t => t.TreatmentStartDate)
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            IsAdmin = await _access.IsAdminAsync(),
            DisplayName = string.IsNullOrWhiteSpace(user?.FullName) ? user?.Email ?? "there" : user!.FullName,
            TotalFarms = await farms.CountAsync(),
            TotalLivestock = await animals.CountAsync(),
            TotalTreatments = await treatments.CountAsync(),
            AnimalsUnderWithdrawal = await animals.CountAsync(a => a.Treatments.Any(t => t.WithdrawalEndDate > today)),
            ActiveMedicines = await _db.Medicines.CountAsync(m => m.IsActive && m.Batches.Any(b => b.AvailableQuantity > 0 && b.ExpiryDate >= today)),
            TreatmentsThisMonth = await treatments.CountAsync(t => t.TreatmentStartDate >= monthStart),
            ComplianceRate = decided == 0 ? 0 : Math.Round(compliant * 100m / decided, 1),
            CompliantCount = compliant,
            NonCompliantCount = violations,
            PendingCount = CountOf(MrlResults.Pending)
        };

        foreach (var species in SpeciesList.All)
        {
            vm.SpeciesLabels.Add(species);
            vm.SpeciesCounts.Add(speciesRows.FirstOrDefault(s => s.Key == species)?.Count ?? 0);
        }

        for (var i = 0; i < 6; i++)
        {
            var month = chartStart.AddMonths(i);
            vm.MonthLabels.Add(month.ToString("MMM yyyy"));
            vm.MonthlyTreatmentCounts.Add(starts.Count(d => d.Year == month.Year && d.Month == month.Month));
        }

        vm.RecentTreatments = await treatments
            .OrderByDescending(t => t.TreatmentStartDate)
            .Take(6)
            .Select(t => new RecentTreatmentItem
            {
                Id = t.Id,
                AnimalCode = t.Livestock.AnimalCode,
                AnimalName = t.Livestock.AnimalName,
                MedicineName = t.MedicineName,
                Start = t.TreatmentStartDate,
                End = t.TreatmentEndDate,
                FarmName = t.Livestock.Farm.FarmName
            })
            .ToListAsync();

        vm.RecentTests = await tests
            .OrderByDescending(t => t.TestDate)
            .Take(6)
            .Select(t => new RecentTestItem
            {
                Id = t.Id,
                AnimalCode = t.Livestock.AnimalCode,
                ProductType = t.ProductType,
                ActiveIngredient = t.ActiveIngredient,
                Measured = t.MeasuredResidueLevel,
                Unit = t.Unit,
                Limit = t.ApplicableMrlLimit,
                LimitUnit = t.LimitUnit,
                Result = t.TestResult,
                TestDate = t.TestDate
            })
            .ToListAsync();

        vm.Violations = await tests
            .Where(t => t.TestResult == MrlResults.NonCompliant)
            .OrderByDescending(t => t.TestDate)
            .Take(5)
            .Select(t => new RecentTestItem
            {
                Id = t.Id,
                AnimalCode = t.Livestock.AnimalCode,
                ProductType = t.ProductType,
                ActiveIngredient = t.ActiveIngredient,
                Measured = t.MeasuredResidueLevel,
                Unit = t.Unit,
                Limit = t.ApplicableMrlLimit,
                LimitUnit = t.LimitUnit,
                Result = t.TestResult,
                TestDate = t.TestDate
            })
            .ToListAsync();

        vm.WithdrawalAlerts = await treatments
            .Where(t => t.WithdrawalEndDate > today && t.WithdrawalEndDate <= today.AddDays(3))
            .OrderBy(t => t.WithdrawalEndDate)
            .Select(t => new WithdrawalAlertItem
            {
                LivestockId = t.LivestockId,
                AnimalCode = t.Livestock.AnimalCode,
                AnimalName = t.Livestock.AnimalName,
                MedicineName = t.MedicineName,
                EligibleOn = t.WithdrawalEndDate
            })
            .ToListAsync();
        foreach (var alert in vm.WithdrawalAlerts)
            alert.RemainingDays = (alert.EligibleOn.Date - today).Days;

        return vm;
    }
}
