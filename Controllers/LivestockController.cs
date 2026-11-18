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
public class LivestockController : PortalController
{
    private readonly ApplicationDbContext _db;
    private readonly FarmAccessService _access;

    public LivestockController(ApplicationDbContext db, FarmAccessService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<IActionResult> Index(string? search, string? species, string? healthStatus, string? sort, int page = 1)
    {
        var today = DateTime.Today;
        IQueryable<Livestock> query = (await _access.LivestockAsync()).AsNoTracking().Include(a => a.Farm);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a => a.AnimalCode.Contains(term) || a.AnimalName.Contains(term) || a.Breed.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(species))
            query = query.Where(a => a.Species == species);
        if (!string.IsNullOrWhiteSpace(healthStatus))
            query = query.Where(a => a.HealthStatus == healthStatus);

        query = sort switch
        {
            "name" => query.OrderBy(a => a.AnimalName),
            "species" => query.OrderBy(a => a.Species).ThenBy(a => a.AnimalCode),
            "status" => query.OrderBy(a => a.HealthStatus),
            _ => query.OrderBy(a => a.AnimalCode)
        };

        var result = await PaginatedList<Livestock>.CreateAsync(query, PageNumber(page), PageSize.Default);
        var ids = result.Select(a => a.Id).ToList();
        var held = await _db.Treatments.AsNoTracking()
            .Where(t => ids.Contains(t.LivestockId) && t.WithdrawalEndDate > today)
            .Select(t => t.LivestockId)
            .Distinct()
            .ToListAsync();

        return View(new LivestockIndexViewModel
        {
            Items = result.Select(a => new LivestockListItem
            {
                Id = a.Id,
                AnimalCode = a.AnimalCode,
                AnimalName = a.AnimalName,
                Species = a.Species,
                Breed = a.Breed,
                Gender = a.Gender,
                Weight = a.Weight,
                HealthStatus = a.HealthStatus,
                FarmName = a.Farm.FarmName,
                UnderWithdrawal = held.Contains(a.Id)
            }).ToList(),
            Pager = Pager(result, nameof(Index), RouteBag.From(("search", search), ("species", species), ("healthStatus", healthStatus), ("sort", sort))),
            Search = search,
            Species = species,
            HealthStatus = healthStatus,
            Sort = sort
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var animal = await (await _access.LivestockAsync())
            .Include(a => a.Farm)
            .Include(a => a.Treatments)
            .Include(a => a.MrlTests)
            .Include(a => a.Withdrawals).ThenInclude(w => w.Treatment)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (animal == null)
            return NotFound();

        return View(new LivestockDetailsViewModel
        {
            Animal = animal,
            Treatments = animal.Treatments.OrderByDescending(t => t.TreatmentStartDate).ToList(),
            Tests = animal.MrlTests.OrderByDescending(t => t.TestDate).ToList(),
            Withdrawals = animal.Withdrawals.OrderByDescending(w => w.ExpectedCompletionDate).ToList()
        });
    }

    public async Task<IActionResult> Create()
    {
        var model = new LivestockFormViewModel();
        await FillAsync(model);
        if (model.Farms.Count == 0)
        {
            Failure("Add a farm before you register an animal.");
            return RedirectToAction("Create", "Farm");
        }
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LivestockFormViewModel model)
    {
        await FillAsync(model);
        if (!await OwnsFarm(model.FarmId))
            ModelState.AddModelError(nameof(model.FarmId), "Choose one of your farms.");
        var code = await NextCodeAsync(model.AnimalCode);
        if (!ModelState.IsValid)
            return View("Form", model);

        var animal = new Livestock { AnimalCode = code };
        Apply(animal, model);
        _db.Livestock.Add(animal);
        await _db.SaveChangesAsync();
        Success($"{animal.AnimalCode} {animal.AnimalName} was registered.");
        return RedirectToAction(nameof(Details), new { id = animal.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var animal = await (await _access.LivestockAsync()).FirstOrDefaultAsync(a => a.Id == id);
        if (animal == null)
            return NotFound();
        var model = Map(animal);
        await FillAsync(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LivestockFormViewModel model)
    {
        await FillAsync(model);
        var animal = await (await _access.LivestockAsync()).FirstOrDefaultAsync(a => a.Id == model.Id);
        if (animal == null)
            return NotFound();
        if (!await OwnsFarm(model.FarmId))
            ModelState.AddModelError(nameof(model.FarmId), "Choose one of your farms.");
        if (await _db.Livestock.AnyAsync(a => a.AnimalCode == model.AnimalCode && a.Id != animal.Id))
            ModelState.AddModelError(nameof(model.AnimalCode), "That animal ID is already in use.");
        if (!ModelState.IsValid)
            return View("Form", model);

        animal.AnimalCode = string.IsNullOrWhiteSpace(model.AnimalCode) ? animal.AnimalCode : model.AnimalCode.Trim().ToUpperInvariant();
        Apply(animal, model);
        await _db.SaveChangesAsync();
        Success("Livestock record updated.");
        return RedirectToAction(nameof(Details), new { id = animal.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var animal = await (await _access.LivestockAsync())
            .Include(a => a.Treatments)
            .Include(a => a.MrlTests)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (animal == null)
            return NotFound();
        if (animal.Treatments.Count > 0 || animal.MrlTests.Count > 0)
        {
            Failure("This animal has treatments or residue tests, so the record stays in the history.");
            return RedirectToAction(nameof(Details), new { id });
        }

        _db.Livestock.Remove(animal);
        await _db.SaveChangesAsync();
        Success("Livestock record deleted.");
        return RedirectToAction(nameof(Index));
    }

    private async Task FillAsync(LivestockFormViewModel model)
    {
        model.Farms = await (await _access.FarmsAsync())
            .OrderBy(f => f.FarmName)
            .Select(f => new FarmOption { Id = f.Id, Name = f.FarmName + " · " + f.Location })
            .ToListAsync();
    }

    private async Task<bool> OwnsFarm(int farmId)
        => await (await _access.FarmsAsync()).AnyAsync(f => f.Id == farmId);

    private async Task<string> NextCodeAsync(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            var code = requested.Trim().ToUpperInvariant();
            if (await _db.Livestock.AnyAsync(a => a.AnimalCode == code))
                ModelState.AddModelError(nameof(LivestockFormViewModel.AnimalCode), "That animal ID is already in use.");
            return code;
        }

        var number = await _db.Livestock.CountAsync() + 1001;
        var generated = $"AN-{number:0000}";
        while (await _db.Livestock.AnyAsync(a => a.AnimalCode == generated))
        {
            number++;
            generated = $"AN-{number:0000}";
        }
        return generated;
    }

    private static void Apply(Livestock animal, LivestockFormViewModel model)
    {
        animal.AnimalName = model.AnimalName.Trim();
        animal.Species = model.Species;
        animal.Breed = model.Breed.Trim();
        animal.Gender = model.Gender;
        animal.DateOfBirth = model.DateOfBirth.Date;
        animal.Weight = model.Weight;
        animal.HealthStatus = model.HealthStatus;
        animal.FarmId = model.FarmId;
        animal.RegistrationDate = model.RegistrationDate.Date;
    }

    private static LivestockFormViewModel Map(Livestock animal) => new()
    {
        Id = animal.Id,
        AnimalCode = animal.AnimalCode,
        AnimalName = animal.AnimalName,
        Species = animal.Species,
        Breed = animal.Breed,
        Gender = animal.Gender,
        DateOfBirth = animal.DateOfBirth,
        Weight = animal.Weight,
        HealthStatus = animal.HealthStatus,
        FarmId = animal.FarmId,
        RegistrationDate = animal.RegistrationDate
    };
}
