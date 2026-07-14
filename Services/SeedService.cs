using Itransition.Data;
using Itransition.Models;
using Microsoft.AspNetCore.Identity;

namespace Itransition.Services;

public class SeedService
{
    public static async Task SeedDataBase(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedService>>();
        

        try
        {
            logger.LogInformation("Ensuring the databse is created.");
            await context.Database.EnsureCreatedAsync();
            
            logger.LogInformation("Ensuring Roles are created.");
            await AddRoleAsync(roleManager,"Admin");
            await AddRoleAsync(roleManager,"User");
            
            logger.LogInformation("Ensuring Admin is created.");
            var adminEmail = "admin@admin.com";
            var adminPassword = "Admin123!";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var AdminUser = new ApplicationUser{
                    FullName = "Admin",
                    UserName = adminEmail,
                    NormalizedUserName = adminEmail.ToUpper(),
                    Email = adminEmail,
                    NormalizedEmail = adminEmail.ToUpper(),
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };
                
                var result = await userManager.CreateAsync(AdminUser, adminPassword);
                if (result.Succeeded)
                {
                    logger.LogInformation("Assigning Admin role to User.");
                    await userManager.AddToRoleAsync(AdminUser, "Admin");
                }
                else
                {
                    logger.LogError("An error occurred seeding the database, Error:{}", string.Join(",", result.Errors.Select(x => x.Description)));
                }
            }


        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred seeding the database.");
        }
    }

    private static async Task AddRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(",", result.Errors.Select(x => x.Description)));
            }
        }
    }
}