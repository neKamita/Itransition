using System.Security.Claims;
using Itransition.Data;
using Itransition.Models.Cvs;
using Itransition.Services;
using Itransition.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Itransition.Controllers;

[Authorize(Roles = "Candidate,Recruiter,Administrator")]
public class CvsController : Controller
{
    private static readonly HashSet<string> EditableStatuses =
        new(StringComparer.OrdinalIgnoreCase) { CvStatuses.New, CvStatuses.Draft };

    private readonly ApplicationDbContext _context;
    private readonly PositionAccessService _positionAccessService;
    private readonly ILogger<CvsController> _logger;

    public CvsController(
        ApplicationDbContext context,
        PositionAccessService positionAccessService,
        ILogger<CvsController> logger)
    {
        _context = context;
        _positionAccessService = positionAccessService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var roles = RoleAccessContext.FromPrincipal(User);
        var cvs = await BuildCvQuery()
            .AsNoTracking()
            .ToListAsync();

        if (roles.IsAdministrator)
        {
            return View(cvs);
        }

        var visibleCvs = cvs
            .Where(cv => RoleAccessPolicy.CanSeeCvInList(
                roles,
                cv.CandidateProfile.UserId,
                cv.Status))
            .Where(CandidateStillHasAccess)
            .ToList();

        return View(visibleCvs);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var cv = await BuildCvQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (cv is null)
        {
            return NotFound();
        }

        if (!CanViewCv(cv))
        {
            LogDeniedAccess("view", cv.Id);
            return Forbid();
        }

        var currentUserId = GetCurrentUserId();
        ViewData["CurrentUserLiked"] = cv.Likes.Any(like => like.RecruiterId == currentUserId);
        ViewData["CanManageCv"] = CanManageCv(cv);
        ViewData["MissingPublicationFields"] = CvPublicationPolicy.GetMissingFields(cv);
        ViewData["RelevantProjects"] = CvProjectSelector.SelectRelevantProjects(
            cv.Position,
            cv.CandidateProfile.Projects);

        return View(cv);
    }

    [Authorize(Roles = "Candidate,Administrator")]
    public async Task<IActionResult> Create(Guid? positionId = null)
    {
        await PopulateCreateListsAsync(positionId, null);
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Candidate,Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid positionId, Guid? candidateProfileId)
    {
        var positionExists = await _context.Positions.AnyAsync(position => position.Id == positionId);
        if (!positionExists)
        {
            return NotFound("Position was not found.");
        }

        Guid finalCandidateId;
        if (User.IsInRole("Administrator"))
        {
            if (!candidateProfileId.HasValue
                || !await _context.CandidateProfiles.AnyAsync(candidate => candidate.Id == candidateProfileId.Value))
            {
                ModelState.AddModelError(nameof(candidateProfileId), "Please select a valid candidate.");
                await PopulateCreateListsAsync(positionId, candidateProfileId);
                return View();
            }

            finalCandidateId = candidateProfileId.Value;
        }
        else
        {
            var currentUserId = GetCurrentUserId();
            var profileId = await _context.CandidateProfiles
                .Where(candidate => candidate.UserId == currentUserId)
                .Select(candidate => (Guid?)candidate.Id)
                .FirstOrDefaultAsync();

            if (!profileId.HasValue)
            {
                return BadRequest("Create a candidate profile before creating a CV.");
            }

            finalCandidateId = profileId.Value;
            if (!await _positionAccessService.CanCandidateAccessAsync(positionId, finalCandidateId))
            {
                _logger.LogWarning(
                    "Candidate {UserId} attempted to create a CV for inaccessible position {PositionId}",
                    currentUserId,
                    positionId);
                return Forbid();
            }
        }

        var existingCvId = await _context.Cvs
            .Where(cv => cv.CandidateProfileId == finalCandidateId && cv.PositionId == positionId)
            .Select(cv => (Guid?)cv.Id)
            .FirstOrDefaultAsync();

        if (existingCvId.HasValue)
        {
            _logger.LogInformation(
                "CV creation skipped because CV {CvId} already exists for candidate profile {CandidateProfileId} and position {PositionId}",
                existingCvId.Value,
                finalCandidateId,
                positionId);

            TempData["CvNotice"] = "A CV for this candidate and position already exists. The existing CV was opened instead.";
            return RedirectToAction(nameof(Details), new { id = existingCvId.Value });
        }

        var cv = new Cv
        {
            Id = Guid.NewGuid(),
            CandidateProfileId = finalCandidateId,
            PositionId = positionId,
            Status = CvStatuses.New,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            CandidateProfile = null!,
            Position = null!
        };

        _context.Cvs.Add(cv);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            var concurrentCvId = await _context.Cvs
                .AsNoTracking()
                .Where(item => item.CandidateProfileId == finalCandidateId && item.PositionId == positionId)
                .Select(item => (Guid?)item.Id)
                .FirstOrDefaultAsync();

            if (concurrentCvId.HasValue)
            {
                TempData["CvNotice"] = "A CV for this candidate and position was created in another session. The existing CV was opened instead.";
                return RedirectToAction(nameof(Details), new { id = concurrentCvId.Value });
            }

            throw;
        }

        _logger.LogInformation(
            "CV {CvId} created for candidate profile {CandidateProfileId} and position {PositionId}",
            cv.Id,
            cv.CandidateProfileId,
            cv.PositionId);

        return RedirectToAction(nameof(Details), new { id = cv.Id });
    }

    [Authorize(Roles = "Candidate,Administrator")]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var cv = await BuildCvQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (cv is null)
        {
            return NotFound();
        }

        if (!CanManageCv(cv))
        {
            LogDeniedAccess("edit", cv.Id);
            return Forbid();
        }

        if (!CvStatuses.IsEditable(cv.Status))
        {
            TempData["CvNotice"] = "Unpublish this CV before editing its draft state.";
            return RedirectToAction(nameof(Details), new { id = cv.Id });
        }

        return View(new CvEditViewModel
        {
            Id = cv.Id,
            Status = cv.Status,
            CandidateName = $"{cv.CandidateProfile.FirstName} {cv.CandidateProfile.LastName}".Trim(),
            PositionTitle = cv.Position.Title,
            Version = cv.Version
        });
    }

    [HttpPost]
    [Authorize(Roles = "Candidate,Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CvEditViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var cv = await BuildCvQuery().FirstOrDefaultAsync(item => item.Id == id);
        if (cv is null)
        {
            return NotFound();
        }

        if (!CanManageCv(cv))
        {
            LogDeniedAccess("edit", cv.Id);
            return Forbid();
        }

        if (!CvStatuses.IsEditable(cv.Status))
        {
            TempData["CvNotice"] = "Unpublish this CV before editing its draft state.";
            return RedirectToAction(nameof(Details), new { id = cv.Id });
        }

        if (!EditableStatuses.Contains(model.Status))
        {
            ModelState.AddModelError(nameof(model.Status), "Publishing is available only through the Publish action.");
        }

        if (!ModelState.IsValid)
        {
            model.CandidateName = $"{cv.CandidateProfile.FirstName} {cv.CandidateProfile.LastName}".Trim();
            model.PositionTitle = cv.Position.Title;
            return View(model);
        }

        cv.Status = model.Status;
        cv.UpdatedDate = DateTime.UtcNow;

        _context.Entry(cv).Property(item => item.Version).OriginalValue = model.Version;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Cvs.AnyAsync(item => item.Id == id))
            {
                return NotFound();
            }

            _logger.LogWarning(
                "Concurrency conflict editing CV {CvId} by user {UserId}",
                id,
                GetCurrentUserId());
            ModelState.AddModelError(string.Empty, "The CV was changed by another user. Reload the page and try again.");
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Candidate,Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAttributeValue(
        Guid cvId,
        Guid attributeDefinitionId,
        string? value,
        uint? version,
        uint cvVersion)
    {
        var cv = await BuildCvQuery().FirstOrDefaultAsync(item => item.Id == cvId);
        if (cv is null)
        {
            return NotFound(new { error = "CV was not found." });
        }

        if (!CanManageCv(cv))
        {
            LogDeniedAccess("update an attribute on", cv.Id);
            return Forbid();
        }

        if (!CvStatuses.IsEditable(cv.Status))
        {
            return Conflict(new { error = "Unpublish this CV before editing its attributes." });
        }

        var requirement = cv.Position.PositionRequiredAttributes
            .FirstOrDefault(item => item.AttributeDefinitionId == attributeDefinitionId);
        if (requirement is null)
        {
            return BadRequest(new { error = "This attribute is not part of the position template." });
        }

        if (!AttributeValuePolicy.TryNormalize(
                requirement.AttributeDefinition,
                value,
                out var normalizedValue,
                out var validationError))
        {
            return BadRequest(new { error = validationError });
        }

        var attributeValue = cv.CandidateProfile.AttributeValues
            .FirstOrDefault(item => item.AttributeDefinitionId == attributeDefinitionId);

        if (attributeValue is null)
        {
            if (version.HasValue)
            {
                return Conflict(new { error = "This attribute was removed in another session. Reload the CV and try again." });
            }

            attributeValue = new UserAttributeValue
            {
                Id = Guid.NewGuid(),
                CandidateProfileId = cv.CandidateProfileId,
                CandidateProfile = cv.CandidateProfile,
                AttributeDefinitionId = attributeDefinitionId,
                AttributeDefinition = requirement.AttributeDefinition,
                Value = normalizedValue
            };
            _context.UserAttributeValues.Add(attributeValue);
        }
        else
        {
            if (!version.HasValue)
            {
                return Conflict(new { error = "This attribute was added in another session. Reload the CV and try again." });
            }

            _context.Entry(attributeValue).Property(item => item.Version).OriginalValue = version.Value;
            attributeValue.Value = normalizedValue;
        }

        _context.Entry(cv).Property(item => item.Version).OriginalValue = cvVersion;
        cv.UpdatedDate = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict updating attribute {AttributeId} through CV {CvId} by user {UserId}",
                attributeDefinitionId,
                cvId,
                GetCurrentUserId());
            return Conflict(new
            {
                error = "The CV or attribute changed in another session. Reload the page before saving again."
            });
        }
        catch (DbUpdateException)
        {
            _logger.LogWarning(
                "Database conflict updating attribute {AttributeId} through CV {CvId} by user {UserId}",
                attributeDefinitionId,
                cvId,
                GetCurrentUserId());
            return Conflict(new
            {
                error = "The attribute changed while it was being saved. Reload the page and try again."
            });
        }

        return Json(new
        {
            success = true,
            version = attributeValue.Version,
            cvVersion = cv.Version,
            value = attributeValue.Value
        });
    }

    [HttpPost]
    [Authorize(Roles = "Candidate,Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id, uint version)
    {
        var cv = await BuildCvQuery().FirstOrDefaultAsync(item => item.Id == id);
        if (cv is null)
        {
            return NotFound();
        }

        if (!CanManageCv(cv))
        {
            LogDeniedAccess("publish", cv.Id);
            return Forbid();
        }

        if (string.Equals(cv.Status, CvStatuses.Published, StringComparison.OrdinalIgnoreCase))
        {
            TempData["CvNotice"] = "This CV is already published and visible to recruiters.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!CvStatuses.IsEditable(cv.Status))
        {
            _logger.LogWarning(
                "User {UserId} attempted to publish CV {CvId} from unsupported status {Status}",
                GetCurrentUserId(),
                cv.Id,
                cv.Status);
            return Conflict("The CV cannot be published from its current status.");
        }

        var missingFields = CvPublicationPolicy.GetMissingFields(cv);
        if (missingFields.Count > 0)
        {
            TempData["CvPublishError"] =
                $"Complete the following information before publishing: {string.Join(", ", missingFields)}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        cv.Status = CvStatuses.Published;
        cv.UpdatedDate = DateTime.UtcNow;
        _context.Entry(cv).Property(item => item.Version).OriginalValue = version;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict publishing CV {CvId} by user {UserId}",
                id,
                GetCurrentUserId());
            TempData["CvPublishError"] = "The CV changed while it was being published. Reload it and try again.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _logger.LogInformation(
            "CV {CvId} was published by user {UserId}",
            cv.Id,
            GetCurrentUserId());
        TempData["CvSuccess"] = "CV published. Recruiters can now find and review it.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Candidate,Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(Guid id, uint version)
    {
        var cv = await BuildCvQuery().FirstOrDefaultAsync(item => item.Id == id);
        if (cv is null)
        {
            return NotFound();
        }

        if (!CanManageCv(cv))
        {
            LogDeniedAccess("unpublish", cv.Id);
            return Forbid();
        }

        if (!string.Equals(cv.Status, CvStatuses.Published, StringComparison.OrdinalIgnoreCase))
        {
            TempData["CvNotice"] = "This CV is already hidden from recruiters.";
            return RedirectToAction(nameof(Details), new { id });
        }

        cv.Status = CvStatuses.Draft;
        cv.UpdatedDate = DateTime.UtcNow;
        _context.Entry(cv).Property(item => item.Version).OriginalValue = version;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict unpublishing CV {CvId} by user {UserId}",
                id,
                GetCurrentUserId());
            TempData["CvPublishError"] = "The CV changed while it was being unpublished. Reload it and try again.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _logger.LogInformation(
            "CV {CvId} was unpublished by user {UserId}",
            cv.Id,
            GetCurrentUserId());
        TempData["CvSuccess"] = "CV unpublished. Recruiters can no longer view it.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Candidate,Administrator")]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var cv = await BuildCvQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (cv is null)
        {
            return NotFound();
        }

        if (!CanManageCv(cv))
        {
            LogDeniedAccess("delete", cv.Id);
            return Forbid();
        }

        return View(cv);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Candidate,Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, uint version)
    {
        var cv = await BuildCvQuery().FirstOrDefaultAsync(item => item.Id == id);
        if (cv is null)
        {
            return NotFound();
        }

        if (!CanManageCv(cv))
        {
            LogDeniedAccess("delete", cv.Id);
            return Forbid();
        }

        _context.Entry(cv).Property(item => item.Version).OriginalValue = version;
        _context.Cvs.Remove(cv);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["CvPublishError"] = "The CV changed before it could be deleted. Review the latest version and try again.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Candidate,Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSelected(
        List<Guid>? selectedIds,
        Dictionary<Guid, uint>? selectedVersions)
    {
        if (selectedIds is null || selectedIds.Count == 0)
        {
            TempData["CvNotice"] = "Select at least one CV to delete.";
            return RedirectToAction(nameof(Index));
        }

        var requestedIds = selectedIds.Distinct().ToHashSet();
        var cvs = await BuildCvQuery()
            .Where(cv => requestedIds.Contains(cv.Id))
            .ToListAsync();

        if (cvs.Count != requestedIds.Count || cvs.Any(cv => !CanManageCv(cv)))
        {
            _logger.LogWarning(
                "User {UserId} attempted an unauthorized bulk CV deletion",
                GetCurrentUserId());
            return Forbid();
        }

        if (selectedVersions is null || requestedIds.Any(id => !selectedVersions.ContainsKey(id)))
        {
            return BadRequest("A version is required for every selected CV.");
        }

        foreach (var cv in cvs)
        {
            _context.Entry(cv).Property(item => item.Version).OriginalValue = selectedVersions[cv.Id];
        }

        _context.Cvs.RemoveRange(cvs);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["CvNotice"] = "At least one selected CV changed. Nothing was deleted; reload the list and try again.";
            return RedirectToAction(nameof(Index));
        }
        TempData["CvSuccess"] = $"Deleted {cvs.Count} CV(s).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Recruiter")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(Guid id)
    {
        var cv = await BuildCvQuery().FirstOrDefaultAsync(item => item.Id == id);
        if (cv is null)
        {
            return NotFound();
        }

        if (!string.Equals(cv.Status, CvStatuses.Published, StringComparison.OrdinalIgnoreCase)
            || !CandidateStillHasAccess(cv))
        {
            LogDeniedAccess("like", cv.Id);
            return Forbid();
        }

        var currentUserId = GetCurrentUserId();
        var existingLike = cv.Likes.FirstOrDefault(like => like.RecruiterId == currentUserId);
        if (existingLike is null)
        {
            _context.CvLikes.Add(new CvLike
            {
                CvId = cv.Id,
                Cv = cv,
                RecruiterId = currentUserId,
                Recruiter = null!
            });
            TempData["CvSuccess"] = "Like added.";
        }
        else
        {
            _context.CvLikes.Remove(existingLike);
            TempData["CvSuccess"] = "Like removed.";
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData.Remove("CvSuccess");
            _logger.LogInformation(
                "CV like changed concurrently for CV {CvId} and recruiter {RecruiterId}",
                cv.Id,
                currentUserId);
            TempData["CvNotice"] = "The like was changed in another request. Reload the CV to see its current state.";
        }
        catch (DbUpdateException)
        {
            TempData.Remove("CvSuccess");
            _logger.LogInformation(
                "Duplicate concurrent CV like prevented for CV {CvId} and recruiter {RecruiterId}",
                cv.Id,
                currentUserId);
            TempData["CvNotice"] = "The like was already updated in another request.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private IQueryable<Cv> BuildCvQuery()
    {
        return _context.Cvs
            .AsSplitQuery()
            .Include(cv => cv.CandidateProfile)
                .ThenInclude(candidate => candidate.AttributeValues)
                    .ThenInclude(value => value.AttributeDefinition)
                        .ThenInclude(definition => definition.Options)
            .Include(cv => cv.CandidateProfile)
                .ThenInclude(candidate => candidate.Projects)
                    .ThenInclude(project => project.TechnologyTags)
            .Include(cv => cv.Position)
                .ThenInclude(position => position.PositionRequiredAttributes)
                    .ThenInclude(requirement => requirement.AttributeDefinition)
                        .ThenInclude(definition => definition.Options)
            .Include(cv => cv.Position)
                .ThenInclude(position => position.PositionAccessRules)
            .Include(cv => cv.Likes);
    }

    private bool CandidateStillHasAccess(Cv cv)
    {
        return _positionAccessService.CanCandidateAccess(
            cv.Position,
            cv.CandidateProfile.AttributeValues);
    }

    private bool CanViewCv(Cv cv)
    {
        return RoleAccessPolicy.CanViewCv(
            RoleAccessContext.FromPrincipal(User),
            cv.CandidateProfile.UserId,
            cv.Status,
            CandidateStillHasAccess(cv));
    }

    private bool CanManageCv(Cv cv)
    {
        return RoleAccessPolicy.CanManageCv(
            RoleAccessContext.FromPrincipal(User),
            cv.CandidateProfile.UserId,
            CandidateStillHasAccess(cv));
    }

    private async Task PopulateCreateListsAsync(Guid? selectedPositionId, Guid? selectedCandidateId)
    {
        var positions = await _context.Positions
            .AsNoTracking()
            .Include(position => position.PositionAccessRules)
            .OrderBy(position => position.Title)
            .ToListAsync();

        if (!User.IsInRole("Administrator"))
        {
            var currentUserId = GetCurrentUserId();
            var candidate = await _context.CandidateProfiles
                .AsNoTracking()
                .Include(profile => profile.AttributeValues)
                .FirstOrDefaultAsync(profile => profile.UserId == currentUserId);

            positions = candidate is null
                ? []
                : positions
                    .Where(position => _positionAccessService.CanCandidateAccess(position, candidate.AttributeValues))
                    .ToList();
        }

        ViewData["PositionId"] = new SelectList(positions, "Id", "Title", selectedPositionId);

        if (User.IsInRole("Administrator"))
        {
            var candidates = await _context.CandidateProfiles
                .AsNoTracking()
                .Include(candidate => candidate.AttributeValues)
                    .ThenInclude(value => value.AttributeDefinition)
                .ToListAsync();

            var candidateOptions = candidates
                .Select(candidate => new
                {
                    candidate.Id,
                    FullName = $"{candidate.FirstName} {candidate.LastName}".Trim()
                })
                .OrderBy(candidate => candidate.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            ViewData["CandidateProfileId"] = new SelectList(
                candidateOptions,
                "Id",
                "FullName",
                selectedCandidateId);
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The authenticated user has no identifier claim.");
    }

    private void LogDeniedAccess(string operation, Guid cvId)
    {
        _logger.LogWarning(
            "User {UserId} was denied permission to {Operation} CV {CvId}",
            GetCurrentUserId(),
            operation,
            cvId);
    }

}
