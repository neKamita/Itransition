using Itransition.Data;
using Itransition.Services;
using Itransition.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itransition.Controllers;

[Authorize(Roles = "Recruiter,Administrator")]
public sealed class CandidateDirectoryController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PositionAccessService _positionAccessService;

    public CandidateDirectoryController(
        ApplicationDbContext context,
        PositionAccessService positionAccessService)
    {
        _context = context;
        _positionAccessService = positionAccessService;
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var candidate = await _context.CandidateProfiles
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(profile => profile.AttributeValues)
                .ThenInclude(value => value.AttributeDefinition)
            .Include(profile => profile.Cvs)
                .ThenInclude(cv => cv.Position)
                    .ThenInclude(position => position.PositionAccessRules)
            .Include(profile => profile.Cvs)
                .ThenInclude(cv => cv.Likes)
            .FirstOrDefaultAsync(profile => profile.Id == id, cancellationToken);

        if (candidate is null)
        {
            return NotFound();
        }

        var visibleCvs = candidate.Cvs
            .Where(cv => CandidateDirectoryVisibilityPolicy.CanShowCv(
                cv.Status,
                _positionAccessService.CanCandidateAccess(cv.Position, candidate.AttributeValues)))
            .OrderByDescending(cv => cv.UpdatedDate)
            .Select(cv => new CandidateDirectoryCvViewModel
            {
                Id = cv.Id,
                PositionTitle = cv.Position.Title,
                Company = cv.Position.Company,
                Level = cv.Position.Level,
                LikesCount = cv.Likes.Count,
                UpdatedDate = cv.UpdatedDate
            })
            .ToList();

        // Do not reveal whether a private profile exists when the recruiter has
        // no published, currently accessible CV through which to discover it.
        if (visibleCvs.Count == 0)
        {
            return NotFound();
        }

        var candidateName = $"{candidate.FirstName} {candidate.LastName}".Trim();
        return View(new CandidateDirectoryDetailsViewModel
        {
            CandidateProfileId = candidate.Id,
            CandidateName = string.IsNullOrWhiteSpace(candidateName) ? "Candidate" : candidateName,
            PublishedCvs = visibleCvs
        });
    }
}
