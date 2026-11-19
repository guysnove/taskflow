using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models;

// What the registration form sends (Chapter 25).
public class RegisterInput
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That doesn't look like an email address.")]
    [StringLength(254)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Please tell us what to call you.")]
    [StringLength(60)]
    [Display(Name = "Your name")]
    public string? DisplayName { get; set; }

    [Required(ErrorMessage = "Please choose a password.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Use at least 10 characters.")]
    [DataType(DataType.Password)]                  // makes asp-for write type="password"
    public string? Password { get; set; }

    [Compare(nameof(Password), ErrorMessage = "The two passwords don't match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password again")]
    public string? ConfirmPassword { get; set; }
}

// What the sign-in form sends.
public class SignInInput
{
    [Required(ErrorMessage = "Please enter your email address.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Display(Name = "Keep me signed in on this device")]
    public bool RememberMe { get; set; }
}
