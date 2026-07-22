using Itransition.Models.Cvs;

namespace Itransition.Services;

public static class CvPublicationPolicy
{
    public static IReadOnlyList<string> GetMissingFields(Cv cv)
    {
        ArgumentNullException.ThrowIfNull(cv);

        var missingFields = new List<string>();

        if (string.IsNullOrWhiteSpace(cv.CandidateProfile.FirstName))
        {
            missingFields.Add("First name");
        }

        if (string.IsNullOrWhiteSpace(cv.CandidateProfile.LastName))
        {
            missingFields.Add("Last name");
        }

        var valuesByAttribute = cv.CandidateProfile.AttributeValues
            .GroupBy(value => value.AttributeDefinitionId)
            .ToDictionary(group => group.Key, group => group.First().Value);

        foreach (var requirement in cv.Position.PositionRequiredAttributes.OrderBy(item => item.OrderIndex))
        {
            if (!valuesByAttribute.TryGetValue(requirement.AttributeDefinitionId, out var value)
                || string.IsNullOrWhiteSpace(value))
            {
                missingFields.Add(requirement.AttributeDefinition.Name);
            }
        }

        return missingFields.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
