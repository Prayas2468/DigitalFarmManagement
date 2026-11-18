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
public class MRLController : PortalController
{
    private readonly ApplicationDbContext _db;
    private readonly FarmAccessService _access;
    private readonly MrlLookupService _lookup;
    private readonly NotificationSyncService _sync;

    public MRLController(ApplicationDbContext db, FarmAccessService access, MrlLookupService lookup, NotificationSyncService sync)
    {
        _db = db;
        _access = access;
        _lookup = lookup;
        _sync = sync;
    }

    public async Task<IActionResult> Index(string? productType, string? ingredient, string? result, DateTime? from, DateTime? to, int page = 1)
    {
        var all = (await _access.TestsAsync()).AsNoTracking();
        IQueryable<MRLTest> query = all.Include(t => t.Livestock);
        if (!string.IsNullOrWhiteSpace(productType))
            query = query.Where(t => t.ProductType == productType);
        if (!string.IsNullOrWhiteSpace(ingredient))
            query = query.Where(t => t.ActiveIngredient == ingredient);
        if (!string.IsNullOrWhiteSpace(result))
            query = query.Where(t => t.TestResult == result);
        if (from.HasValue)
            query = query.Where(t => t.TestDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(t => t.TestDate <= to.Value.Date);

        var list = await PaginatedList<MRLTest>.CreateAsync(query.OrderByDescending(t => t.TestDate), PageNumber(page), PageSize.Default);
        return View(new MrlIndexViewModel
        {
            Items = list,
            Pager = Pager(list, nameof(Index), RouteBag.From(
                ("productType", productType),
                ("ingredient", ingredient),
                ("result", result),
                ("from", from?.ToString("yyyy-MM-dd")),
                ("to", to?.ToString("yyyy-MM-dd")))),
            ProductType = productType,
            Ingredient = ingredient,
            Result = result,
            From = from,
            To = to,
            Compliant = await all.CountAsync(t => t.TestResult == MrlResults.Compliant),
            NonCompliant = await all.CountAsync(t => t.TestResult == MrlResults.NonCompliant),
            Pending = await all.CountAsync(t => t.TestResult == MrlResults.Pending),
            Ingredients = await _db.MrlStandards.Select(s => s.ActiveIngredient).Distinct().OrderBy(s => s).ToListAsync()
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var test = await (await _access.TestsAsync())
            .Include(t => t.Livestock).ThenInclude(l => l.Farm)
            .Include(t => t.Standard)
            .FirstOrDefaultAsync(t => t.Id == id);
        return test == null ? NotFound() : View(test);
    }

    public async Task<IActionResult> Create(int? animalId)
    {
        var model = new MrlTestFormViewModel { Country = _lookup.Country };
        if (animalId.HasValue)
            model.LivestockId = animalId.Value;
        await FillTestAsync(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MrlTestFormViewModel model)
    {
        await FillTestAsync(model);
        if (!ModelState.IsValid)
            return View("Form", model);

        var built = await BuildTestAsync(model, new MRLTest());
        if (built == null)
            return View("Form", model);

        _db.MrlTests.Add(built);
        await _db.SaveChangesAsync();
        await _sync.SyncAsync();
        Success($"Test saved as {built.TestResult}.");
        return RedirectToAction(nameof(Details), new { id = built.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var test = await (await _access.TestsAsync()).FirstOrDefaultAsync(t => t.Id == id);
        if (test == null)
            return NotFound();
        var model = new MrlTestFormViewModel
        {
            Id = test.Id,
            LivestockId = test.LivestockId,
            ProductType = test.ProductType,
            ActiveIngredient = test.ActiveIngredient,
            MeasuredResidueLevel = test.MeasuredResidueLevel,
            Unit = test.Unit,
            TestDate = test.TestDate,
            LaboratoryName = test.LaboratoryName,
            Remarks = test.Remarks,
            HoldAsPending = test.TestResult == MrlResults.Pending,
            Country = _lookup.Country
        };
        await FillTestAsync(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MrlTestFormViewModel model)
    {
        await FillTestAsync(model);
        if (!ModelState.IsValid)
            return View("Form", model);

        var test = await (await _access.TestsAsync()).FirstOrDefaultAsync(t => t.Id == model.Id);
        if (test == null)
            return NotFound();

        if (await BuildTestAsync(model, test) == null)
            return View("Form", model);

        await _db.SaveChangesAsync();
        await _sync.SyncAsync();
        Success($"Test updated. Result is now {test.TestResult}.");
        return RedirectToAction(nameof(Details), new { id = test.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var test = await (await _access.TestsAsync()).FirstOrDefaultAsync(t => t.Id == id);
        if (test == null)
            return NotFound();
        _db.MrlTests.Remove(test);
        await _db.SaveChangesAsync();
        await _sync.SyncAsync();
        Success("MRL test deleted.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Standards()
    {
        var rows = await _db.MrlStandards.AsNoTracking()
            .OrderBy(s => s.Country).ThenBy(s => s.ActiveIngredient).ThenBy(s => s.ProductType)
            .ToListAsync();
        return View(rows);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult CreateStandard() => View("StandardForm", new MrlStandardFormViewModel());

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStandard(MrlStandardFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View("StandardForm", model);
        if (await DuplicateStandard(model))
        {
            ModelState.AddModelError(string.Empty, "A limit for that country, product, ingredient, and species already exists.");
            return View("StandardForm", model);
        }

        _db.MrlStandards.Add(MapStandard(new MRLStandard(), model));
        await _db.SaveChangesAsync();
        Success("MRL limit saved. New tests will use it.");
        return RedirectToAction(nameof(Standards));
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> EditStandard(int id)
    {
        var row = await _db.MrlStandards.FindAsync(id);
        if (row == null)
            return NotFound();
        return View("StandardForm", new MrlStandardFormViewModel
        {
            Id = row.Id,
            Country = row.Country,
            ProductType = row.ProductType,
            ActiveIngredient = row.ActiveIngredient,
            Species = row.Species,
            LimitValue = row.LimitValue,
            Unit = row.Unit,
            RegulatorySource = row.RegulatorySource
        });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStandard(MrlStandardFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View("StandardForm", model);
        var row = await _db.MrlStandards.FindAsync(model.Id);
        if (row == null)
            return NotFound();
        if (await DuplicateStandard(model))
        {
            ModelState.AddModelError(string.Empty, "A limit for that country, product, ingredient, and species already exists.");
            return View("StandardForm", model);
        }

        MapStandard(row, model);
        await _db.SaveChangesAsync();
        Success("MRL limit updated.");
        return RedirectToAction(nameof(Standards));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStandard(int id)
    {
        var row = await _db.MrlStandards.FindAsync(id);
        if (row == null)
            return NotFound();
        _db.MrlStandards.Remove(row);
        await _db.SaveChangesAsync();
        Success("MRL limit removed. Older tests keep the limit that was copied onto them.");
        return RedirectToAction(nameof(Standards));
    }

    private async Task FillTestAsync(MrlTestFormViewModel model)
    {
        model.Country = _lookup.Country;
        model.Animals = await (await _access.LivestockAsync())
            .OrderBy(a => a.AnimalCode)
            .Select(a => new AnimalOption { Id = a.Id, Label = a.AnimalCode + " — " + a.AnimalName + " (" + a.Species + ")" })
            .ToListAsync();
        var standards = await _lookup.AllForCountryAsync();
        model.Ingredients = standards.Select(s => s.ActiveIngredient).Distinct().OrderBy(s => s).ToList();
        model.Standards = standards.Select(s => new MrlPreviewStandard
        {
            ProductType = s.ProductType,
            ActiveIngredient = s.ActiveIngredient,
            Species = s.Species,
            LimitValue = s.LimitValue,
            Unit = s.Unit,
            RegulatorySource = s.RegulatorySource
        }).ToList();
    }

    private async Task<MRLTest?> BuildTestAsync(MrlTestFormViewModel model, MRLTest test)
    {
        var animal = await (await _access.LivestockAsync()).FirstOrDefaultAsync(a => a.Id == model.LivestockId);
        if (animal == null)
        {
            ModelState.AddModelError(nameof(model.LivestockId), "Choose an animal from your farms.");
            return null;
        }

        var standard = await _lookup.FindAsync(model.ProductType, model.ActiveIngredient.Trim(), animal.Species);
        test.LivestockId = animal.Id;
        test.ProductType = model.ProductType;
        test.ActiveIngredient = model.ActiveIngredient.Trim();
        test.MRLStandardId = standard?.Id;
        test.MeasuredResidueLevel = model.MeasuredResidueLevel;
        test.Unit = model.Unit;
        test.ApplicableMrlLimit = standard?.LimitValue ?? 0;
        test.LimitUnit = standard?.Unit ?? model.Unit;
        test.TestDate = model.TestDate.Date;
        test.LaboratoryName = model.LaboratoryName.Trim();
        test.Remarks = string.IsNullOrWhiteSpace(model.Remarks) ? null : model.Remarks.Trim();
        test.TestResult = ResidueUnits.Evaluate(
            test.MeasuredResidueLevel,
            test.Unit,
            test.ApplicableMrlLimit,
            test.LimitUnit,
            model.HoldAsPending,
            standard != null);
        return test;
    }

    private async Task<bool> DuplicateStandard(MrlStandardFormViewModel model)
    {
        var species = model.Species?.Trim() ?? "";
        return await _db.MrlStandards.AnyAsync(s =>
            s.Id != model.Id &&
            s.Country == model.Country.Trim() &&
            s.ProductType == model.ProductType &&
            s.ActiveIngredient == model.ActiveIngredient.Trim() &&
            s.Species == species);
    }

    private static MRLStandard MapStandard(MRLStandard row, MrlStandardFormViewModel model)
    {
        row.Country = model.Country.Trim();
        row.ProductType = model.ProductType;
        row.ActiveIngredient = model.ActiveIngredient.Trim();
        row.Species = model.Species?.Trim() ?? "";
        row.LimitValue = model.LimitValue;
        row.Unit = model.Unit;
        row.RegulatorySource = model.RegulatorySource.Trim();
        return row;
    }
}
