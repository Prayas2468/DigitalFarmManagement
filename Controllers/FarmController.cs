using DigitalFarmManagement.Data;
using DigitalFarmManagement.Infrastructure;
using DigitalFarmManagement.Models;
using DigitalFarmManagement.Services;
using DigitalFarmManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DigitalFarmManagement.Controllers;

[Authorize]
public class FarmController : PortalController
{
    private readonly ApplicationDbContext _db;
    private readonly FarmAccessService _access;
    private readonly UserManager<ApplicationUser> _users;

    public FarmController(ApplicationDbContext db, FarmAccessService access, UserManager<ApplicationUser> users)
    {
        _db = db;
        _access = access;
        _users = users;
    }

    public async Task<IActionResult> Index(string? search, string? location, string? farmType, string? sort, int page = 1)
    {
        var query = (await _access.FarmsAsync()).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(f => f.FarmName.Contains(term) || f.FarmerName.Contains(term) || f.Email.Contains(term) || f.ContactNumber.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(f => f.Location == location);
        if (!string.IsNullOrWhiteSpace(farmType))
            query = query.Where(f => f.FarmType == farmType);

        query = sort switch
        {
            "location" => query.OrderBy(f => f.Location).ThenBy(f => f.FarmName),
            "date" => query.OrderByDescending(f => f.RegistrationDate),
            "farmer" => query.OrderBy(f => f.FarmerName),
            _ => query.OrderBy(f => f.FarmName)
        };

        var result = await PaginatedList<Farm>.CreateAsync(query, PageNumber(page), PageSize.Default);
        var counts = await _db.Livestock.AsNoTracking()
            .Where(a => result.Select(f => f.Id).Contains(a.FarmId))
            .GroupBy(a => a.FarmId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var vm = new FarmIndexViewModel
        {
            Items = result.Select(f => new FarmListItem
            {
                Id = f.Id,
                FarmName = f.FarmName,
                FarmerName = f.FarmerName,
                Location = f.Location,
                ContactNumber = f.ContactNumber,
                Email = f.Email,
                FarmType = f.FarmType,
                RegistrationDate = f.RegistrationDate,
                LivestockCount = counts.FirstOrDefault(c => c.Key == f.Id)?.Count ?? 0
            }).ToList(),
            Pager = Pager(result, nameof(Index), RouteBag.From(("search", search), ("location", location), ("farmType", farmType), ("sort", sort))),
            Search = search,
            Location = location,
            FarmType = farmType,
            Sort = sort,
            Locations = await (await _access.FarmsAsync()).Select(f => f.Location).Distinct().OrderBy(l => l).ToListAsync()
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var farm = await (await _access.FarmsAsync())
            .Include(f => f.Livestock)
            .Include(f => f.Owner)
            .FirstOrDefaultAsync(f => f.Id == id);
        return farm == null ? NotFound() : View(farm);
    }

    public async Task<IActionResult> Create()
    {
        var model = new FarmFormViewModel();
        await FillAsync(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FarmFormViewModel model)
    {
        await FillAsync(model);
        if (!ModelState.IsValid)
            return View("Form", model);

        var farm = new Farm();
        Apply(farm, model);
        _db.Farms.Add(farm);
        await _db.SaveChangesAsync();
        Success($"{farm.FarmName} was added.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var farm = await (await _access.FarmsAsync()).FirstOrDefaultAsync(f => f.Id == id);
        if (farm == null)
            return NotFound();
        var model = Map(farm);
        await FillAsync(model);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(FarmFormViewModel model)
    {
        await FillAsync(model);
        if (!ModelState.IsValid)
            return View("Form", model);

        var farm = await (await _access.FarmsAsync()).FirstOrDefaultAsync(f => f.Id == model.Id);
        if (farm == null)
            return NotFound();

        Apply(farm, model);
        await _db.SaveChangesAsync();
        Success($"{farm.FarmName} was updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var farm = await (await _access.FarmsAsync()).Include(f => f.Livestock).FirstOrDefaultAsync(f => f.Id == id);
        if (farm == null)
            return NotFound();
        if (farm.Livestock.Count > 0)
        {
            Failure("Move or delete the livestock on this farm before deleting it.");
            return RedirectToAction(nameof(Index));
        }

        _db.Farms.Remove(farm);
        await _db.SaveChangesAsync();
        Success("Farm deleted.");
        return RedirectToAction(nameof(Index));
    }

    private async Task FillAsync(FarmFormViewModel model)
    {
        var me = await _access.CurrentUserAsync();
        model.CanAssignOwner = await _access.IsAdminAsync();
        model.KnownLocations = await _db.Farms.Select(f => f.Location).Distinct().OrderBy(l => l).ToListAsync();
        if (model.CanAssignOwner)
        {
            model.OwnerOptions = await _users.Users.OrderBy(u => u.FullName).Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = u.FullName + " (" + u.Email + ")"
            }).ToListAsync();
        }
        else if (me != null)
        {
            model.OwnerId = me.Id;
        }
    }

    private void Apply(Farm farm, FarmFormViewModel model)
    {
        farm.FarmName = model.FarmName.Trim();
        farm.FarmerName = model.FarmerName.Trim();
        farm.Location = model.Location.Trim();
        farm.ContactNumber = model.ContactNumber.Trim();
        farm.Email = model.Email.Trim();
        farm.FarmType = model.FarmType;
        farm.RegistrationDate = model.RegistrationDate.Date;
        if (User.IsInRole(AppRoles.Admin))
            farm.OwnerId = string.IsNullOrWhiteSpace(model.OwnerId) ? null : model.OwnerId;
        else if (string.IsNullOrEmpty(farm.OwnerId))
            farm.OwnerId = _users.GetUserId(User);
    }

    private static FarmFormViewModel Map(Farm farm) => new()
    {
        Id = farm.Id,
        FarmName = farm.FarmName,
        FarmerName = farm.FarmerName,
        Location = farm.Location,
        ContactNumber = farm.ContactNumber,
        Email = farm.Email,
        FarmType = farm.FarmType,
        RegistrationDate = farm.RegistrationDate,
        OwnerId = farm.OwnerId
    };
}
