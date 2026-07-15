
using Microsoft.AspNetCore.Identity;

namespace Itransition.Models;

public class ApplicationUser : IdentityUser
{
    public required string FullName { get; set; }


}
