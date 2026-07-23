using System.ComponentModel.DataAnnotations;

namespace Itransition.ViewModel;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your first name")]
    [StringLength(50)]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Please enter your last name")]
    [StringLength(50)]
    public required string LastName { get; set; }

    [StringLength(100)]
    public string? Location { get; set; }

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
