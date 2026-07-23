using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Itransition.Data;
using Itransition.Models.Positions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Itransition.Models.Cvs;
using Itransition.Services;

namespace Itransition.Controllers
{
    [Authorize(Roles="Administrator,Recruiter")]
    public class PositionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PositionAccessService _positionAccessService;
        private readonly ILogger<PositionsController> _logger;

        public PositionsController(
            ApplicationDbContext context,
            PositionAccessService positionAccessService,
            ILogger<PositionsController> logger)
        {
            _context = context;
            _positionAccessService = positionAccessService;
            _logger = logger;
        }

        // GET: Positions
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var positions = await _context.Positions
                .AsNoTracking()
                .Include(p => p.PositionAccessRules)
                .ToListAsync();

            if (User.IsInRole("Administrator") || User.IsInRole("Recruiter"))
            {
                return View(positions);
            }

            if (User.Identity?.IsAuthenticated != true)
            {
                return View(positions.Where(position => position.IsPublic).ToList());
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var candidate = await _context.CandidateProfiles
                .AsNoTracking()
                .Include(profile => profile.AttributeValues)
                .FirstOrDefaultAsync(profile => profile.UserId == userId);

            var visiblePositions = candidate is null
                ? positions.Where(position => position.IsPublic).ToList()
                : positions
                    .Where(position => _positionAccessService.CanCandidateAccess(position, candidate.AttributeValues))
                    .ToList();

            return View(visiblePositions);
        }

        // GET: Positions/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var position = await _context.Positions
                .Include(p => p.PositionRequiredAttributes)
                    .ThenInclude(pa => pa.AttributeDefinition)
                        .ThenInclude(attribute => attribute.Category)
                .Include(p => p.PositionAccessRules)
                    .ThenInclude(pr => pr.AttributeDefinition)
                        .ThenInclude(attribute => attribute.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (position == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Administrator") && !User.IsInRole("Recruiter"))
            {
                if (User.Identity?.IsAuthenticated != true)
                {
                    if (!position.IsPublic)
                    {
                        return Forbid();
                    }
                }
                else
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var candidate = await _context.CandidateProfiles
                        .AsNoTracking()
                        .Include(profile => profile.AttributeValues)
                        .FirstOrDefaultAsync(profile => profile.UserId == userId);

                    if (candidate is null
                        || !_positionAccessService.CanCandidateAccess(position, candidate.AttributeValues))
                    {
                        _logger.LogWarning(
                            "User {UserId} was denied access to position {PositionId}",
                            userId,
                            position.Id);
                        return Forbid();
                    }
                }
            }

            if (User.IsInRole("Administrator") || User.IsInRole("Recruiter"))
            {
                var positionCvs = await _context.Cvs
                    .AsNoTracking()
                    .Include(cv => cv.CandidateProfile)
                        .ThenInclude(candidate => candidate.AttributeValues)
                            .ThenInclude(value => value.AttributeDefinition)
                    .Include(cv => cv.Likes)
                    .Where(cv => cv.PositionId == position.Id)
                    .OrderByDescending(cv => cv.UpdatedDate)
                    .ToListAsync();

                if (User.IsInRole("Recruiter") && !User.IsInRole("Administrator"))
                {
                    positionCvs = positionCvs
                        .Where(cv => string.Equals(cv.Status, CvStatuses.Published, StringComparison.OrdinalIgnoreCase))
                        .Where(cv => _positionAccessService.CanCandidateAccess(position, cv.CandidateProfile.AttributeValues))
                        .ToList();
                }

                ViewData["PositionCvs"] = positionCvs;
            }

            return View(position);
        }

        // GET: Positions/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Positions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Tags,Company,Level,MaxProjectInCv,IsPublic")] Position position)
        {
            if (ModelState.IsValid)
            {
                position.Id = Guid.NewGuid();
                position.CreatedDate = DateTime.UtcNow;
                position.UpdatedDate = position.CreatedDate;
                _context.Add(position);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(position);
        }

        // GET: Positions/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var position = await _context.Positions.FindAsync(id);
            if (position == null)
            {
                return NotFound();
            }
            return View(position);
        }

        // POST: Positions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Description,Tags,Company,Level,MaxProjectInCv,IsPublic,Version")] Position position)
        {
            if (id != position.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(position);
            }

            var positionToUpdate = await _context.Positions.FindAsync(id);
            if (positionToUpdate is null)
            {
                return NotFound();
            }

            positionToUpdate.Title = position.Title;
            positionToUpdate.Description = position.Description;
            positionToUpdate.Tags = position.Tags;
            positionToUpdate.Company = position.Company;
            positionToUpdate.Level = position.Level;
            positionToUpdate.MaxProjectInCv = position.MaxProjectInCv;
            positionToUpdate.IsPublic = position.IsPublic;
            positionToUpdate.UpdatedDate = DateTime.UtcNow;
            _context.Entry(positionToUpdate).Property(item => item.Version).OriginalValue = position.Version;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Positions.AnyAsync(item => item.Id == id))
                {
                    return NotFound();
                }

                var databaseValues = await _context.Entry(positionToUpdate).GetDatabaseValuesAsync();
                position.Version = databaseValues?.GetValue<uint>(nameof(Position.Version)) ?? position.Version;
                ModelState.AddModelError(string.Empty, "This position was changed by another user. Review your values and submit again to overwrite the latest version.");
                return View(position);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Positions/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var position = await _context.Positions
                .FirstOrDefaultAsync(m => m.Id == id);
            if (position == null)
            {
                return NotFound();
            }

            return View(position);
        }

        // POST: Positions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id, uint version)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position != null)
            {
                _context.Entry(position).Property(item => item.Version).OriginalValue = version;
                _context.Positions.Remove(position);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["PositionError"] = "The position changed before deletion. Review it and try again.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(
            List<Guid>? selectedIds,
            Dictionary<Guid, uint>? selectedVersions)
        {
            if (selectedIds is null || selectedIds.Count == 0)
            {
                TempData["PositionNotice"] = "Select at least one position to delete.";
                return RedirectToAction(nameof(Index));
            }

            var ids = selectedIds.Distinct().ToList();
            var positions = await _context.Positions.Where(position => ids.Contains(position.Id)).ToListAsync();
            if (positions.Count != ids.Count)
            {
                TempData["PositionError"] = "One or more selected positions no longer exist.";
                return RedirectToAction(nameof(Index));
            }

            if (selectedVersions is null || ids.Any(id => !selectedVersions.ContainsKey(id)))
            {
                return BadRequest("A version is required for every selected position.");
            }

            foreach (var position in positions)
            {
                _context.Entry(position).Property(item => item.Version).OriginalValue = selectedVersions[position.Id];
            }

            _context.Positions.RemoveRange(positions);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["PositionError"] = "At least one selected position changed. Nothing was deleted; reload the list and try again.";
                return RedirectToAction(nameof(Index));
            }
            TempData["PositionSuccess"] = $"Deleted {positions.Count} position(s).";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duplicate(Guid id)
        {
            var source = await _context.Positions
                .AsNoTracking()
                .Include(position => position.PositionRequiredAttributes)
                .Include(position => position.PositionAccessRules)
                .FirstOrDefaultAsync(position => position.Id == id);
            if (source is null)
            {
                return NotFound();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var now = DateTime.UtcNow;
            var copy = new Position
            {
                Id = Guid.NewGuid(),
                Title = $"Copy of {source.Title}",
                Description = source.Description,
                Tags = source.Tags,
                Company = source.Company,
                Level = source.Level,
                MaxProjectInCv = source.MaxProjectInCv,
                IsPublic = false,
                CreatedDate = now,
                UpdatedDate = now
            };

            copy.PositionRequiredAttributes = source.PositionRequiredAttributes.Select(requirement => new PositionAttribute
            {
                Id = Guid.NewGuid(),
                PositionId = copy.Id,
                Position = copy,
                AttributeDefinitionId = requirement.AttributeDefinitionId,
                AttributeDefinition = null!,
                OrderIndex = requirement.OrderIndex
            }).ToList();

            copy.PositionAccessRules = source.PositionAccessRules.Select(rule => new PositionAccessRule
            {
                Id = Guid.NewGuid(),
                PositionId = copy.Id,
                Position = copy,
                AttributeDefinitionId = rule.AttributeDefinitionId,
                AttributeDefinition = null!,
                Operator = rule.Operator,
                TargetValue = rule.TargetValue
            }).ToList();

            _context.Positions.Add(copy);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Position {SourcePositionId} duplicated as {CopyPositionId} by user {UserId}",
                source.Id,
                copy.Id,
                User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData["PositionSuccess"] = "Position duplicated. Review the copy before making it available to candidates.";
            return RedirectToAction(nameof(Edit), new { id = copy.Id });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRequirement(Guid positionId, Guid attributeId, uint positionVersion)
        {
            if (positionId == Guid.Empty || attributeId == Guid.Empty)
            {
                return BadRequest("A position and attribute are required.");
            }

            var position = await _context.Positions.FindAsync(positionId);
            var attributeDefinition = await _context.AttributeDefinitions.FindAsync(attributeId);
            if (position is null || attributeDefinition is null)
            {
                return NotFound();
            }

            if (await _context.PositionAttributes.AnyAsync(requirement =>
                    requirement.PositionId == positionId
                    && requirement.AttributeDefinitionId == attributeId))
            {
                TempData["PositionError"] = "This attribute is already part of the position template.";
                return RedirectToAction(nameof(Details), new { id = positionId });
            }

            var req = new PositionAttribute
            {
                Id = Guid.NewGuid(),
                PositionId = positionId,
                AttributeDefinitionId = attributeId,
                Position = null!,
                AttributeDefinition = null!
            };

            _context.Entry(position).Property(item => item.Version).OriginalValue = positionVersion;
            position.UpdatedDate = DateTime.UtcNow;
            attributeDefinition.LastUsedAt = DateTime.UtcNow;
            _context.PositionAttributes.Add(req);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["PositionError"] = "The position template changed while you were editing it. Reload the page and try again.";
                return RedirectToAction(nameof(Details), new { id = positionId });
            }
            catch (DbUpdateException)
            {
                TempData["PositionError"] = "This attribute was added by another recruiter. Reload the position.";
                return RedirectToAction(nameof(Details), new { id = positionId });
            }

            TempData["PositionSuccess"] = "Attribute added to the position template.";
            return RedirectToAction(nameof(Details), new { id = positionId });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRequirement(Guid id, Guid positionId, uint version, uint positionVersion)
        {
            var req = await _context.PositionAttributes.FindAsync(id);
            var position = await _context.Positions.FindAsync(positionId);
            if (req is null || position is null || req.PositionId != positionId)
            {
                return NotFound();
            }

            _context.Entry(req).Property(item => item.Version).OriginalValue = version;
            _context.Entry(position).Property(item => item.Version).OriginalValue = positionVersion;
            position.UpdatedDate = DateTime.UtcNow;
            _context.PositionAttributes.Remove(req);

            try
            {
                await _context.SaveChangesAsync();
            }

            catch (DbUpdateConcurrencyException)
            {
                TempData["PositionError"] = "The requirement or position changed before it could be removed. Reload the page and try again.";
            }

            return RedirectToAction(nameof(Details), new { id = positionId });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAccessRule(Guid positionId, Guid attributeId, string operatorType, string targetValue, uint positionVersion)
        {
            var position = await _context.Positions
                .FirstOrDefaultAsync(item => item.Id == positionId);

            if (position is null)
            {
                return NotFound();
            }

            if (position.IsPublic)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to add an access rule to public position {PositionId}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    positionId);
                TempData["AccessRuleError"] = "Public positions are available without filters. Switch the position to Restricted before adding access rules.";
                return RedirectToAction(nameof(Details), new { id = positionId });
            }

            var attributeDefinition = await _context.AttributeDefinitions
                .Include(attribute => attribute.Options)
                .FirstOrDefaultAsync(attribute => attribute.Id == attributeId);

            if (attributeDefinition is null)
            {
                TempData["AccessRuleError"] = "The selected attribute no longer exists. Choose another attribute.";
                return RedirectToAction(nameof(Details), new { id = positionId });
            }

            var normalizedOperator = PositionAccessRulePolicy.NormalizeOperator(operatorType);
            if (!PositionAccessRulePolicy.GetAllowedOperators(attributeDefinition.DataType)
                    .Contains(normalizedOperator, StringComparer.Ordinal))
            {
                _logger.LogWarning(
                    "User {UserId} submitted an invalid operator for attribute {AttributeId} of type {DataType}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    attributeDefinition.Id,
                    attributeDefinition.DataType);
                TempData["AccessRuleError"] = $"The selected operator is not valid for {attributeDefinition.DataType} attributes.";
                return RedirectToAction(nameof(Details), new { id = positionId });
            }

            if (!PositionAccessRulePolicy.TryNormalizeTargetValue(
                    attributeDefinition,
                    targetValue,
                    out var normalizedTargetValue,
                    out var validationError))
            {
                TempData["AccessRuleError"] = validationError;
                return RedirectToAction(nameof(Details), new { id = positionId });
            }

            var duplicateExists = await _context.PositionAccessRules.AnyAsync(rule =>
                rule.PositionId == positionId
                && rule.AttributeDefinitionId == attributeId
                && rule.Operator == normalizedOperator
                && rule.TargetValue == normalizedTargetValue);

            if (duplicateExists)
            {
                TempData["AccessRuleError"] = "This access rule already exists.";
                return RedirectToAction(nameof(Details), new { id = positionId });
            }

            var rule = new PositionAccessRule
            {
                Id = Guid.NewGuid(),
                PositionId = positionId,
                AttributeDefinitionId = attributeId,
                Operator = normalizedOperator,
                TargetValue = normalizedTargetValue,
                Position = null!,
                AttributeDefinition = null!
            };

            _context.Entry(position).Property(item => item.Version).OriginalValue = positionVersion;
            position.UpdatedDate = DateTime.UtcNow;
            attributeDefinition.LastUsedAt = DateTime.UtcNow;
            _context.PositionAccessRules.Add(rule);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["AccessRuleError"] = "The position changed while you were editing its access rules. Reload the page and try again.";
                return RedirectToAction(nameof(Details), new { id = positionId });
            }
            catch (DbUpdateException)
            {
                TempData["AccessRuleError"] = "This access rule was added by another recruiter. Reload the position.";
                return RedirectToAction(nameof(Details), new { id = positionId });
            }

            _logger.LogInformation(
                "Access rule {RuleId} added to position {PositionId} by user {UserId}",
                rule.Id,
                positionId,
                User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData["AccessRuleSuccess"] = "Access rule added successfully.";
            return RedirectToAction(nameof(Details), new { id = positionId });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAccessRule(Guid id, Guid positionId, uint version, uint positionVersion)
        {
            var rule = await _context.PositionAccessRules.FindAsync(id);
            var position = await _context.Positions.FindAsync(positionId);
            if (rule is null || position is null || rule.PositionId != positionId)
            {
                return NotFound();
            }

            _context.Entry(rule).Property(item => item.Version).OriginalValue = version;
            _context.Entry(position).Property(item => item.Version).OriginalValue = positionVersion;
            position.UpdatedDate = DateTime.UtcNow;
            _context.PositionAccessRules.Remove(rule);

            try
            {
                await _context.SaveChangesAsync();
            }

            catch (DbUpdateConcurrencyException)
            {
                TempData["AccessRuleError"] = "The access rule or position changed before it could be removed. Reload the page and try again.";
            }

            return RedirectToAction(nameof(Details), new { id = positionId });
        }
    }
}
