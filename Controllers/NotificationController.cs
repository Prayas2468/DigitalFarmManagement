using System.Security.Claims;
using DigitalFarmManagement.Data;
using DigitalFarmManagement.Models;
using DigitalFarmManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigitalFarmManagement.Controllers;

[Authorize]
public class NotificationController : PortalController
{
    private readonly ApplicationDbContext _db;
    private readonly NotificationSyncService _sync;

    public NotificationController(ApplicationDbContext db, NotificationSyncService sync)
    {
        _db = db;
        _sync = sync;
    }

    public async Task<IActionResult> Index(string? category)
    {
        await _sync.SyncAsync();
        var query = Visible();
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(n => n.Category == category);

        ViewBag.Category = category;
        return View(await query.OrderByDescending(n => n.CreatedAt).ToListAsync());
    }

    public async Task<IActionResult> Open(int id)
    {
        var note = await Visible().FirstOrDefaultAsync(n => n.Id == id);
        if (note == null)
            return NotFound();
        note.IsRead = true;
        await _db.SaveChangesAsync();
        if (!string.IsNullOrWhiteSpace(note.LinkUrl) && Url.IsLocalUrl(note.LinkUrl))
            return Redirect(note.LinkUrl);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAll()
    {
        var notes = await Visible().Where(n => !n.IsRead).ToListAsync();
        foreach (var note in notes)
            note.IsRead = true;
        await _db.SaveChangesAsync();
        Success("All visible alerts marked as read.");
        return RedirectToAction(nameof(Index));
    }

    private IQueryable<Notification> Visible()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = _db.Notifications.AsQueryable();
        if (!User.IsInRole(AppRoles.Admin))
            query = query.Where(n => n.UserId == null || n.UserId == id);
        return query;
    }
}
