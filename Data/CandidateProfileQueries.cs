using Itransition.Models.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Itransition.Data;

public static class CandidateProfileQueries
{
    public static IQueryable<CandidateProfile> WithDetailsGraph(
        this IQueryable<CandidateProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        return profiles
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(candidate => candidate.User)
            .Include(candidate => candidate.AttributeValues)
                .ThenInclude(value => value.AttributeDefinition)
                    .ThenInclude(definition => definition.Options)
            .Include(candidate => candidate.AttributeValues)
                .ThenInclude(value => value.AttributeDefinition)
                    .ThenInclude(definition => definition.Category)
            .Include(candidate => candidate.Projects)
                .ThenInclude(project => project.TechnologyTags)
            .Include(candidate => candidate.Cvs)
                .ThenInclude(cv => cv.Position)
            .Include(candidate => candidate.Cvs)
                .ThenInclude(cv => cv.Likes);
    }
}
