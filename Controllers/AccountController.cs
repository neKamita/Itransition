using Itransition.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Itransition.ViewModel;
using Itransition.Models.Profiles;
using Itransition.Models.Cvs;
using Itransition.Models.Attributes;
using Itransition.Data;

using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace Itransition.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly RoleManager<IdentityRole> roleManager;
    private readonly IEmailSender emailSender;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountController> logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IEmailSender emailSender,
        ApplicationDbContext context,
        ILogger<AccountController> logger)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.emailSender = emailSender;
        this._context = context;
        this.logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel
        {
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null
        });
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel login)
    {
        if (!ModelState.IsValid) return View(login);
        var res = await signInManager.PasswordSignInAsync(login.EmailAddress, login.Password, login.RememberMe, lockoutOnFailure: true);
        if (res.Succeeded)
        {
            return Url.IsLocalUrl(login.ReturnUrl)
                ? LocalRedirect(login.ReturnUrl)
                : RedirectToAction("Index", "Home");
        }
        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(login);
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel register)
    {
        if (!ModelState.IsValid) return View(register);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var user = CreateUser(register);
        var result = await userManager.CreateAsync(user, register.Password);

        if (!result.Succeeded)
        {
            await transaction.RollbackAsync();
            return AddRegisterErrors(result, register);
        }

        try
        {
            var roleResult = await EnsureRoleExists("Candidate");
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return AddRegisterErrors(roleResult, register);
            }

            var addToRoleResult = await userManager.AddToRoleAsync(user, "Candidate");
            if (!addToRoleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return AddRegisterErrors(addToRoleResult, register);
            }

            var profile = new CandidateProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                Projects = new List<ProjectProfile>(),
                AttributeValues =
                [
                    NewBuiltInValue(BuiltInAttributeKeys.FirstNameId, register.FirstName),
                    NewBuiltInValue(BuiltInAttributeKeys.LastNameId, register.LastName),
                    NewBuiltInValue(BuiltInAttributeKeys.LocationId, register.Location),
                    NewBuiltInValue(BuiltInAttributeKeys.PersonalPhotoId, null)
                ]
            };

            foreach (var attributeValue in profile.AttributeValues)
            {
                attributeValue.CandidateProfileId = profile.Id;
                attributeValue.CandidateProfile = profile;
            }

            _context.CandidateProfiles.Add(profile);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await signInManager.SignInAsync(user, false);
            logger.LogInformation("User {UserId} registered with Candidate role", user.Id);
            return RedirectToAction("Index", "Home");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            logger.LogError(exception, "Registration failed after creating user {UserId}", user.Id);
            ModelState.AddModelError(string.Empty, "Registration could not be completed. Please try again.");
            return View(register);
        }
    }

    private ApplicationUser CreateUser(RegisterViewModel r)
    {
        return new ApplicationUser
        {
            FullName = $"{r.FirstName.Trim()} {r.LastName.Trim()}",
            Email = r.Email,
            UserName = r.Email
        };
    }

    private static UserAttributeValue NewBuiltInValue(Guid definitionId, string? value)
    {
        return new UserAttributeValue
        {
            Id = Guid.NewGuid(),
            CandidateProfileId = Guid.Empty,
            CandidateProfile = null!,
            AttributeDefinitionId = definitionId,
            AttributeDefinition = null!,
            Value = string.IsNullOrWhiteSpace(value) ? null : value.Trim()
        };
    }

    private async Task<IdentityResult> EnsureRoleExists(string roleName)
    {
        return await roleManager.RoleExistsAsync(roleName)
            ? IdentityResult.Success
            : await roleManager.CreateAsync(new IdentityRole(roleName));
    }

    private IActionResult AddRegisterErrors(IdentityResult res, RegisterViewModel vm)
    {
        foreach (var error in res.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return View(vm);
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(VerifyViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is not null)
        {
            await SendResetEmail(user);
        }

        ViewBag.Success = "If an account exists for this email, a password reset link has been sent.";
        return View(model);
    }

    private async Task SendResetEmail(ApplicationUser user)
    {
        try
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme);
            await emailSender.SendEmailAsync(user.Email!, "Reset Password", $"Reset password link: <a href='{link}'>Click Here</a>");
            logger.LogInformation("Password reset email requested for user {UserId}", user.Id);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to send a password reset email for user {UserId}", user.Id);
        }
    }

    [HttpGet]
    public IActionResult ResetPassword(string token, string email)
    {
        if (token == null || email == null) return RedirectToAction("Login");
        return View(new ResetPasswordViewModel { Token = token, Email = email });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user == null) return RedirectToAction("Login");
        var result = await userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded) return RedirectToAction("Login");
        foreach (var err in result.Errors) ModelState.AddModelError(string.Empty, err.Description);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }
}
