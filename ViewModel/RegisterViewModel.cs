using System.ComponentModel.DataAnnotations;

namespace Itransition.ViewModel;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter a username")]
    [StringLength(100)]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Please enter an email address")]
    [EmailAddress]
    [StringLength(256)]
    public required string Email { get; set; }

    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Please enter a password")]
    [StringLength(40, MinimumLength = 8 , ErrorMessage = "Password must be between 8 and 40 characters")]
    public required string Password { get; set; }

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [Required(ErrorMessage = "Please enter a confirm password")]
    [StringLength(40, MinimumLength = 8 , ErrorMessage = "Password must be between 8 and 40 characters")]
    public required string ConfirmPassword { get; set; }
}
