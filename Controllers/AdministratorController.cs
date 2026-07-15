using Itransition.Models;
using Itransition.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Itransition.ViewModel;

namespace Itransition.Controllers;


[Authorize(Roles = "Administrator")]
public class AdministratorController : Controller
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ApplicationDbContext dbContext;

    public AdministratorController(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
    {
        this.userManager = userManager;
        this.dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> ListUsers()
    {
        var result = new List<AdminUserListViewModel>();

        foreach (var user in userManager.Users.ToList())
        {
            var roles = await userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "No Role";
            result.Add(new AdminUserListViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleName = userRole,
                IsBlocked = user.LockoutEnd > DateTimeOffset.UtcNow
            });
        }

        return View(result);
    }

    [HttpPost]
    public async Task<IActionResult> BulkAction(string action, List<string> userIds)
    {
        var selfId = userManager.GetUserId(User);
        var users = userManager.Users.Where(x => userIds.Contains(x.Id) && x.Id != selfId).ToList();



        if (action == "Block")
        {
            foreach (var user in users)
            {
                user.LockoutEnd = DateTimeOffset.MaxValue;
                await userManager.UpdateAsync(user);
            }
        }
        else if (action == "Unblock")
        {
            foreach (var user in users)
            {
                user.LockoutEnd = null;
                await userManager.UpdateAsync(user);
            }
        }
        else if (action == "Delete")
        {
            foreach (var user in users)
            {
                await userManager.DeleteAsync(user);
            }
        }
        return RedirectToAction("ListUsers");
    }
}
