using System.Globalization;
using Itransition.Data;
using Itransition.Models.Cvs;
using Itransition.Models.Positions;
using Microsoft.EntityFrameworkCore;

namespace Itransition.Services;

public sealed class PositionAccessService
{
    private readonly ApplicationDbContext _context;

    public PositionAccessService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanCandidateAccessAsync(
        Guid positionId,
        Guid candidateProfileId,
        CancellationToken cancellationToken = default)
    {
        var position = await _context.Positions
            .AsNoTracking()
            .Include(p => p.PositionAccessRules)
            .FirstOrDefaultAsync(p => p.Id == positionId, cancellationToken);

        if (position is null)
        {
            return false;
        }

        var attributeValues = await _context.UserAttributeValues
            .AsNoTracking()
            .Where(value => value.CandidateProfileId == candidateProfileId)
            .ToListAsync(cancellationToken);

        return CanCandidateAccess(position, attributeValues);
    }

    public bool CanCandidateAccess(
        Position position,
        IEnumerable<UserAttributeValue> attributeValues)
    {
        if (position.IsPublic)
        {
            return true;
        }

        if (position.PositionAccessRules.Count == 0)
        {
            return false;
        }

        var valuesByAttribute = attributeValues
            .GroupBy(value => value.AttributeDefinitionId)
            .ToDictionary(group => group.Key, group => group.First().Value);

        return position.PositionAccessRules.All(rule =>
            valuesByAttribute.TryGetValue(rule.AttributeDefinitionId, out var candidateValue)
            && MatchesRule(candidateValue, rule));
    }

    private static bool MatchesRule(string? candidateValue, PositionAccessRule rule)
    {
        if (string.IsNullOrWhiteSpace(candidateValue))
        {
            return false;
        }

        var expectedValue = rule.TargetValue ?? string.Empty;

        return rule.Operator.ToUpperInvariant() switch
        {
            "==" => string.Equals(candidateValue, expectedValue, StringComparison.OrdinalIgnoreCase),
            "!=" => !string.Equals(candidateValue, expectedValue, StringComparison.OrdinalIgnoreCase),
            "CONTAINS" => candidateValue.Contains(expectedValue, StringComparison.OrdinalIgnoreCase),
            ">" => CompareOrderedValues(candidateValue, expectedValue) is > 0,
            "<" => CompareOrderedValues(candidateValue, expectedValue) is < 0,
            _ => false
        };
    }

    private static int? CompareOrderedValues(string left, string right)
    {
        if (decimal.TryParse(left, NumberStyles.Number, CultureInfo.InvariantCulture, out var leftNumber)
            && decimal.TryParse(right, NumberStyles.Number, CultureInfo.InvariantCulture, out var rightNumber))
        {
            return leftNumber.CompareTo(rightNumber);
        }

        if (DateTimeOffset.TryParse(left, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var leftDate)
            && DateTimeOffset.TryParse(right, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var rightDate))
        {
            return leftDate.CompareTo(rightDate);
        }

        return null;
    }
}
