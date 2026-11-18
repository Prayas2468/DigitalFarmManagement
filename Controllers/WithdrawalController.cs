using System.Security.Claims;
using DigitalFarmManagement.Infrastructure;
using DigitalFarmManagement.Models;
using DigitalFarmManagement.Services;
using DigitalFarmManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigitalFarmManagement.Controllers;

[Authorize]
public class WithdrawalController : PortalController
{
    private readonly FarmAccessService _access;
    private readonly AnimalHealthService _health;

    public WithdrawalController(FarmAccessService access, AnimalHealthService health)
    {
        _access = access;
        _health = health;
    }

    public async Task<IActionResult> Index(string? status, string? search, int page = 1)
    {
        var today = DateTime.Today;
        IQueryable<WithdrawalPeriod> query = (await _access.WithdrawalsAsync())
            .AsNoTracking()
            .Include(w => w.Livestock)
            .Include(w => w.Treatment);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(w => w.Livestock.AnimalCode.Contains(term) || w.Livestock.AnimalName.Contains(term) || w.Treatment.MedicineName.Contains(term));
        }

        var rows = await query.OrderBy(w => w.ExpectedCompletionDate).ToListAsync();
        var allItems = rows.Select(w => Map(w, today)).ToList();
        var items = string.IsNullOrWhiteSpace(status) ? allItems : allItems.Where(i => i.Status == status).ToList();

        var paged = items.Skip((PageNumber(page) - 1) * PageSize.Default).Take(PageSize.Default).ToList();
        var pagerSource = new PaginatedList<WithdrawalListItem>(paged, items.Count, PageNumber(page), PageSize.Default);

        return View(new WithdrawalIndexViewModel
        {
            Items = paged,
            Pager = Pager(pagerSource, nameof(Index), RouteBag.From(("status", status), ("search", search))),
            Status = status,
            Search = search,
            Active = allItems.Count(i => i.Status == WithdrawalStatuses.Active),
            EndingSoon = allItems.Count(i => i.Status == WithdrawalStatuses.EndingSoon),
            Completed = allItems.Count(i => i.Status == WithdrawalStatuses.Completed)
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var today = DateTime.Today;
        var animal = await (await _access.LivestockAsync())
            .Include(a => a.Farm)
            .Include(a => a.Withdrawals).ThenInclude(w => w.Treatment)
            .Include(a => a.Clearances)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (animal == null)
            return NotFound();

        return View(new WithdrawalDetailsViewModel
        {
            Animal = animal,
            Periods = animal.Withdrawals
                .OrderByDescending(w => w.ExpectedCompletionDate)
                .Select(w => Map(w, today))
                .ToList(),
            Clearances = animal.Clearances.OrderByDescending(c => c.ClearedOn).ToList(),
            Products = SpeciesInfo.ProductsFor(animal.Species)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkEligible(int id, string product)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var result = await _health.MarkEligibleAsync(id, product, userId);
        if (!result.Ok)
            Failure(result.Error ?? "This product cannot be cleared yet.");
        else
            Success($"{product} was marked eligible for sale.");
        return RedirectToAction(nameof(Details), new { id });
    }

    private static WithdrawalListItem Map(WithdrawalPeriod row, DateTime today) => new()
    {
        Id = row.Id,
        LivestockId = row.LivestockId,
        AnimalCode = row.Livestock.AnimalCode,
        AnimalName = row.Livestock.AnimalName,
        Species = row.Livestock.Species,
        MedicineName = row.Treatment.MedicineName,
        TreatmentEnd = row.StartDate,
        EligibleOn = row.ExpectedCompletionDate,
        RemainingDays = WithdrawalMath.RemainingDays(row.ExpectedCompletionDate, today),
        Status = WithdrawalMath.Status(row.ExpectedCompletionDate, today),
        AffectedProducts = row.AffectedProducts
    };
}
