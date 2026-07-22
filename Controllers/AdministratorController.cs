using Itransition.Data;
using Itransition.Models;
using Itransition.Services;
using Itransition.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itransition.Controllers;

[Authorize(Policy = "RequireAdministratorRole")]
public class AdministratorController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly AdministratorBulkActionService _bulkActionService;
    private readonly ILogger<AdministratorController> _logger;

    public AdministratorController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext dbContext,
        AdministratorBulkActionService bulkActionService,
        ILogger<AdministratorController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _bulkActionService = bulkActionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ListUsers()
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.Email)
            .ToListAsync();

        var roleAssignments = await (
                from userRole in _dbContext.UserRoles
                join role in _dbContext.Roles on userRole.RoleId equals role.Id
                select new { userRole.UserId, RoleName = role.Name! })
            .AsNoTracking()
            .ToListAsync();

        var rolesByUser = roleAssignments
            .GroupBy(assignment => assignment.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(item => item.RoleName).Order().ToList());

        var result = users.Select(user => new AdminUserListViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            RoleNames = rolesByUser.GetValueOrDefault(user.Id, []),
            IsBlocked = user.LockoutEnd > DateTimeOffset.UtcNow
        }).ToList();

        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAction(
        string action,
        List<string>? userIds,
        string? roleName)
    {
        if (userIds is null || userIds.Count == 0)
        {
            TempData["AdminNotice"] = "Select at least one user.";
            return RedirectToAction(nameof(ListUsers));
        }

        var selfId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(selfId))
        {
            return Challenge();
        }

        AdministratorBulkActionResult result;
        try
        {
            result = await _bulkActionService.ExecuteAsync(
                action,
                userIds,
                roleName,
                selfId,
                HttpContext.RequestAborted);
        }
        catch (AdministratorBulkActionException exception)
        {
            _logger.LogWarning(
                exception,
                "Administrator {AdministratorId} could not complete action {Action}",
                selfId,
                action);
            TempData["AdminError"] = exception.Message;
            return RedirectToAction(nameof(ListUsers));
        }

        if (result.CurrentUserChanged)
        {
            var currentUser = await _userManager.FindByIdAsync(selfId);
            if (currentUser is not null)
            {
                await _signInManager.RefreshSignInAsync(currentUser);
            }
        }

        _logger.LogInformation(
            "Administrator {AdministratorId} completed action {Action} for {ChangedUsers} user(s)",
            selfId,
            action,
            result.ChangedUsers);

        TempData["AdminSuccess"] = $"{action} completed for {result.ChangedUsers} user(s).";

        return result.CurrentAdministratorRoleRemoved
            ? RedirectToAction("Index", "Home")
            : RedirectToAction(nameof(ListUsers));
    }
}
