using Itransition.Data;
using Itransition.Models;
using Itransition.Models.Attributes;
using Itransition.Models.Cvs;
using Itransition.Models.Positions;
using Itransition.Models.Profiles;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Itransition.Services;

public static class SeedService
{
    public static async Task SeedDatabaseAsync(
        IServiceProvider serviceProvider,
        IHostEnvironment environment)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedServiceMarker>>();

        try
        {
            logger.LogInformation("Applying pending database migrations.");
            await context.Database.MigrateAsync();

            await EnsureRoleAsync(roleManager, "Administrator");
            await EnsureRoleAsync(roleManager, "Recruiter");
            await EnsureRoleAsync(roleManager, "Candidate");

            await EnsureConfiguredUserAsync(
                userManager,
                configuration,
                environment,
                logger,
                "AdminConfiguration",
                "Administrator",
                "Administrator");
            await EnsureConfiguredUserAsync(
                userManager,
                configuration,
                environment,
                logger,
                "RecruiterConfiguration",
                "Recruiter",
                "Recruiter");

            var demoSeedEnabled = environment.IsDevelopment()
                && configuration.GetValue<bool>("SeedData:Enabled");
            if (!demoSeedEnabled)
            {
                logger.LogInformation("Demo data seed is disabled.");
                return;
            }

            logger.LogInformation("Seeding explicitly enabled development data.");
            await SeedDemoDomainDataAsync(context);
            await SeedDemoUsersAsync(context, userManager, configuration, logger);
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "Database migration or required seed failed. Application startup is stopping.");
            throw;
        }
    }

    private static async Task EnsureConfiguredUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger,
        string configurationSection,
        string roleName,
        string defaultFullName)
    {
        var email = configuration[$"{configurationSection}:Email"]?.Trim();
        var password = configuration[$"{configurationSection}:Password"];

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("{RoleName} bootstrap seed is not configured.", roleName);
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "{RoleName} bootstrap seed was skipped because both Email and Password must be configured.",
                roleName);
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                FullName = configuration[$"{configurationSection}:FullName"]?.Trim() ?? defaultFullName,
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded && environment.IsDevelopment())
            {
                logger.LogError(
                    "Configured {RoleName} was not created because its configuration is invalid: {Errors}",
                    roleName,
                    string.Join("; ", createResult.Errors.Select(error => error.Description)));
                return;
            }

            EnsureIdentitySucceeded(createResult, $"create the configured {roleName}");
            logger.LogInformation("Configured {RoleName} account was created.", roleName);
        }
        else if (configuration.GetValue<bool>($"{configurationSection}:ResetPassword"))
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, resetToken, password);
            EnsureIdentitySucceeded(resetResult, $"reset the configured {roleName} password");
            logger.LogWarning(
                "Configured {RoleName} bootstrap password was reset. Disable ResetPassword immediately.",
                roleName);
        }

        await EnsureUserRoleAsync(userManager, user, roleName);
    }

    private static async Task SeedDemoDomainDataAsync(ApplicationDbContext context)
    {
        var definitionsByName = await context.AttributeDefinitions
            .Include(attribute => attribute.Options)
            .ToDictionaryAsync(attribute => attribute.Name, StringComparer.OrdinalIgnoreCase);

        if (!definitionsByName.ContainsKey("English Level"))
        {
            var englishAttribute = new AttributeDefinition
            {
                Id = Guid.NewGuid(),
                Name = "English Level",
                CategoryId = AttributeCategoryIds.Language,
                Category = null!,
                DataType = AttributeDataType.Dropdown,
                Options =
                [
                    new AttributeOption { Id = Guid.NewGuid(), Value = "A1" },
                    new AttributeOption { Id = Guid.NewGuid(), Value = "A2" },
                    new AttributeOption { Id = Guid.NewGuid(), Value = "B1" },
                    new AttributeOption { Id = Guid.NewGuid(), Value = "B2" },
                    new AttributeOption { Id = Guid.NewGuid(), Value = "C1" },
                    new AttributeOption { Id = Guid.NewGuid(), Value = "C2" }
                ]
            };
            context.AttributeDefinitions.Add(englishAttribute);
            definitionsByName.Add(englishAttribute.Name, englishAttribute);
        }

        if (!definitionsByName.ContainsKey("Years of Experience"))
        {
            var experienceAttribute = new AttributeDefinition
            {
                Id = Guid.NewGuid(),
                Name = "Years of Experience",
                CategoryId = AttributeCategoryIds.Professional,
                Category = null!,
                DataType = AttributeDataType.Numeric
            };
            context.AttributeDefinitions.Add(experienceAttribute);
            definitionsByName.Add(experienceAttribute.Name, experienceAttribute);
        }

        if (!definitionsByName.ContainsKey("Primary Language"))
        {
            var skillAttribute = new AttributeDefinition
            {
                Id = Guid.NewGuid(),
                Name = "Primary Language",
                CategoryId = AttributeCategoryIds.Technical,
                Category = null!,
                DataType = AttributeDataType.Dropdown,
                Options =
                [
                    new AttributeOption { Id = Guid.NewGuid(), Value = "C#" },
                    new AttributeOption { Id = Guid.NewGuid(), Value = "Java" },
                    new AttributeOption { Id = Guid.NewGuid(), Value = "JavaScript" }
                ]
            };
            context.AttributeDefinitions.Add(skillAttribute);
            definitionsByName.Add(skillAttribute.Name, skillAttribute);
        }

        await context.SaveChangesAsync();

        var english = definitionsByName["English Level"];
        var primaryLanguage = definitionsByName["Primary Language"];
        var existingPositionKeys = await context.Positions
            .AsNoTracking()
            .Select(position => position.Company + "\n" + position.Title)
            .ToListAsync();
        var positionKeys = existingPositionKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!positionKeys.Contains("Tech Corp\nSenior C# Developer"))
        {
            var publicPosition = new Position
            {
                Id = Guid.NewGuid(),
                Title = "Senior C# Developer",
                Company = "Tech Corp",
                Level = "Senior",
                IsPublic = true,
                MaxProjectInCv = 5,
                Tags = "C#, .NET, Azure, SQL",
                Description = "Looking for an experienced C# developer to lead our backend team."
            };
            publicPosition.PositionRequiredAttributes.Add(new PositionAttribute
            {
                Id = Guid.NewGuid(),
                PositionId = publicPosition.Id,
                Position = publicPosition,
                AttributeDefinitionId = english.Id,
                AttributeDefinition = null!
            });
            publicPosition.PositionRequiredAttributes.Add(new PositionAttribute
            {
                Id = Guid.NewGuid(),
                PositionId = publicPosition.Id,
                Position = publicPosition,
                AttributeDefinitionId = primaryLanguage.Id,
                AttributeDefinition = null!
            });
            context.Positions.Add(publicPosition);
        }

        if (!positionKeys.Contains("WebStudio\nReact Frontend Engineer"))
        {
            var restrictedPosition = new Position
            {
                Id = Guid.NewGuid(),
                Title = "React Frontend Engineer",
                Company = "WebStudio",
                Level = "Middle",
                IsPublic = false,
                MaxProjectInCv = 3,
                Tags = "React, TypeScript, Redux, CSS",
                Description = "Join us to build accessible interfaces using React."
            };
            restrictedPosition.PositionAccessRules.Add(new PositionAccessRule
            {
                Id = Guid.NewGuid(),
                PositionId = restrictedPosition.Id,
                Position = restrictedPosition,
                AttributeDefinitionId = english.Id,
                AttributeDefinition = null!,
                Operator = "==",
                TargetValue = "B2"
            });
            context.Positions.Add(restrictedPosition);
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDemoUsersAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var demoPassword = configuration["SeedData:DemoPassword"];
        if (string.IsNullOrWhiteSpace(demoPassword)
            || string.Equals(demoPassword, "CHANGE_ME", StringComparison.Ordinal))
        {
            logger.LogWarning("Demo users were skipped because SeedData:DemoPassword is not configured securely.");
            return;
        }

        await EnsureDemoUserAsync(
            userManager,
            "recruiter@test.local",
            "Test Recruiter",
            demoPassword,
            "Recruiter");

        var candidate = await EnsureDemoUserAsync(
            userManager,
            "candidate@test.local",
            "Test Candidate",
            demoPassword,
            "Candidate");

        if (await context.CandidateProfiles.AnyAsync(profile => profile.UserId == candidate.Id))
        {
            return;
        }

        var profile = new CandidateProfile
        {
            Id = Guid.NewGuid(),
            UserId = candidate.Id,
            User = candidate
        };

        profile.AttributeValues.AddRange(
        [
            NewBuiltInValue(profile, BuiltInAttributeKeys.FirstNameId, "Alice"),
            NewBuiltInValue(profile, BuiltInAttributeKeys.LastNameId, "Candidate"),
            NewBuiltInValue(profile, BuiltInAttributeKeys.LocationId, "London, UK"),
            NewBuiltInValue(profile, BuiltInAttributeKeys.PersonalPhotoId, null)
        ]);

        profile.Projects.Add(new ProjectProfile
        {
            Id = Guid.NewGuid(),
            CandidateProfileId = profile.Id,
            Name = "Enterprise ERP System",
            Description = "Built a scalable ERP using C# and .NET.",
            StartDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            CandidateProfile = profile,
            TechnologyTags =
            [
                NewProjectTag("C#"),
                NewProjectTag(".NET"),
                NewProjectTag("SQL")
            ]
        });

        profile.Projects.Add(new ProjectProfile
        {
            Id = Guid.NewGuid(),
            CandidateProfileId = profile.Id,
            Name = "E-Commerce Dashboard",
            Description = "Frontend dashboard for merchants.",
            StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CandidateProfile = profile,
            TechnologyTags =
            [
                NewProjectTag("React"),
                NewProjectTag("TypeScript")
            ]
        });

        context.CandidateProfiles.Add(profile);
        await context.SaveChangesAsync();
    }

    private static ProjectTechnologyTag NewProjectTag(string name)
    {
        return new ProjectTechnologyTag
        {
            Id = Guid.NewGuid(),
            TagName = name,
            ProjectProfile = null!
        };
    }

    private static UserAttributeValue NewBuiltInValue(
        CandidateProfile profile,
        Guid definitionId,
        string? value)
    {
        return new UserAttributeValue
        {
            Id = Guid.NewGuid(),
            CandidateProfileId = profile.Id,
            CandidateProfile = profile,
            AttributeDefinitionId = definitionId,
            AttributeDefinition = null!,
            Value = value
        };
    }

    private static async Task<ApplicationUser> EnsureDemoUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string password,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                FullName = fullName,
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            EnsureIdentitySucceeded(createResult, $"create demo user {email}");
        }

        await EnsureUserRoleAsync(userManager, user, role);
        return user;
    }

    private static async Task EnsureRoleAsync(
        RoleManager<IdentityRole> roleManager,
        string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await roleManager.CreateAsync(new IdentityRole(roleName));
        EnsureIdentitySucceeded(result, $"create role {roleName}");
    }

    private static async Task EnsureUserRoleAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string role)
    {
        if (await userManager.IsInRoleAsync(user, role))
        {
            return;
        }

        var result = await userManager.AddToRoleAsync(user, role);
        EnsureIdentitySucceeded(result, $"assign role {role}");
    }

    private static void EnsureIdentitySucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Failed to {operation}: {string.Join(", ", result.Errors.Select(error => error.Description))}");
    }

    private sealed class SeedServiceMarker;
}
