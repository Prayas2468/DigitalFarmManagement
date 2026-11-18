using System.Security.Claims;
using DigitalFarmManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DigitalFarmManagement.Infrastructure;

public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public AppClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        var name = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "User" : user.FullName;
        identity.AddClaim(new Claim("FullName", name));
        return identity;
    }
}
