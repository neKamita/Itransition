using System.ComponentModel.DataAnnotations;

namespace Itransition.ViewModel;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter a username")]
    public string Name  { get; set; }

    [Required(ErrorMessage = "Please enter a password")]
    [EmailAddress]
    public string Email { get; set; }

    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Please enter a password")]
    [StringLength(40, MinimumLength = 8 , ErrorMessage = "Password must be between 8 and 40 characters")]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [Required(ErrorMessage = "Please enter a confirm password")]
    [StringLength(40, MinimumLength = 8 , ErrorMessage = "Password must be between 8 and 40 characters")]
    public string ConfirmPassword { get; set; }
}
