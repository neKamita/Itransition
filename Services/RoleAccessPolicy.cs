using System.Security.Claims;
using Itransition.Models.Cvs;

namespace Itransition.Services;

public readonly record struct RoleAccessContext(
    bool IsAdministrator,
    bool IsRecruiter,
    bool IsCandidate,
    string? UserId)
{
    public static RoleAccessContext FromPrincipal(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new RoleAccessContext(
            user.IsInRole("Administrator"),
            user.IsInRole("Recruiter"),
            user.IsInRole("Candidate"),
            user.FindFirstValue(ClaimTypes.NameIdentifier));
    }
}

public static class RoleAccessPolicy
{
    public static bool CanBrowseAllPositions(RoleAccessContext roles)
    {
        return roles.IsAdministrator || roles.IsRecruiter;
    }

    public static bool CanSeeCvInList(
        RoleAccessContext roles,
        string candidateUserId,
        string status)
    {
        if (roles.IsAdministrator)
        {
            return true;
        }

        var isOwnCv = roles.IsCandidate
            && string.Equals(candidateUserId, roles.UserId, StringComparison.Ordinal);
        var isPublishedForRecruiter = roles.IsRecruiter
            && string.Equals(status, CvStatuses.Published, StringComparison.OrdinalIgnoreCase);

        return isOwnCv || isPublishedForRecruiter;
    }

    public static bool CanViewCv(
        RoleAccessContext roles,
        string candidateUserId,
        string status,
        bool candidateStillHasPositionAccess)
    {
        if (roles.IsAdministrator)
        {
            return true;
        }

        return candidateStillHasPositionAccess
            && CanSeeCvInList(roles, candidateUserId, status);
    }

    public static bool CanManageCv(
        RoleAccessContext roles,
        string candidateUserId,
        bool candidateStillHasPositionAccess)
    {
        return roles.IsAdministrator
            || (roles.IsCandidate
                && candidateStillHasPositionAccess
                && string.Equals(candidateUserId, roles.UserId, StringComparison.Ordinal));
    }
}
