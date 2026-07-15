using System.ComponentModel.DataAnnotations;

namespace Itransition.ViewModel;

public class VerifyViewModel
{
    [Required(ErrorMessage = "Please is required")]
    [EmailAddress]
    public required string Email { get; set; }

}
