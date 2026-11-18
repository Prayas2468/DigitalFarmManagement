using DigitalFarmManagement.Models;
using DigitalFarmManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DigitalFarmManagement.Controllers;

[Authorize]
public class ProfileController : PortalController
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;

    public ProfileController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
    {
        _users = users;
        _signIn = signIn;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Challenge();
        return View(await MapAsync(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Challenge();

        if (!ModelState.IsValid)
        {
            model.Email = user.Email ?? "";
            model.Role = await RoleName(user);
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = model.PhoneNumber;
        var updated = await _users.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            foreach (var error in updated.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            model.Email = user.Email ?? "";
            model.Role = await RoleName(user);
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(model.CurrentPassword))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Enter your current password to set a new one.");
                model.Email = user.Email ?? "";
                model.Role = await RoleName(user);
                return View(model);
            }

            var changed = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!changed.Succeeded)
            {
                foreach (var error in changed.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                model.Email = user.Email ?? "";
                model.Role = await RoleName(user);
                return View(model);
            }
        }

        await _signIn.RefreshSignInAsync(user);
        Success("Profile saved.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProfileViewModel> MapAsync(ApplicationUser user) => new()
    {
        FullName = user.FullName,
        Email = user.Email ?? "",
        PhoneNumber = user.PhoneNumber,
        Role = await RoleName(user)
    };

    private async Task<string> RoleName(ApplicationUser user)
    {
        var roles = await _users.GetRolesAsync(user);
        return roles.FirstOrDefault() ?? "User";
    }
}
