using DigitalFarmManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalFarmManagement.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly DashboardService _dashboard;
    private readonly NotificationSyncService _sync;

    public DashboardController(DashboardService dashboard, NotificationSyncService sync)
    {
        _dashboard = dashboard;
        _sync = sync;
    }

    public async Task<IActionResult> Index()
    {
        await _sync.SyncAsync();
        return View(await _dashboard.BuildAsync());
    }
}
