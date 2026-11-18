using System.Security.Claims;
using DigitalFarmManagement.Data;
using DigitalFarmManagement.Infrastructure;
using DigitalFarmManagement.Models;
using DigitalFarmManagement.Services;
using DigitalFarmManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigitalFarmManagement.Controllers;

[Authorize]
public class TreatmentController : PortalController
{
    private readonly ApplicationDbContext _db;
    private readonly FarmAccessService _access;
    private readonly AnimalHealthService _health;
    private readonly NotificationSyncService _sync;

    public TreatmentController(ApplicationDbContext db, FarmAccessService access, AnimalHealthService health, NotificationSyncService sync)
    {
        _db = db;
        _access = access;
        _health = health;
        _sync = sync;
    }

    public async Task<IActionResult> Index(string? search, int? animalId, DateTime? from, DateTime? to, int page = 1)
    {
        var today = DateTime.Today;
        var all = (await _access.TreatmentsAsync()).AsNoTracking();
        IQueryable<AntimicrobialTreatment> query = all.Include(t => t.Livestock).ThenInclude(l => l.Farm);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t => t.MedicineName.Contains(term) || t.ActiveIngredient.Contains(term) || t.VeterinarianName.Contains(term));
        }
        if (animalId.HasValue)
            query = query.Where(t => t.LivestockId == animalId);
        if (from.HasValue)
            query = query.Where(t => t.TreatmentStartDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(t => t.TreatmentStartDate <= to.Value.Date);

        var result = await PaginatedList<AntimicrobialTreatment>.CreateAsync(
            query.OrderByDescending(t => t.TreatmentStartDate), PageNumber(page), PageSize.Default);

        var usage = await all.GroupBy(t => t.Unit)
            .Select(g => new UsageTotal { Unit = g.Key, Total = g.Sum(t => t.Dosage) })
            .ToListAsync();
        var frequent = await all.GroupBy(t => new { t.MedicineName, t.Unit })
            .Select(g => new MedicineFrequency
            {
                Name = g.Key.MedicineName,
                Unit = g.Key.Unit,
                Count = g.Count(),
                TotalDosage = g.Sum(t => t.Dosage)
            })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToListAsync();

        return View(new TreatmentIndexViewModel
        {
            Items = result,
            Pager = Pager(result, nameof(Index), RouteBag.From(
                ("search", search),
                ("animalId", animalId?.ToString()),
                ("from", from?.ToString("yyyy-MM-dd")),
                ("to", to?.ToString("yyyy-MM-dd")))),
            Search = search,
            AnimalId = animalId,
            From = from,
            To = to,
            ActiveAnimals = await all.Where(t => t.TreatmentEndDate >= today).Select(t => t.LivestockId).Distinct().CountAsync(),
            UsageTotals = usage,
            Frequent = frequent,
            Animals = await AnimalOptions()
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var treatment = await (await _access.TreatmentsAsync())
            .Include(t => t.Livestock).ThenInclude(l => l.Farm)
            .Include(t => t.MedicineInventory)
            .Include(t => t.Withdrawal)
            .FirstOrDefaultAsync(t => t.Id == id);
        return treatment == null ? NotFound() : View(treatment);
    }

    public async Task<IActionResult> Create(int? animalId)
    {
        var model = new TreatmentFormViewModel();
        if (animalId.HasValue)
            model.LivestockId = animalId.Value;
        await FillAsync(model, null);
        if (model.Animals.Count == 0)
        {
            Failure("Register an animal before you record a treatment.");
            return RedirectToAction("Create", "Livestock");
        }
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TreatmentFormViewModel model)
    {
        await FillAsync(model, null);
        if (!ModelState.IsValid)
            return View("Form", model);

        var saved = await _health.SaveTreatmentAsync(model, UserId());
        if (!saved.Ok)
        {
            ModelState.AddModelError(string.Empty, saved.Error ?? "The treatment could not be saved.");
            return View("Form", model);
        }

        await _sync.SyncAsync();
        Success("Treatment recorded. Stock was reduced and the withdrawal date was calculated.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var treatment = await (await _access.TreatmentsAsync()).FirstOrDefaultAsync(t => t.Id == id);
        if (treatment == null)
            return NotFound();
        var model = new TreatmentFormViewModel
        {
            Id = treatment.Id,
            LivestockId = treatment.LivestockId,
            MedicineInventoryId = treatment.MedicineInventoryId,
            Dosage = treatment.Dosage,
            AdministrationRoute = treatment.AdministrationRoute,
            TreatmentStartDate = treatment.TreatmentStartDate,
            TreatmentEndDate = treatment.TreatmentEndDate,
            VeterinarianName = treatment.VeterinarianName,
            ReasonForTreatment = treatment.ReasonForTreatment,
            Remarks = treatment.Remarks
        };
        await FillAsync(model, treatment.MedicineInventoryId);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TreatmentFormViewModel model)
    {
        await FillAsync(model, model.MedicineInventoryId);
        if (!ModelState.IsValid)
            return View("Form", model);

        var saved = await _health.SaveTreatmentAsync(model, UserId());
        if (!saved.Ok)
        {
            ModelState.AddModelError(string.Empty, saved.Error ?? "The treatment could not be saved.");
            return View("Form", model);
        }

        await _sync.SyncAsync();
        Success("Treatment updated. The withdrawal date follows the new end date.");
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _health.DeleteTreatmentAsync(id);
        if (!deleted.Ok)
        {
            Failure(deleted.Error ?? "The treatment could not be deleted.");
            return RedirectToAction(nameof(Index));
        }

        await _sync.SyncAsync();
        Success("Treatment deleted and the dose was returned to stock.");
        return RedirectToAction(nameof(Index));
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    private async Task<List<AnimalOption>> AnimalOptions()
        => await (await _access.LivestockAsync())
            .OrderBy(a => a.AnimalCode)
            .Select(a => new AnimalOption { Id = a.Id, Label = a.AnimalCode + " — " + a.AnimalName + " (" + a.Species + ")" })
            .ToListAsync();

    private async Task FillAsync(TreatmentFormViewModel model, int? currentBatchId)
    {
        var today = DateTime.Today;
        model.Animals = await AnimalOptions();
        model.Batches = await _db.MedicineInventories.AsNoTracking()
            .Include(b => b.Medicine)
            .Where(b => b.Medicine.IsActive && (b.ExpiryDate >= today || b.Id == currentBatchId))
            .OrderBy(b => b.Medicine.MedicineName)
            .Select(b => new BatchOption
            {
                Id = b.Id,
                MedicineName = b.Medicine.MedicineName,
                Category = b.Medicine.Category,
                ActiveIngredient = b.Medicine.ActiveIngredient,
                Unit = b.Unit,
                WithdrawalDays = b.Medicine.WithdrawalPeriodDays,
                Stock = b.AvailableQuantity,
                Expired = b.ExpiryDate < today,
                Label = b.Medicine.MedicineName + " · " + b.BatchNumber + " · " + b.AvailableQuantity.ToString("0.###") + " " + b.Unit
            })
            .ToListAsync();
    }
}
