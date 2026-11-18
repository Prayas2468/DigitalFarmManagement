using DigitalFarmManagement.Data;
using DigitalFarmManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DigitalFarmManagement.Services;

public class FarmAccessService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly UserManager<ApplicationUser> _users;

    public FarmAccessService(ApplicationDbContext db, IHttpContextAccessor http, UserManager<ApplicationUser> users)
    {
        _db = db;
        _http = http;
        _users = users;
    }

    public async Task<ApplicationUser?> CurrentUserAsync()
    {
        var principal = _http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return null;
        return await _users.GetUserAsync(principal);
    }

    public async Task<bool> IsAdminAsync()
    {
        var user = await CurrentUserAsync();
        return user != null && await _users.IsInRoleAsync(user, AppRoles.Admin);
    }

    public async Task<IQueryable<Farm>> FarmsAsync()
    {
        var user = await CurrentUserAsync();
        if (user == null)
            return _db.Farms.Where(_ => false);
        if (await _users.IsInRoleAsync(user, AppRoles.Admin))
            return _db.Farms;
        return _db.Farms.Where(f => f.OwnerId == user.Id);
    }

    public async Task<IQueryable<Livestock>> LivestockAsync()
    {
        var farms = await FarmsAsync();
        return _db.Livestock.Where(a => farms.Select(f => f.Id).Contains(a.FarmId));
    }

    public async Task<IQueryable<AntimicrobialTreatment>> TreatmentsAsync()
    {
        var animals = await LivestockAsync();
        return _db.Treatments.Where(t => animals.Select(a => a.Id).Contains(t.LivestockId));
    }

    public async Task<IQueryable<MRLTest>> TestsAsync()
    {
        var animals = await LivestockAsync();
        return _db.MrlTests.Where(t => animals.Select(a => a.Id).Contains(t.LivestockId));
    }

    public async Task<IQueryable<WithdrawalPeriod>> WithdrawalsAsync()
    {
        var animals = await LivestockAsync();
        return _db.WithdrawalPeriods.Where(w => animals.Select(a => a.Id).Contains(w.LivestockId));
    }
}
