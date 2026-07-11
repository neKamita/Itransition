using System.ComponentModel.DataAnnotations;

namespace Itransition.ViewModel;

public class ChangePasswordViewModel
{
    [EmailAddress]
    [Required(ErrorMessage = "Please enter a valid email address")]
    public string Email { get; set; }
    
    [Required(ErrorMessage = "Please enter a password")]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string NewPassword { get; set; }
    
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    [Required(ErrorMessage = "Please repeat a password")]
    [Display(Name = "Confirm password")]
    public string ConfirmedNewPassword { get; set; }
}