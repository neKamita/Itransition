using Itransition.Data;
using Itransition.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itransition.Controllers;

[ApiController]
[Route("api/attributes")]
[Authorize(Roles = "Candidate,Recruiter,Administrator")]
public sealed class AttributeLookupController : ControllerBase
{
    private const int DefaultLimit = 20;
    private const int MaximumLimit = 50;
    private readonly ApplicationDbContext _context;

    public AttributeLookupController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Lookup(
        [FromQuery] string? prefix,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? profileId,
        [FromQuery] bool recent = false,
        [FromQuery] int limit = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        var normalizedPrefix = prefix?.Trim();
        if (normalizedPrefix?.Length > 50)
        {
            return BadRequest(new { error = "The prefix cannot exceed 50 characters." });
        }

        limit = Math.Clamp(limit, 1, MaximumLimit);
        var query = _context.AttributeDefinitions.AsNoTracking();

        if (profileId.HasValue)
        {
            var canInspectProfile = User.IsInRole("Administrator")
                || await _context.CandidateProfiles.AnyAsync(
                    profile => profile.Id == profileId.Value
                        && profile.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier),
                    cancellationToken);
            if (!canInspectProfile)
            {
                return Forbid();
            }

            query = query.Where(attribute => !_context.UserAttributeValues.Any(value =>
                value.CandidateProfileId == profileId.Value
                && value.AttributeDefinitionId == attribute.Id));
        }

        if (!string.IsNullOrWhiteSpace(normalizedPrefix))
        {
            var escapedPrefix = normalizedPrefix
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
            query = query.Where(attribute =>
                EF.Functions.ILike(attribute.Name, escapedPrefix + "%", "\\"));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(attribute => attribute.CategoryId == categoryId.Value);
        }

        if (recent)
        {
            query = query.Where(attribute => attribute.LastUsedAt.HasValue)
                .OrderByDescending(attribute => attribute.LastUsedAt)
                .ThenBy(attribute => attribute.Name);
        }
        else
        {
            query = query.OrderBy(attribute => attribute.Category.SortOrder)
                .ThenBy(attribute => attribute.Name);
        }

        var records = await query
            .Take(limit)
            .Select(attribute => new
            {
                attribute.Id,
                attribute.Name,
                attribute.CategoryId,
                Category = attribute.Category.Name,
                attribute.DataType,
                attribute.IsBuiltIn,
                attribute.LastUsedAt,
                Options = attribute.Options
                    .OrderBy(option => option.Value)
                    .Select(option => option.Value)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var results = records.Select(attribute => new
        {
            attribute.Id,
            attribute.Name,
            attribute.CategoryId,
            attribute.Category,
            DataType = attribute.DataType.ToString(),
            attribute.IsBuiltIn,
            attribute.LastUsedAt,
            attribute.Options,
            Operators = PositionAccessRulePolicy.GetAllowedOperators(attribute.DataType)
        });

        return Ok(results);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories(CancellationToken cancellationToken)
    {
        var categories = await _context.AttributeCategories
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new { category.Id, category.Name })
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }
}
