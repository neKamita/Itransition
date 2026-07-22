using System.ComponentModel.DataAnnotations;

namespace Itransition.ViewModel;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [StringLength(256)]
    public string EmailAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }

    [StringLength(2048)]
    public string? ReturnUrl { get; set; }
}
