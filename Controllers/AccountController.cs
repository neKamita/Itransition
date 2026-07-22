using Itransition.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Itransition.ViewModel;
using Itransition.Models.Profiles;
using Itransition.Models.Cvs;
using Itransition.Data;

using Microsoft.AspNetCore.Identity.UI.Services;

namespace Itransition.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly RoleManager<IdentityRole> roleManager;
    private readonly IEmailSender emailSender;
    private readonly ApplicationDbContext _context;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IEmailSender emailSender,
        ApplicationDbContext context)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.emailSender = emailSender;
        this._context = context;
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel login)
    {
        if (!ModelState.IsValid) return View(login);
        var res = await signInManager.PasswordSignInAsync(login.EmailAddress, login.Password, login.RememberMe, false);
        if (res.Succeeded) return RedirectToAction("Index", "Home");
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
        var user = CreateUser(register);
        var result = await userManager.CreateAsync(user, register.Password);

             var profile = new CandidateProfile {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            FirstName = register.Name,
            LastName = "",
            Location = "",
            PersonalPhotoUrl = "",
            Projects = new List<ProjectProfile>(),
            AttributeValues = new List<UserAttributeValue>()
        };

        _context.CandidateProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return await HandleRegisterResult(result, user, register);
    }

    private ApplicationUser CreateUser(RegisterViewModel r)
    {
        return new ApplicationUser { FullName = r.Name, Email = r.Email, UserName = r.Email, NormalizedEmail = r.Email.ToUpper(), NormalizedUserName = r.Email.ToUpper() };
    }

    private async Task<IActionResult> HandleRegisterResult(IdentityResult res, ApplicationUser u, RegisterViewModel vm)
    {
        if (!res.Succeeded) return AddRegisterErrors(res, vm);
        await EnsureRoleExists("Candidate");
        await userManager.AddToRoleAsync(u, "Candidate");
        await signInManager.SignInAsync(u, false);
        return RedirectToAction("Index", "Home");
    }

    private async Task EnsureRoleExists(string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));
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
        if (user == null) return ShowError(model, "User with this email was not found.");
        return await SendResetEmail(user, model);
    }

    private async Task<IActionResult> SendResetEmail(ApplicationUser user, VerifyViewModel model)
    {
        try {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme);
            await emailSender.SendEmailAsync(user.Email!, "Reset Password", $"Reset password link: <a href='{link}'>Click Here</a>");
            ViewBag.Success = "Password reset link has been sent to your email.";
            return View(model);
        } catch {
            return ShowError(model, "Failed to send email. Please check SMTP settings.");
        }
    }

    private IActionResult ShowError(VerifyViewModel model, string error)
    {
        ModelState.AddModelError(string.Empty, error);
        return View(model);
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
