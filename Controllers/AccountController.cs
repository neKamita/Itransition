using Itransition.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Itransition.ViewModel;
namespace Itransition.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly RoleManager<IdentityRole> roleManager;


    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.roleManager = roleManager;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel login)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }

    var result = await signInManager.PasswordSignInAsync(login.EmailAddress, login.Password, login.RememberMe, lockoutOnFailure:  false);

    if (result.Succeeded)
    {
        return RedirectToAction("Index", "Home");
    }

    ModelState.AddModelError(string.Empty, "Invalid email or password.");
    return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel register)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }

    var user = new ApplicationUser
    {
        FullName = register.Name,
        Email = register.Email,
        UserName = register.Email,
        NormalizedEmail = register.Email.ToUpper(),
        NormalizedUserName = register.Email.ToUpper(),
    };

    var result = await userManager.CreateAsync(user, register.Password);
    if (result.Succeeded)
    {
        var roleExists = await roleManager.RoleExistsAsync("Candidate");
        if (!roleExists)
        {
            await roleManager.CreateAsync(new IdentityRole("Candidate"));
        }
        await userManager.AddToRoleAsync(user, "Candidate");
        await signInManager.SignInAsync(user, isPersistent: false);

        return RedirectToAction("Login", "Account");
    }

    foreach (var error in result.Errors)
    {
        ModelState.AddModelError(string.Empty, error.Description);
    }
    return View();
    }


    [HttpGet]
    public IActionResult EmailVerify()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmailVerify(VerifyViewModel verify)
    {
        if (!ModelState.IsValid)
        {
            return View(verify);
        }

        var user = await userManager.FindByEmailAsync(verify.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "User not found.");
            return View(verify);
        }
        else{
            return RedirectToAction("ChangePassword", "Account", new { username = user.UserName });
        }
    }

    [HttpGet]
    public IActionResult ChangePassword(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            return RedirectToAction("VerifyEmail", "Account");
        }

        return View(new ChangePasswordViewModel(){Email = username, NewPassword ="", ConfirmedNewPassword = ""});
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel changePassword)
    {
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Please enter valid data.");
            return View(changePassword);
        }

        var user = await userManager.FindByNameAsync(changePassword.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "User not found.");
            return View(changePassword);
        }
        var result = await userManager.RemovePasswordAsync(user);
        if (result.Succeeded)
        {
            result = await userManager.AddPasswordAsync(user, changePassword.NewPassword);
            return RedirectToAction("Login", "Account");
        }
        else{
         foreach(var error in result.Errors)
         {
            ModelState.AddModelError(string.Empty, error.Description);
         }
         return View(changePassword);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }

}
