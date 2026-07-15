using System.ComponentModel.DataAnnotations;

namespace Itransition.ViewModel;

public class ChangePasswordViewModel
{
    [EmailAddress]
    [Required(ErrorMessage = "Please enter a valid email address")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Please enter a password")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public required string NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    [Required(ErrorMessage = "Please repeat a password")]
    [Display(Name = "Confirm new password")]
    public required string ConfirmedNewPassword { get; set; }
}
