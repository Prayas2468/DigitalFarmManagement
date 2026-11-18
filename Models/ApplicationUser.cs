using Microsoft.AspNetCore.Identity;

namespace DigitalFarmManagement.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ICollection<Farm> Farms { get; set; } = new List<Farm>();
}
