using System.Globalization;
using Itransition.Models.Attributes;

namespace Itransition.Services;

public static class PositionAccessRulePolicy
{
    private const int MaxTargetValueLength = 500;

    private static readonly string[] TextOperators = ["==", "!=", "CONTAINS"];
    private static readonly string[] OrderedOperators = ["==", "!=", ">", "<"];
    private static readonly string[] EqualityOperators = ["==", "!="];

    /// <summary>
    /// Returns the operators supported by an attribute type. Unknown enum values are restricted to equality checks.
    /// </summary>
    public static IReadOnlyList<string> GetAllowedOperators(AttributeDataType dataType)
    {
        return dataType switch
        {
            AttributeDataType.String or AttributeDataType.MarkdownText => TextOperators,
            AttributeDataType.Numeric or AttributeDataType.Date => OrderedOperators,
            AttributeDataType.ImageReference
                or AttributeDataType.Period
                or AttributeDataType.Boolean
                or AttributeDataType.Dropdown => EqualityOperators,
            _ => EqualityOperators
        };
    }

    /// <summary>
    /// Normalizes an operator received from a form before it is validated or persisted.
    /// </summary>
    public static string NormalizeOperator(string? operatorType)
    {
        return operatorType?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    /// <summary>
    /// Validates and normalizes a rule target. Empty, oversized, malformed, or unknown dropdown values are rejected.
    /// </summary>
    public static bool TryNormalizeTargetValue(
        AttributeDefinition definition,
        string? rawValue,
        out string normalizedValue,
        out string errorMessage)
    {
        normalizedValue = rawValue?.Trim() ?? string.Empty;
        errorMessage = string.Empty;

        if (normalizedValue.Length == 0)
        {
            errorMessage = "Enter a target value for the access rule.";
            return false;
        }

        if (normalizedValue.Length > MaxTargetValueLength)
        {
            errorMessage = $"The target value cannot exceed {MaxTargetValueLength} characters.";
            return false;
        }

        switch (definition.DataType)
        {
            case AttributeDataType.Numeric:
                if (!decimal.TryParse(
                        normalizedValue,
                        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture,
                        out var number))
                {
                    errorMessage = "Enter a valid number using a dot as the decimal separator.";
                    return false;
                }

                normalizedValue = number.ToString("G29", CultureInfo.InvariantCulture);
                return true;

            case AttributeDataType.Date:
                if (!DateOnly.TryParseExact(
                        normalizedValue,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var date))
                {
                    errorMessage = "Enter a valid date.";
                    return false;
                }

                normalizedValue = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                return true;

            case AttributeDataType.Boolean:
                if (!bool.TryParse(normalizedValue, out var booleanValue))
                {
                    errorMessage = "Select either True or False.";
                    return false;
                }

                normalizedValue = booleanValue ? "true" : "false";
                return true;

            case AttributeDataType.Dropdown:
                var submittedValue = normalizedValue;
                var matchingOption = definition.Options.FirstOrDefault(option =>
                    string.Equals(option.Value, submittedValue, StringComparison.OrdinalIgnoreCase));

                if (matchingOption is null)
                {
                    errorMessage = definition.Options.Count == 0
                        ? "This dropdown attribute has no configured options."
                        : "Select one of the configured dropdown options.";
                    return false;
                }

                normalizedValue = matchingOption.Value;
                return true;

            default:
                return true;
        }
    }
}
