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
public class MedicineController : PortalController
{
    private readonly ApplicationDbContext _db;
    private readonly NotificationSyncService _sync;

    public MedicineController(ApplicationDbContext db, NotificationSyncService sync)
    {
        _db = db;
        _sync = sync;
    }

    public async Task<IActionResult> Index(string? search, string? category, int page = 1)
    {
        var today = DateTime.Today;
        var query = _db.MedicineInventories.AsNoTracking().Include(i => i.Medicine).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i => i.Medicine.MedicineName.Contains(term) || i.Medicine.ActiveIngredient.Contains(term) || i.BatchNumber.Contains(term) || i.Medicine.Manufacturer.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(i => i.Medicine.Category == category);

        var result = await PaginatedList<MedicineInventory>.CreateAsync(query.OrderBy(i => i.Medicine.MedicineName), PageNumber(page), PageSize.Default);
        var all = _db.MedicineInventories.AsNoTracking();
        return View(new MedicineIndexViewModel
        {
            Items = result.Select(MapItem).ToList(),
            Pager = Pager(result, nameof(Index), RouteBag.From(("search", search), ("category", category))),
            Search = search,
            Category = category,
            LowStock = await all.CountAsync(i => i.AvailableQuantity <= i.LowStockThreshold && i.ExpiryDate >= today),
            Expired = await all.CountAsync(i => i.ExpiryDate < today && i.AvailableQuantity > 0)
        });
    }

    public IActionResult Create() => View("Form", new MedicineFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MedicineFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Form", model);

        var medicine = await _db.Medicines.FirstOrDefaultAsync(m =>
            m.MedicineName == model.MedicineName.Trim() && m.ActiveIngredient == model.ActiveIngredient.Trim());
        if (medicine == null)
        {
            medicine = new Medicine();
            ApplyMedicine(medicine, model);
            _db.Medicines.Add(medicine);
        }
        else
        {
            ApplyMedicine(medicine, model);
            if (await _db.MedicineInventories.AnyAsync(i => i.MedicineId == medicine.Id && i.BatchNumber == model.BatchNumber.Trim()))
            {
                ModelState.AddModelError(nameof(model.BatchNumber), "That batch number already exists for this medicine.");
                return View("Form", model);
            }
        }

        _db.MedicineInventories.Add(new MedicineInventory
        {
            Medicine = medicine,
            BatchNumber = model.BatchNumber.Trim(),
            AvailableQuantity = model.AvailableQuantity,
            Unit = model.Unit,
            PurchaseDate = model.PurchaseDate.Date,
            ExpiryDate = model.ExpiryDate.Date,
            LowStockThreshold = model.LowStockThreshold
        });
        await _db.SaveChangesAsync();
        await _sync.SyncAsync();
        Success("Medicine batch added to inventory.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var batch = await _db.MedicineInventories.Include(i => i.Medicine).FirstOrDefaultAsync(i => i.Id == id);
        if (batch == null)
            return NotFound();
        return View("Form", new MedicineFormViewModel
        {
            Id = batch.Id,
            MedicineName = batch.Medicine.MedicineName,
            ActiveIngredient = batch.Medicine.ActiveIngredient,
            Category = batch.Medicine.Category,
            Manufacturer = batch.Medicine.Manufacturer,
            WithdrawalPeriodDays = batch.Medicine.WithdrawalPeriodDays,
            BatchNumber = batch.BatchNumber,
            AvailableQuantity = batch.AvailableQuantity,
            Unit = batch.Unit,
            PurchaseDate = batch.PurchaseDate,
            ExpiryDate = batch.ExpiryDate,
            LowStockThreshold = batch.LowStockThreshold
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MedicineFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Form", model);

        var batch = await _db.MedicineInventories.Include(i => i.Medicine).FirstOrDefaultAsync(i => i.Id == model.Id);
        if (batch == null)
            return NotFound();
        if (await _db.MedicineInventories.AnyAsync(i => i.MedicineId == batch.MedicineId && i.BatchNumber == model.BatchNumber.Trim() && i.Id != batch.Id))
        {
            ModelState.AddModelError(nameof(model.BatchNumber), "That batch number already exists for this medicine.");
            return View("Form", model);
        }

        ApplyMedicine(batch.Medicine, model);
        batch.BatchNumber = model.BatchNumber.Trim();
        batch.AvailableQuantity = model.AvailableQuantity;
        batch.Unit = model.Unit;
        batch.PurchaseDate = model.PurchaseDate.Date;
        batch.ExpiryDate = model.ExpiryDate.Date;
        batch.LowStockThreshold = model.LowStockThreshold;
        await _db.SaveChangesAsync();
        await _sync.SyncAsync();
        Success("Inventory updated. Treatments already recorded keep their original dose and withdrawal period.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var batch = await _db.MedicineInventories.Include(i => i.Medicine).ThenInclude(m => m.Batches).Include(i => i.Treatments)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (batch == null)
            return NotFound();
        if (batch.Treatments.Count > 0)
        {
            Failure("This batch has been used in a treatment, so it stays in the history.");
            return RedirectToAction(nameof(Index));
        }

        var medicine = batch.Medicine;
        if (medicine.Batches.Count <= 1 && !await _db.Treatments.AnyAsync(t => t.MedicineId == medicine.Id))
            _db.Medicines.Remove(medicine);
        else
            _db.MedicineInventories.Remove(batch);

        await _db.SaveChangesAsync();
        await _sync.SyncAsync();
        Success("Medicine batch removed.");
        return RedirectToAction(nameof(Index));
    }

    private static void ApplyMedicine(Medicine medicine, MedicineFormViewModel model)
    {
        medicine.MedicineName = model.MedicineName.Trim();
        medicine.ActiveIngredient = model.ActiveIngredient.Trim();
        medicine.Category = model.Category;
        medicine.Manufacturer = model.Manufacturer.Trim();
        medicine.WithdrawalPeriodDays = model.WithdrawalPeriodDays;
        medicine.IsActive = true;
    }

    private static MedicineListItem MapItem(MedicineInventory batch) => new()
    {
        Id = batch.Id,
        MedicineId = batch.MedicineId,
        MedicineName = batch.Medicine.MedicineName,
        ActiveIngredient = batch.Medicine.ActiveIngredient,
        Category = batch.Medicine.Category,
        Manufacturer = batch.Medicine.Manufacturer,
        BatchNumber = batch.BatchNumber,
        AvailableQuantity = batch.AvailableQuantity,
        Unit = batch.Unit,
        PurchaseDate = batch.PurchaseDate,
        ExpiryDate = batch.ExpiryDate,
        LowStockThreshold = batch.LowStockThreshold,
        WithdrawalPeriodDays = batch.Medicine.WithdrawalPeriodDays
    };
}
