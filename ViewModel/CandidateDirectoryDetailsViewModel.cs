namespace Itransition.ViewModel;

public sealed class CandidateDirectoryDetailsViewModel
{
    public required Guid CandidateProfileId { get; init; }
    public required string CandidateName { get; init; }
    public required IReadOnlyList<CandidateDirectoryCvViewModel> PublishedCvs { get; init; }
}

public sealed class CandidateDirectoryCvViewModel
{
    public required Guid Id { get; init; }
    public required string PositionTitle { get; init; }
    public required string Company { get; init; }
    public required string Level { get; init; }
    public required int LikesCount { get; init; }
    public required DateTime UpdatedDate { get; init; }
}
