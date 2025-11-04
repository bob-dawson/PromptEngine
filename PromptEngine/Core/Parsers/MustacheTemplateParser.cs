using System;
using System.Collections.Generic;
using System.Linq;
using Stubble.Core.Parser;
using Stubble.Core.Tokens;

namespace PromptEngine.Core.Parsers;

/// <summary>
/// Mustache template parser for runtime dynamic validation and rendering
/// </summary>
public static class MustacheTemplateParser
{
    /// <summary>
    /// Extract all variable paths from a Mustache template (only root-level properties)
    /// </summary>
    public static HashSet<string> ExtractPlaceholders(string templateContent)
    {
        if (string.IsNullOrWhiteSpace(templateContent))
            return new HashSet<string>();

        var placeholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var parser = new InstanceMustacheParser();
            var rootNode = parser.Parse(templateContent);

            // Only process top-level tokens, no recursion into children
            foreach (var token in rootNode.Children)
            {
                var path = GetPathStringFromToken(token);
                if (!string.IsNullOrEmpty(path))
                {
                    // Only extract root property (first segment before dot)
                    var rootProperty = path.Split('.')[0];
                    placeholders.Add(rootProperty);
                }
            }
        }
        catch
        {
            // If parsing fails, return empty set
            return new HashSet<string>();
        }

        return placeholders;
    }

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
            // All placeholders are now root properties only
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

    private static string GetPathStringFromToken(MustacheToken token)
    {
        // Use reflection to get the path/content property from any token type
        var type = token.GetType();

        // Try common property names
        var prop = type.GetProperty("Content") ??
           type.GetProperty("SectionName") ??
     type.GetProperty("ContentToken") ??
          type.GetProperty("Path");

        if (prop != null)
        {
            var value = prop.GetValue(token);
            if (value != null)
            {
                var strValue = value.ToString() ?? string.Empty;

                // Filter out internal type names and invalid paths
                if (string.IsNullOrWhiteSpace(strValue) ||
                strValue.Contains("Stubble.") ||
        strValue.Contains("[]") ||
strValue.Contains("System."))
                {
                    return string.Empty;
                }

                return strValue;
            }
        }

        return string.Empty;
    }
}
