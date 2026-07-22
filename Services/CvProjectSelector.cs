using Itransition.Models.Positions;
using Itransition.Models.Profiles;

namespace Itransition.Services;

public static class CvProjectSelector
{
    public static IReadOnlyList<ProjectProfile> SelectRelevantProjects(
        Position position,
        IEnumerable<ProjectProfile> projects)
    {
        if (position.MaxProjectInCv <= 0)
        {
            return [];
        }

        var positionTags = (position.Tags ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => tag.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var query = projects;
        if (positionTags.Count > 0)
        {
            query = query.Where(project => project.TechnologyTags.Any(tag =>
                positionTags.Contains(tag.TagName.Trim())));
        }

        return query
            .OrderByDescending(project => project.EndDate ?? DateTime.MaxValue)
            .ThenByDescending(project => project.StartDate ?? DateTime.MinValue)
            .ThenBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
            .Take(position.MaxProjectInCv)
            .ToList();
    }
}
