using Itransition.Models.Cvs;

namespace Itransition.Services;

public static class CandidateDirectoryVisibilityPolicy
{
    public static bool CanShowCv(string status, bool candidateStillHasPositionAccess)
    {
        return candidateStillHasPositionAccess
            && string.Equals(status, CvStatuses.Published, StringComparison.OrdinalIgnoreCase);
    }
}
