using Itransition.Models;
using Microsoft.AspNetCore.Identity;

namespace Itransition.ViewModel;

public class AdminUserListViewModel
{
    public required string Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string RoleName { get; set; }
    public required bool IsBlocked { get; set; }

}
