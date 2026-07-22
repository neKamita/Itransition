using System.Security.Claims;
using Itransition.Data;
using Itransition.Models.Cvs;
using Itransition.Models.Attributes;
using Itransition.Models.Profiles;
using Itransition.Services;
using Itransition.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itransition.Controllers;

[Authorize(Roles = "Candidate,Administrator")]
public class CandidateProfilesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CandidateProfilesController> _logger;

    public CandidateProfilesController(
        ApplicationDbContext context,
        ILogger<CandidateProfilesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Administrator"))
        {
            var profiles = await _context.CandidateProfiles
                .AsNoTracking()
                .Include(candidate => candidate.User)
                .Include(candidate => candidate.AttributeValues)
                    .ThenInclude(value => value.AttributeDefinition)
                .ToListAsync();
            return View(profiles
                .OrderBy(candidate => candidate.FirstName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => candidate.LastName, StringComparer.OrdinalIgnoreCase)
                .ToList());
        }

        var currentUserId = GetCurrentUserId();
        var profile = await _context.CandidateProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.UserId == currentUserId);

        return profile is null
            ? RedirectToAction(nameof(Create))
            : RedirectToAction(nameof(Details), new { id = profile.Id });
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var candidateProfile = await _context.CandidateProfiles
            .WithDetailsGraph()
            .FirstOrDefaultAsync(candidate => candidate.Id == id);

        if (candidateProfile is null)
        {
            return NotFound();
        }

        if (!CanManageProfile(candidateProfile.UserId))
        {
            LogDeniedAccess("view", candidateProfile.Id);
            return Forbid();
        }

        return View(candidateProfile);
    }

    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Create()
    {
        var currentUserId = GetCurrentUserId();
        var existingProfileId = await _context.CandidateProfiles
            .Where(candidate => candidate.UserId == currentUserId)
            .Select(candidate => (Guid?)candidate.Id)
            .FirstOrDefaultAsync();

        return existingProfileId.HasValue
            ? RedirectToAction(nameof(Details), new { id = existingProfileId.Value })
            : View();
    }

    [HttpPost]
    [Authorize(Roles = "Candidate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CandidateProfileViewModel model)
    {
        var currentUserId = GetCurrentUserId();
        if (await _context.CandidateProfiles.AnyAsync(candidate => candidate.UserId == currentUserId))
        {
            return Conflict("A candidate profile already exists for this account.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var candidateProfile = new CandidateProfile
        {
            Id = Guid.NewGuid(),
            UserId = currentUserId,
            User = null!
        };

        AddBuiltInValues(candidateProfile, model);

        _context.CandidateProfiles.Add(candidateProfile);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Candidate profile {ProfileId} created by user {UserId}",
            candidateProfile.Id,
            currentUserId);

        return RedirectToAction(nameof(Details), new { id = candidateProfile.Id });
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var candidateProfile = await _context.CandidateProfiles
            .AsNoTracking()
            .Include(candidate => candidate.AttributeValues)
                .ThenInclude(value => value.AttributeDefinition)
            .FirstOrDefaultAsync(candidate => candidate.Id == id);

        if (candidateProfile is null)
        {
            return NotFound();
        }

        if (!CanManageProfile(candidateProfile.UserId))
        {
            LogDeniedAccess("edit", candidateProfile.Id);
            return Forbid();
        }

        var viewModel = new CandidateProfileViewModel
        {
            Id = candidateProfile.Id,
            FirstName = candidateProfile.FirstName,
            LastName = candidateProfile.LastName,
            Location = candidateProfile.Location,
            PersonalPhotoUrl = candidateProfile.PersonalPhotoUrl,
            Version = candidateProfile.Version
        };
        SetBuiltInVersions(candidateProfile, viewModel);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CandidateProfileViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var profileToUpdate = await _context.CandidateProfiles
            .Include(candidate => candidate.AttributeValues)
                .ThenInclude(value => value.AttributeDefinition)
            .FirstOrDefaultAsync(candidate => candidate.Id == id);
        if (profileToUpdate is null)
        {
            return NotFound();
        }

        if (!CanManageProfile(profileToUpdate.UserId))
        {
            LogDeniedAccess("edit", profileToUpdate.Id);
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        UpsertBuiltInValue(profileToUpdate, BuiltInAttributeKeys.FirstNameId, BuiltInAttributeKeys.FirstName, model.FirstName, model.FirstNameVersion);
        UpsertBuiltInValue(profileToUpdate, BuiltInAttributeKeys.LastNameId, BuiltInAttributeKeys.LastName, model.LastName, model.LastNameVersion);
        UpsertBuiltInValue(profileToUpdate, BuiltInAttributeKeys.LocationId, BuiltInAttributeKeys.Location, model.Location, model.LocationVersion);
        UpsertBuiltInValue(profileToUpdate, BuiltInAttributeKeys.PersonalPhotoId, BuiltInAttributeKeys.PersonalPhoto, model.PersonalPhotoUrl, model.PersonalPhotoVersion);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.CandidateProfiles.AnyAsync(candidate => candidate.Id == id))
            {
                return NotFound();
            }

            _logger.LogWarning(
                "Concurrency conflict editing candidate profile {ProfileId} by user {UserId}",
                id,
                GetCurrentUserId());
            ModelState.AddModelError(string.Empty, "The profile was changed by another user. Reload the page and try again.");
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var candidateProfile = await _context.CandidateProfiles
            .AsNoTracking()
            .Include(candidate => candidate.User)
            .FirstOrDefaultAsync(candidate => candidate.Id == id);

        if (candidateProfile is null)
        {
            return NotFound();
        }

        if (!CanManageProfile(candidateProfile.UserId))
        {
            LogDeniedAccess("delete", candidateProfile.Id);
            return Forbid();
        }

        return View(candidateProfile);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, uint version)
    {
        var candidateProfile = await _context.CandidateProfiles.FindAsync(id);
        if (candidateProfile is null)
        {
            return NotFound();
        }

        if (!CanManageProfile(candidateProfile.UserId))
        {
            LogDeniedAccess("delete", candidateProfile.Id);
            return Forbid();
        }

        _context.Entry(candidateProfile).Property(candidate => candidate.Version).OriginalValue = version;
        _context.CandidateProfiles.Remove(candidateProfile);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.CandidateProfiles.AnyAsync(candidate => candidate.Id == id))
            {
                return NotFound();
            }

            TempData["ErrorMessage"] = "The profile changed in another session. Reload it before deleting.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAttribute(Guid profileId, Guid attributeDefinitionId)
    {
        if (profileId == Guid.Empty || attributeDefinitionId == Guid.Empty)
        {
            return BadRequest(new { error = "Profile and attribute are required." });
        }

        if (!await CanManageProfileAsync(profileId))
        {
            LogDeniedAccess("add attribute to", profileId);
            return Forbid();
        }

        var definition = await _context.AttributeDefinitions.FindAsync(attributeDefinitionId);
        if (definition is null)
        {
            return NotFound(new { error = "Attribute definition was not found." });
        }

        if (await _context.UserAttributeValues.AnyAsync(value =>
                value.CandidateProfileId == profileId
                && value.AttributeDefinitionId == attributeDefinitionId))
        {
            return Conflict(new { error = "This attribute is already present in the profile." });
        }

        var value = new UserAttributeValue
        {
            Id = Guid.NewGuid(),
            CandidateProfileId = profileId,
            AttributeDefinitionId = attributeDefinitionId,
            AttributeDefinition = null!,
            CandidateProfile = null!
        };

        _context.UserAttributeValues.Add(value);
        definition.LastUsedAt = DateTime.UtcNow;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "This attribute was added in another session. Reload the profile." });
        }
        return Json(new { success = true, id = value.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSelectedAttributes([FromBody] CandidateAttributeBatchRequest? request)
    {
        if (request is null
            || request.ProfileId == Guid.Empty
            || request.Items.Count is < 1 or > 100
            || request.Items.Any(item => item.Id == Guid.Empty)
            || request.Items.Select(item => item.Id).Distinct().Count() != request.Items.Count)
        {
            return BadRequest(new { error = "Select between 1 and 100 valid attributes." });
        }

        if (!await CanManageProfileAsync(request.ProfileId))
        {
            LogDeniedAccess("update selected attributes on", request.ProfileId);
            return Forbid();
        }

        var ids = request.Items.Select(item => item.Id).ToArray();
        var values = await _context.UserAttributeValues
            .Include(item => item.AttributeDefinition)
                .ThenInclude(definition => definition.Options)
            .Where(item => item.CandidateProfileId == request.ProfileId && ids.Contains(item.Id))
            .ToListAsync();

        if (values.Count != request.Items.Count)
        {
            return Conflict(new { error = "One or more selected attributes no longer exist. Reload the profile." });
        }

        var inputById = request.Items.ToDictionary(item => item.Id);
        foreach (var attributeValue in values)
        {
            var input = inputById[attributeValue.Id];
            if (!AttributeValuePolicy.TryNormalize(
                    attributeValue.AttributeDefinition,
                    input.Value,
                    out var normalizedValue,
                    out var validationError))
            {
                return BadRequest(new { error = validationError });
            }

            _context.Entry(attributeValue).Property(item => item.Version).OriginalValue = input.Version;
            attributeValue.Value = normalizedValue;
            attributeValue.AttributeDefinition.LastUsedAt = DateTime.UtcNow;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "At least one selected attribute changed. Reload the profile before saving." });
        }

        return Json(new
        {
            success = true,
            versions = values.ToDictionary(item => item.Id, item => item.Version)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSelectedAttributes([FromBody] CandidateAttributeDeleteRequest? request)
    {
        if (request is null
            || request.ProfileId == Guid.Empty
            || request.Items.Count is < 1 or > 100
            || request.Items.Any(item => item.Id == Guid.Empty)
            || request.Items.Select(item => item.Id).Distinct().Count() != request.Items.Count)
        {
            return BadRequest(new { error = "Select between 1 and 100 valid attributes." });
        }

        if (!await CanManageProfileAsync(request.ProfileId))
        {
            LogDeniedAccess("remove selected attributes from", request.ProfileId);
            return Forbid();
        }

        var ids = request.Items.Select(item => item.Id).ToArray();
        var values = await _context.UserAttributeValues
            .Include(item => item.AttributeDefinition)
            .Where(item => item.CandidateProfileId == request.ProfileId && ids.Contains(item.Id))
            .ToListAsync();

        if (values.Count != request.Items.Count)
        {
            return Conflict(new { error = "One or more selected attributes no longer exist. Reload the profile." });
        }

        if (values.Any(item => item.AttributeDefinition.IsBuiltIn))
        {
            return Conflict(new { error = "Built-in profile attributes cannot be removed." });
        }

        var inputById = request.Items.ToDictionary(item => item.Id);
        foreach (var attributeValue in values)
        {
            _context.Entry(attributeValue).Property(item => item.Version).OriginalValue = inputById[attributeValue.Id].Version;
        }

        _context.UserAttributeValues.RemoveRange(values);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "At least one selected attribute changed. Reload the profile before deleting." });
        }

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProject(
        Guid profileId,
        string name,
        string? description,
        DateTime? startDate,
        DateTime? endDate,
        string? tags)
    {
        if (!await CanManageProfileAsync(profileId))
        {
            LogDeniedAccess("add project to", profileId);
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name is required.");
        }

        if (name.Trim().Length > 200 || description?.Length > 10_000)
        {
            return BadRequest("Project name cannot exceed 200 characters and description cannot exceed 10,000 characters.");
        }

        if (startDate.HasValue && endDate.HasValue && endDate < startDate)
        {
            return BadRequest("End date cannot be earlier than start date.");
        }

        var projectTags = ParseTags(tags);
        if (projectTags.Count > 30 || projectTags.Any(tag => tag.Length > 50))
        {
            return BadRequest("Use at most 30 tags, each no longer than 50 characters.");
        }

        var project = new ProjectProfile
        {
            Id = Guid.NewGuid(),
            CandidateProfileId = profileId,
            CandidateProfile = null!,
            Name = name.Trim(),
            Description = description,
            StartDate = ToUtcDate(startDate),
            EndDate = ToUtcDate(endDate)
        };

        foreach (var tag in projectTags)
        {
            project.TechnologyTags.Add(new ProjectTechnologyTag
            {
                Id = Guid.NewGuid(),
                ProjectProfileId = project.Id,
                ProjectProfile = null!,
                TagName = tag
            });
        }

        _context.ProjectProfiles.Add(project);
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProject(
        Guid id,
        uint version,
        string name,
        string? description,
        DateTime? startDate,
        DateTime? endDate,
        string? tags)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name is required.");
        }

        if (name.Trim().Length > 200 || description?.Length > 10_000)
        {
            return BadRequest("Project name cannot exceed 200 characters and description cannot exceed 10,000 characters.");
        }

        if (startDate.HasValue && endDate.HasValue && endDate < startDate)
        {
            return BadRequest("End date cannot be earlier than start date.");
        }

        var projectTags = ParseTags(tags);
        if (projectTags.Count > 30 || projectTags.Any(tag => tag.Length > 50))
        {
            return BadRequest("Use at most 30 tags, each no longer than 50 characters.");
        }

        var project = await _context.ProjectProfiles
            .Include(item => item.CandidateProfile)
            .Include(item => item.TechnologyTags)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (project is null)
        {
            return NotFound("Project not found.");
        }

        if (!CanManageProfile(project.CandidateProfile.UserId))
        {
            LogDeniedAccess("edit project on", project.CandidateProfileId);
            return Forbid();
        }

        project.Name = name.Trim();
        project.Description = description;
        project.StartDate = ToUtcDate(startDate);
        project.EndDate = ToUtcDate(endDate);
        _context.Entry(project).Property(item => item.Version).OriginalValue = version;

        _context.ProjectTechnologyTags.RemoveRange(project.TechnologyTags);
        project.TechnologyTags.Clear();

        foreach (var tag in projectTags)
        {
            project.TechnologyTags.Add(new ProjectTechnologyTag
            {
                Id = Guid.NewGuid(),
                ProjectProfileId = project.Id,
                ProjectProfile = project,
                TagName = tag
            });
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "This project was changed in another session. Reload the profile before saving." });
        }
        return Json(new { success = true, version = project.Version });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProject(Guid id, uint version)
    {
        var project = await _context.ProjectProfiles
            .Include(item => item.CandidateProfile)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (project is null)
        {
            return NotFound();
        }

        if (!CanManageProfile(project.CandidateProfile.UserId))
        {
            LogDeniedAccess("delete project on", project.CandidateProfileId);
            return Forbid();
        }

        _context.Entry(project).Property(item => item.Version).OriginalValue = version;
        _context.ProjectProfiles.Remove(project);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "This project changed before deletion. Reload the profile and try again." });
        }
        return Json(new { success = true });
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The authenticated user has no identifier claim.");
    }

    private bool CanManageProfile(string ownerUserId)
    {
        return User.IsInRole("Administrator")
            || string.Equals(ownerUserId, GetCurrentUserId(), StringComparison.Ordinal);
    }

    private async Task<bool> CanManageProfileAsync(Guid profileId)
    {
        if (User.IsInRole("Administrator"))
        {
            return await _context.CandidateProfiles.AnyAsync(candidate => candidate.Id == profileId);
        }

        var currentUserId = GetCurrentUserId();
        return await _context.CandidateProfiles.AnyAsync(candidate =>
            candidate.Id == profileId && candidate.UserId == currentUserId);
    }

    private void LogDeniedAccess(string operation, Guid profileId)
    {
        _logger.LogWarning(
            "User {UserId} was denied permission to {Operation} candidate profile {ProfileId}",
            GetCurrentUserId(),
            operation,
            profileId);
    }

    private static IReadOnlyCollection<string> ParseTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return Array.Empty<string>();
        }

        return tags
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static DateTime? ToUtcDate(DateTime? value)
    {
        return value.HasValue
            ? DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc)
            : null;
    }

    private static void AddBuiltInValues(
        CandidateProfile profile,
        CandidateProfileViewModel model)
    {
        profile.AttributeValues.Add(NewBuiltInValue(profile, BuiltInAttributeKeys.FirstNameId, model.FirstName));
        profile.AttributeValues.Add(NewBuiltInValue(profile, BuiltInAttributeKeys.LastNameId, model.LastName));
        profile.AttributeValues.Add(NewBuiltInValue(profile, BuiltInAttributeKeys.LocationId, model.Location));
        profile.AttributeValues.Add(NewBuiltInValue(profile, BuiltInAttributeKeys.PersonalPhotoId, model.PersonalPhotoUrl));
    }

    private static UserAttributeValue NewBuiltInValue(
        CandidateProfile profile,
        Guid attributeDefinitionId,
        string? value)
    {
        return new UserAttributeValue
        {
            Id = Guid.NewGuid(),
            CandidateProfileId = profile.Id,
            CandidateProfile = profile,
            AttributeDefinitionId = attributeDefinitionId,
            AttributeDefinition = null!,
            Value = string.IsNullOrWhiteSpace(value) ? null : value.Trim()
        };
    }

    private void UpsertBuiltInValue(
        CandidateProfile profile,
        Guid attributeDefinitionId,
        string key,
        string? value,
        uint version)
    {
        var attributeValue = profile.FindBuiltInValue(key);
        if (attributeValue is null)
        {
            profile.AttributeValues.Add(NewBuiltInValue(profile, attributeDefinitionId, value));
            return;
        }

        _context.Entry(attributeValue).Property(item => item.Version).OriginalValue = version;
        attributeValue.Value = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void SetBuiltInVersions(
        CandidateProfile profile,
        CandidateProfileViewModel model)
    {
        model.FirstNameVersion = profile.FindBuiltInValue(BuiltInAttributeKeys.FirstName)?.Version ?? 0;
        model.LastNameVersion = profile.FindBuiltInValue(BuiltInAttributeKeys.LastName)?.Version ?? 0;
        model.LocationVersion = profile.FindBuiltInValue(BuiltInAttributeKeys.Location)?.Version ?? 0;
        model.PersonalPhotoVersion = profile.FindBuiltInValue(BuiltInAttributeKeys.PersonalPhoto)?.Version ?? 0;
    }
}
