using System.Diagnostics;
using System.Security.Claims;
using Itransition.Data;
using Itransition.Models;
using Itransition.Models.Cvs;
using Itransition.Services;
using Itransition.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itransition.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PositionAccessService _positionAccessService;

    public HomeController(
        ApplicationDbContext context,
        PositionAccessService positionAccessService)
    {
        _context = context;
        _positionAccessService = positionAccessService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var roles = RoleAccessContext.FromPrincipal(User);
        var canBrowseAllPositions = RoleAccessPolicy.CanBrowseAllPositions(roles);
        var positionQuery = _context.Positions
            .AsNoTracking()
            .Include(position => position.PositionAccessRules)
            .AsQueryable();

        if (!canBrowseAllPositions && !roles.IsCandidate)
        {
            positionQuery = positionQuery.Where(position => position.IsPublic);
        }

        var visiblePositions = await positionQuery.ToListAsync(cancellationToken);

        if (!canBrowseAllPositions && roles.IsCandidate)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var candidate = await _context.CandidateProfiles
                .AsNoTracking()
                .Include(profile => profile.AttributeValues)
                .FirstOrDefaultAsync(profile => profile.UserId == currentUserId, cancellationToken);

            visiblePositions = candidate is null
                ? []
                : visiblePositions
                    .Where(position => _positionAccessService.CanCandidateAccess(
                        position,
                        candidate.AttributeValues))
                    .ToList();
        }

        var visiblePositionIds = visiblePositions.Select(position => position.Id).ToList();
        var cvCounts = await _context.Cvs
            .AsNoTracking()
            .Where(cv => visiblePositionIds.Contains(cv.PositionId))
            .GroupBy(cv => cv.PositionId)
            .Select(group => new { PositionId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.PositionId, item => item.Count, cancellationToken);

        var latestPositions = visiblePositions
            .OrderByDescending(position => position.UpdatedDate)
            .ThenBy(position => position.Title, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .Select(position => new PositionDashboardItem(
                position,
                cvCounts.GetValueOrDefault(position.Id)))
            .ToList();

        var popularPositions = visiblePositions
            .Select(position => new PositionDashboardItem(
                position,
                cvCounts.GetValueOrDefault(position.Id)))
            .OrderByDescending(item => item.CvCount)
            .ThenByDescending(item => item.Position.UpdatedDate)
            .Take(5)
            .ToList();

        var popularTags = visiblePositions
            .Where(position => !string.IsNullOrWhiteSpace(position.Tags))
            .Select(position => position.Tags!)
            .SelectMany(tags => tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(15)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var recruiterRoleId = await _context.Roles
            .AsNoTracking()
            .Where(role => role.Name == "Recruiter")
            .Select(role => role.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var recruiterCount = recruiterRoleId is null
            ? 0
            : await _context.UserRoles
                .AsNoTracking()
                .Where(userRole => userRole.RoleId == recruiterRoleId)
                .Select(userRole => userRole.UserId)
                .Distinct()
                .CountAsync(cancellationToken);

        var since = DateTime.UtcNow.AddHours(-24);
        return View(new HomeDashboardViewModel
        {
            VisiblePositionsCount = visiblePositions.Count,
            CandidatesCount = await _context.CandidateProfiles.CountAsync(cancellationToken),
            RecruitersCount = recruiterCount,
            SubmittedCvsCount = await _context.Cvs.CountAsync(cancellationToken),
            CvsCreatedLast24Hours = await _context.Cvs.CountAsync(cv => cv.CreatedDate >= since, cancellationToken),
            LatestPositions = latestPositions,
            PopularPositions = popularPositions,
            PopularTags = popularTags
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public async Task<IActionResult> Search(string? query, CancellationToken cancellationToken)
    {
        var normalizedQuery = query?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return View("SearchResults", new SearchResultsViewModel { Query = string.Empty });
        }

        if (normalizedQuery.Length > 200)
        {
            ModelState.AddModelError(nameof(query), "Search text cannot exceed 200 characters.");
            return View("SearchResults", new SearchResultsViewModel { Query = normalizedQuery[..200] });
        }

        ViewData["SearchQuery"] = normalizedQuery;

        var positionQuery = _context.Positions
            .AsNoTracking()
            .Include(position => position.PositionAccessRules)
            .Where(position => position.SearchVector.Matches(
                EF.Functions.WebSearchToTsQuery("english", normalizedQuery)));

        var roles = RoleAccessContext.FromPrincipal(User);
        var canBrowseAllPositions = RoleAccessPolicy.CanBrowseAllPositions(roles);
        if (!canBrowseAllPositions && !roles.IsCandidate)
        {
            positionQuery = positionQuery.Where(position => position.IsPublic);
        }

        var positions = await positionQuery
            .OrderByDescending(position => position.SearchVector.Rank(
                EF.Functions.WebSearchToTsQuery("english", normalizedQuery)))
            .ThenByDescending(position => position.UpdatedDate)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (!canBrowseAllPositions && roles.IsCandidate)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var candidate = await _context.CandidateProfiles
                .AsNoTracking()
                .Include(profile => profile.AttributeValues)
                .FirstOrDefaultAsync(profile => profile.UserId == currentUserId, cancellationToken);

            positions = candidate is null
                ? []
                : positions
                    .Where(position => _positionAccessService.CanCandidateAccess(
                        position,
                        candidate.AttributeValues))
                    .ToList();
        }

        var cvs = new List<Cv>();
        var canSearchCvs = roles.IsAdministrator || roles.IsRecruiter || roles.IsCandidate;
        if (canSearchCvs)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cvQuery = _context.Cvs
                .AsNoTracking()
                .AsSplitQuery()
                .Include(cv => cv.Likes)
                .Include(cv => cv.Position)
                    .ThenInclude(position => position.PositionAccessRules)
                .Include(cv => cv.CandidateProfile)
                    .ThenInclude(profile => profile.AttributeValues)
                .Where(cv =>
                    cv.Position.SearchVector.Matches(
                        EF.Functions.WebSearchToTsQuery("english", normalizedQuery))
                    || cv.CandidateProfile.AttributeValues.Any(value =>
                        value.SearchVector.Matches(
                            EF.Functions.WebSearchToTsQuery("english", normalizedQuery))));

            if (!roles.IsAdministrator)
            {
                cvQuery = cvQuery.Where(cv =>
                    (roles.IsRecruiter && cv.Status == CvStatuses.Published)
                    || (roles.IsCandidate && cv.CandidateProfile.UserId == currentUserId));
            }

            cvs = await cvQuery
                .OrderByDescending(cv => cv.UpdatedDate)
                .Take(100)
                .ToListAsync(cancellationToken);

            if (!roles.IsAdministrator)
            {
                cvs = cvs
                    .Where(cv => _positionAccessService.CanCandidateAccess(
                        cv.Position,
                        cv.CandidateProfile.AttributeValues))
                    .ToList();
            }
        }

        return View("SearchResults", new SearchResultsViewModel
        {
            Query = normalizedQuery,
            Positions = positions,
            Cvs = cvs
        });
    }
}
