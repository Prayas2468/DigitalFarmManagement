using System.Security.Claims;
using DigitalFarmManagement.Data;
using DigitalFarmManagement.Models;
using DigitalFarmManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigitalFarmManagement.ViewComponents;

public class NavAlertsViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;

    public NavAlertsViewComponent(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = HttpContext.User;
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = _db.Notifications.AsNoTracking().AsQueryable();
        if (!user.IsInRole(AppRoles.Admin))
            query = query.Where(n => n.UserId == null || n.UserId == id);

        return View(new NavAlertsViewModel
        {
            Unread = await query.CountAsync(n => !n.IsRead),
            Latest = await query.OrderByDescending(n => n.CreatedAt).Take(5).ToListAsync()
        });
    }
}
