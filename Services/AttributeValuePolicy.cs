using System.Globalization;
using Itransition.Models.Attributes;

namespace Itransition.Services;

public static class AttributeValuePolicy
{
    public static bool TryNormalize(
        AttributeDefinition definition,
        string? rawValue,
        out string? normalizedValue,
        out string? error)
    {
        normalizedValue = string.IsNullOrWhiteSpace(rawValue) ? null : rawValue.Trim();
        error = null;

        if (normalizedValue is null)
        {
            return true;
        }

        var builtInMaximumLength = definition.BuiltInKey switch
        {
            BuiltInAttributeKeys.FirstName or BuiltInAttributeKeys.LastName => 100,
            BuiltInAttributeKeys.Location => 200,
            _ => (int?)null
        };
        if (builtInMaximumLength.HasValue && normalizedValue.Length > builtInMaximumLength.Value)
        {
            error = $"{definition.Name} cannot exceed {builtInMaximumLength.Value} characters.";
            return false;
        }

        switch (definition.DataType)
        {
            case AttributeDataType.Numeric:
                if (!decimal.TryParse(
                        normalizedValue,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out var number))
                {
                    error = $"{definition.Name} must be a valid number.";
                    return false;
                }

                normalizedValue = number.ToString(CultureInfo.InvariantCulture);
                return true;

            case AttributeDataType.Date:
                if (!DateOnly.TryParse(
                        normalizedValue,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var date))
                {
                    error = $"{definition.Name} must be a valid date.";
                    return false;
                }

                normalizedValue = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                return true;

            case AttributeDataType.Boolean:
                if (!bool.TryParse(normalizedValue, out var booleanValue))
                {
                    error = $"{definition.Name} must be Yes or No.";
                    return false;
                }

                normalizedValue = booleanValue ? "true" : "false";
                return true;

            case AttributeDataType.Dropdown:
                var submittedValue = normalizedValue;
                var option = definition.Options.FirstOrDefault(item =>
                    string.Equals(item.Value, submittedValue, StringComparison.OrdinalIgnoreCase));
                if (option is null)
                {
                    error = $"Select a valid value for {definition.Name}.";
                    return false;
                }

                normalizedValue = option.Value;
                return true;

            case AttributeDataType.ImageReference:
                if (normalizedValue.Length > 2048
                    || !Uri.TryCreate(normalizedValue, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    error = $"{definition.Name} must be a valid HTTP or HTTPS URL.";
                    return false;
                }

                return true;

            case AttributeDataType.MarkdownText:
                if (normalizedValue.Length > 10_000)
                {
                    error = $"{definition.Name} cannot exceed 10,000 characters.";
                    return false;
                }

                return true;

            case AttributeDataType.String:
            case AttributeDataType.Period:
                if (normalizedValue.Length > 1_000)
                {
                    error = $"{definition.Name} cannot exceed 1,000 characters.";
                    return false;
                }

                return true;

            default:
                error = $"{definition.Name} uses an unsupported data type.";
                return false;
        }
    }
}
