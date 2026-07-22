using Itransition.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itransition.Controllers;

[ApiController]
[Route("api/tags")]
[Authorize]
public sealed class TagsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TagsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Lookup(
        [FromQuery] string? prefix,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var normalizedPrefix = prefix?.Trim() ?? string.Empty;
        if (normalizedPrefix.Length > 50)
        {
            return BadRequest(new { error = "The prefix cannot exceed 50 characters." });
        }

        limit = Math.Clamp(limit, 1, 50);
        var pattern = $"%{normalizedPrefix}%";

        var projectTags = await _context.ProjectTechnologyTags
            .AsNoTracking()
            .Where(tag => EF.Functions.ILike(tag.TagName, pattern))
            .Select(tag => tag.TagName)
            .Distinct()
            .Take(100)
            .ToListAsync(cancellationToken);

        var positionTagSets = await _context.Positions
            .AsNoTracking()
            .Where(position => position.Tags != null && EF.Functions.ILike(position.Tags, pattern))
            .Select(position => position.Tags!)
            .Take(100)
            .ToListAsync(cancellationToken);

        var results = projectTags
            .Concat(positionTagSets.SelectMany(tags =>
                tags.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
            .Where(tag => tag.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(tag => new { value = tag, text = tag })
            .ToList();

        return Ok(results);
    }
}
