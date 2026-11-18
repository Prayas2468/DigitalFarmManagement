using DigitalFarmManagement.Data;
using DigitalFarmManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalFarmManagement.Services;

public class NotificationSyncService
{
    private readonly ApplicationDbContext _db;

    public NotificationSyncService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task SyncAsync()
    {
        var today = DateTime.Today;
        var desired = new Dictionary<string, Notification>(StringComparer.Ordinal);

        var violations = await _db.MrlTests
            .Include(t => t.Livestock).ThenInclude(l => l.Farm)
            .Where(t => t.TestResult == MrlResults.NonCompliant)
            .ToListAsync();
        foreach (var test in violations)
        {
            desired[$"mrl:{test.Id}"] = new Notification
            {
                SourceKey = $"mrl:{test.Id}",
                Title = "MRL limit exceeded",
                Message = $"{test.Livestock.AnimalCode} {test.ProductType}: {test.ActiveIngredient} measured {test.MeasuredResidueLevel:0.####} {test.Unit} against a limit of {test.ApplicableMrlLimit:0.####} {test.LimitUnit}.",
                Category = AlertCategories.Mrl,
                Severity = "Danger",
                UserId = test.Livestock.Farm.OwnerId,
                LinkUrl = $"/MRL/Details/{test.Id}"
            };
        }

        var openWithdrawals = await _db.Treatments
            .Include(t => t.Livestock).ThenInclude(l => l.Farm)
            .Where(t => t.WithdrawalEndDate > today)
            .ToListAsync();
        foreach (var treatment in openWithdrawals)
        {
            var remaining = (treatment.WithdrawalEndDate.Date - today).Days;
            var soon = remaining <= 3;
            var key = soon ? $"wd-soon:{treatment.Id}" : $"wd-active:{treatment.Id}";
            desired[key] = new Notification
            {
                SourceKey = key,
                Title = soon ? "Withdrawal period ending soon" : "Withdrawal period active",
                Message = $"{treatment.Livestock.AnimalCode} ({treatment.Livestock.AnimalName}) is eligible on {treatment.WithdrawalEndDate:dd MMM yyyy}. {remaining} day(s) remain after {treatment.MedicineName}.",
                Category = AlertCategories.Withdrawal,
                Severity = soon ? "Warning" : "Danger",
                UserId = treatment.Livestock.Farm.OwnerId,
                LinkUrl = $"/Withdrawal/Details/{treatment.LivestockId}"
            };
        }

        var batches = await _db.MedicineInventories.Include(b => b.Medicine).ToListAsync();
        foreach (var batch in batches)
        {
            if (batch.ExpiryDate.Date < today && batch.AvailableQuantity > 0)
            {
                var key = $"exp:{batch.Id}";
                desired[key] = new Notification
                {
                    SourceKey = key,
                    Title = "Expired medicine",
                    Message = $"{batch.Medicine.MedicineName} batch {batch.BatchNumber} expired on {batch.ExpiryDate:dd MMM yyyy}. Do not use it for a new treatment.",
                    Category = AlertCategories.Inventory,
                    Severity = "Danger",
                    LinkUrl = "/Medicine"
                };
            }
            else if (batch.AvailableQuantity <= batch.LowStockThreshold)
            {
                var key = $"low:{batch.Id}";
                desired[key] = new Notification
                {
                    SourceKey = key,
                    Title = "Low medicine inventory",
                    Message = $"{batch.Medicine.MedicineName} batch {batch.BatchNumber} has {batch.AvailableQuantity:0.###} {batch.Unit} left.",
                    Category = AlertCategories.Inventory,
                    Severity = "Warning",
                    LinkUrl = "/Medicine"
                };
            }
        }

        var followUps = await _db.Treatments
            .Include(t => t.Livestock).ThenInclude(l => l.Farm)
            .Where(t => t.TreatmentEndDate >= today && t.TreatmentEndDate <= today.AddDays(2))
            .ToListAsync();
        foreach (var treatment in followUps)
        {
            var key = $"follow:{treatment.Id}";
            desired[key] = new Notification
            {
                SourceKey = key,
                Title = "Upcoming treatment follow-up",
                Message = $"{treatment.Livestock.AnimalCode} finishes {treatment.MedicineName} on {treatment.TreatmentEndDate:dd MMM yyyy}.",
                Category = AlertCategories.Treatment,
                Severity = "Info",
                UserId = treatment.Livestock.Farm.OwnerId,
                LinkUrl = $"/Treatment/Details/{treatment.Id}"
            };
        }

        var existing = await _db.Notifications.Where(n => n.SourceKey != null).ToListAsync();
        foreach (var stale in existing.Where(n => n.SourceKey != null && !desired.ContainsKey(n.SourceKey)))
            _db.Notifications.Remove(stale);

        var map = existing.Where(n => n.SourceKey != null).ToDictionary(n => n.SourceKey!, StringComparer.Ordinal);
        foreach (var (key, draft) in desired)
        {
            if (map.TryGetValue(key, out var row))
            {
                row.Title = draft.Title;
                row.Message = draft.Message;
                row.Category = draft.Category;
                row.Severity = draft.Severity;
                row.LinkUrl = draft.LinkUrl;
                row.UserId = draft.UserId;
            }
            else
            {
                draft.IsRead = false;
                draft.CreatedAt = DateTime.Now;
                _db.Notifications.Add(draft);
            }
        }

        await _db.SaveChangesAsync();
    }
}
