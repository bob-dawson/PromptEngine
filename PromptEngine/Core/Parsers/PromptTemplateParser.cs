using System.Text.RegularExpressions;

namespace PromptEngine.Core.Parsers;

/// <summary>
/// Prompt template parser
/// </summary>
public static class PromptTemplateParser
{
    private static readonly Regex PlaceholderRegex = new(@"\{(\w+)\}", RegexOptions.Compiled);

    /// <summary>
    /// Validate that placeholders in template match context properties
    /// </summary>
    public static (bool IsValid, List<string> MissingProperties, List<string> UnusedProperties)
        ValidateTemplate(HashSet<string> placeholders, HashSet<string> contextProperties)
    {
        var missingProperties = new List<string>();
        var unusedProperties = new List<string>();

        // Check that each placeholder has a corresponding context property
        foreach (var placeholder in placeholders)
        {
            if (!contextProperties.Contains(placeholder, StringComparer.OrdinalIgnoreCase))
            {
                missingProperties.Add(placeholder);
            }
        }

        // Find context properties that are not used in the template
        foreach (var property in contextProperties)
        {
            if (!placeholders.Contains(property, StringComparer.OrdinalIgnoreCase))
            {
                unusedProperties.Add(property);
            }
        }

        bool isValid = missingProperties.Count == 0;
        return (isValid, missingProperties, unusedProperties);
    }

    /// <summary>
    /// Replace placeholders in the template with provided values
    /// </summary>
    public static string ReplacePlaceholders(string template, Dictionary<string, string?> values)
    {
        return PlaceholderRegex.Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            if (values.TryGetValue(key, out var value))
            {
                return value ?? string.Empty;
            }
            return match.Value; // keep original
        });
    }
}
