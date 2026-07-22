using Itransition.Models.Cvs;
using Itransition.Models.Positions;

namespace Itransition.ViewModel;

public sealed class SearchResultsViewModel
{
    public required string Query { get; init; }
    public IReadOnlyList<Position> Positions { get; init; } = [];
    public IReadOnlyList<Cv> Cvs { get; init; } = [];
}
