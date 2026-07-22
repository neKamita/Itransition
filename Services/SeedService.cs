using Itransition.Data;
using Itransition.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
            await context.Database.MigrateAsync();

            logger.LogInformation("Ensuring Roles are created.");
            await AddRoleAsync(roleManager,"Administrator");
            await AddRoleAsync(roleManager,"Recruiter");
            await AddRoleAsync(roleManager, "Candidate");

            logger.LogInformation("Ensuring Admin is created.");
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var adminEmail = config["AdminConfiguration:Email"];
            var adminPassword = config["AdminConfiguration:Password"];

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
                    await userManager.AddToRoleAsync(AdminUser, "Administrator");
                }
                else
                {
                    logger.LogError("An error occurred seeding the database, Error:{}", string.Join(",", result.Errors.Select(x => x.Description)));
                }
            }

            logger.LogInformation("Generating Fake Data...");

            if (await userManager.FindByEmailAsync("recruiter@test.com") == null)
            {
                var recruiter = new ApplicationUser { FullName = "Test Recruiter", UserName = "recruiter@test.com", Email = "recruiter@test.com", EmailConfirmed = true, NormalizedEmail = "RECRUITER@TEST.COM", NormalizedUserName = "RECRUITER@TEST.COM" };
                await userManager.CreateAsync(recruiter, "Test1234!");
                await userManager.AddToRoleAsync(recruiter, "Recruiter");
            }
            if (await userManager.FindByEmailAsync("candidate@test.com") == null)
            {
                var candidate = new ApplicationUser { FullName = "Test Candidate", UserName = "candidate@test.com", Email = "candidate@test.com", EmailConfirmed = true, NormalizedEmail = "CANDIDATE@TEST.COM", NormalizedUserName = "CANDIDATE@TEST.COM" };
                await userManager.CreateAsync(candidate, "Test1234!");
                await userManager.AddToRoleAsync(candidate, "Candidate");
            }

            if (!context.AttributeDefinitions.Any())
            {
                var englishAttr = new Itransition.Models.Attributes.AttributeDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = "English Level",
                    Category = "Language",
                    DataType = Itransition.Models.Attributes.AttributeDataType.Dropdown,
                    Options = new List<Itransition.Models.Attributes.AttributeOption>
                    {
                        new Itransition.Models.Attributes.AttributeOption { Id = Guid.NewGuid(), Value = "A1" },
                        new Itransition.Models.Attributes.AttributeOption { Id = Guid.NewGuid(), Value = "A2" },
                        new Itransition.Models.Attributes.AttributeOption { Id = Guid.NewGuid(), Value = "B1" },
                        new Itransition.Models.Attributes.AttributeOption { Id = Guid.NewGuid(), Value = "B2" },
                        new Itransition.Models.Attributes.AttributeOption { Id = Guid.NewGuid(), Value = "C1" },
                        new Itransition.Models.Attributes.AttributeOption { Id = Guid.NewGuid(), Value = "C2" }
                    }
                };

                var expAttr = new Itransition.Models.Attributes.AttributeDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = "Years of Experience",
                    Category = "Professional",
                    DataType = Itransition.Models.Attributes.AttributeDataType.String
                };

                var skillAttr = new Itransition.Models.Attributes.AttributeDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = "Primary Language",
                    Category = "Technical",
                    DataType = Itransition.Models.Attributes.AttributeDataType.Dropdown,
                    Options = new List<Itransition.Models.Attributes.AttributeOption>
                    {
                        new Itransition.Models.Attributes.AttributeOption { Id = Guid.NewGuid(), Value = "C#" },
                        new Itransition.Models.Attributes.AttributeOption { Id = Guid.NewGuid(), Value = "Java" },
                        new Itransition.Models.Attributes.AttributeOption { Id = Guid.NewGuid(), Value = "JavaScript" }
                    }
                };

                context.AttributeDefinitions.AddRange(englishAttr, expAttr, skillAttr);
                await context.SaveChangesAsync();
            }
            if (!context.Positions.Any())
            {
                var attrs = context.AttributeDefinitions.ToList();
                var engAttr = attrs.FirstOrDefault(a => a.Name == "English Level");
                var skillAttr = attrs.FirstOrDefault(a => a.Name == "Primary Language");

                var pos1 = new Itransition.Models.Positions.Position
                {
                    Id = Guid.NewGuid(),
                    Title = "Senior C# Developer",
                    Company = "Tech Corp",
                    Level = "Senior",
                    IsPublic = true,
                    MaxProjectInCv = 5,
                    Tags = "C#, .NET, Azure, SQL",
                    Description = "Looking for an experienced C# developer to lead our backend team.",
                    PositionRequiredAttributes = new List<Itransition.Models.Positions.PositionAttribute>()
                };

                if (engAttr != null)
                {
                    pos1.PositionRequiredAttributes.Add(new Itransition.Models.Positions.PositionAttribute
                    {
                        Id = Guid.NewGuid(),
                        PositionId = pos1.Id,
                        Position = null!,
                        AttributeDefinitionId = engAttr.Id,
                        AttributeDefinition = null!
                    });
                }

                if (skillAttr != null)
                {
                    pos1.PositionRequiredAttributes.Add(new Itransition.Models.Positions.PositionAttribute
                    {
                        Id = Guid.NewGuid(),
                        PositionId = pos1.Id,
                        Position = null!,
                        AttributeDefinitionId = skillAttr.Id,
                        AttributeDefinition = null!
                    });
                }

                var pos2 = new Itransition.Models.Positions.Position
                {
                    Id = Guid.NewGuid(),
                    Title = "React Frontend Engineer",
                    Company = "WebStudio",
                    Level = "Middle",
                    IsPublic = false,
                    MaxProjectInCv = 3,
                    Tags = "React, TypeScript, Redux, CSS",
                    Description = "Join us to build amazing UIs using React.",
                    PositionAccessRules = new List<Itransition.Models.Positions.PositionAccessRule>()
                };

                if (engAttr != null)
                {
                    pos2.PositionAccessRules.Add(new Itransition.Models.Positions.PositionAccessRule
                    {
                        Id = Guid.NewGuid(),
                        PositionId = pos2.Id,
                        Position = null!,
                        AttributeDefinitionId = engAttr.Id,
                        AttributeDefinition = null!,
                        Operator = "CONTAINS",
                        TargetValue = "B2"
                    });
                }

                context.Positions.AddRange(pos1, pos2);
                await context.SaveChangesAsync();
            }

            var candUser = await userManager.FindByEmailAsync("candidate@test.com");
            if (candUser != null && !context.CandidateProfiles.Any(c => c.UserId == candUser.Id))
            {
                var profile = new Itransition.Models.Profiles.CandidateProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = candUser.Id,
                    FirstName = "Alice",
                    LastName = "Candidate",
                    Location = "London, UK",
                    User = null!
                };

                context.CandidateProfiles.Add(profile);
                await context.SaveChangesAsync();

                var proj1 = new Itransition.Models.Profiles.ProjectProfile
                {
                    Id = Guid.NewGuid(),
                    CandidateProfileId = profile.Id,
                    Name = "Enterprise ERP System",
                    Description = "Built a scalable ERP using C# and .NET 8.",
                    StartDate = new DateTime(2022, 1, 1),
                    EndDate = new DateTime(2023, 12, 31),
                    CandidateProfile = null!,
                    TechnologyTags = new List<Itransition.Models.Profiles.ProjectTechnologyTag>
                    {
                        new Itransition.Models.Profiles.ProjectTechnologyTag { Id = Guid.NewGuid(), TagName = "C#", ProjectProfile = null! },
                        new Itransition.Models.Profiles.ProjectTechnologyTag { Id = Guid.NewGuid(), TagName = ".NET", ProjectProfile = null! },
                        new Itransition.Models.Profiles.ProjectTechnologyTag { Id = Guid.NewGuid(), TagName = "SQL", ProjectProfile = null! }
                    }
                };

                var proj2 = new Itransition.Models.Profiles.ProjectProfile
                {
                    Id = Guid.NewGuid(),
                    CandidateProfileId = profile.Id,
                    Name = "E-Commerce Dashboard",
                    Description = "Frontend dashboard for merchants.",
                    StartDate = new DateTime(2024, 1, 1),
                    CandidateProfile = null!,
                    TechnologyTags = new List<Itransition.Models.Profiles.ProjectTechnologyTag>
                    {
                        new Itransition.Models.Profiles.ProjectTechnologyTag { Id = Guid.NewGuid(), TagName = "React", ProjectProfile = null! },
                        new Itransition.Models.Profiles.ProjectTechnologyTag { Id = Guid.NewGuid(), TagName = "TypeScript", ProjectProfile = null! }
                    }
                };

                context.ProjectProfiles.AddRange(proj1, proj2);
                await context.SaveChangesAsync();
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
