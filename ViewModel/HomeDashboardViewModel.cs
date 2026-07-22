using Itransition.Models.Positions;

namespace Itransition.ViewModel;

public sealed class HomeDashboardViewModel
{
    public int VisiblePositionsCount { get; init; }
    public int CandidatesCount { get; init; }
    public int RecruitersCount { get; init; }
    public int SubmittedCvsCount { get; init; }
    public int CvsCreatedLast24Hours { get; init; }
    public IReadOnlyList<PositionDashboardItem> LatestPositions { get; init; } = [];
    public IReadOnlyList<PositionDashboardItem> PopularPositions { get; init; } = [];
    public IReadOnlyDictionary<string, int> PopularTags { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
}

public sealed record PositionDashboardItem(Position Position, int CvCount);
