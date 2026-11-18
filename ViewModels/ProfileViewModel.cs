using System.ComponentModel.DataAnnotations;

namespace DigitalFarmManagement.ViewModels;

public class ProfileViewModel
{
    [Required, MaxLength(80), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [Phone, Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    public string Role { get; set; } = string.Empty;

    [DataType(DataType.Password), Display(Name = "Current password")]
    public string? CurrentPassword { get; set; }

    [MinLength(6), DataType(DataType.Password), Display(Name = "New password")]
    public string? NewPassword { get; set; }

    [Compare(nameof(NewPassword)), DataType(DataType.Password), Display(Name = "Confirm new password")]
    public string? ConfirmPassword { get; set; }
}
