using DigitalFarmManagement.Infrastructure;
using DigitalFarmManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DigitalFarmManagement.Controllers;

public abstract class PortalController : Controller
{
    protected void Success(string message) => TempData["Success"] = message;

    protected void Failure(string message) => TempData["Error"] = message;

    protected int PageNumber(int page) => page < 1 ? 1 : page;

    protected PagerViewModel Pager<T>(PaginatedList<T> page, string action, Dictionary<string, string> routes) => new()
    {
        Page = page.PageIndex,
        TotalPages = page.TotalPages,
        TotalCount = page.TotalCount,
        Controller = ControllerContext.ActionDescriptor.ControllerName,
        Action = action,
        Routes = routes
    };
}
