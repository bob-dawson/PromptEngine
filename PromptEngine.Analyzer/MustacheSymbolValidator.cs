#nullable enable
using Microsoft.CodeAnalysis;
using Stubble.Core.Parser;
using Stubble.Core.Tokens;

namespace PromptEngine.Analyzer;

/// <summary>
/// Static Mustache template validator for compile-time analysis using Roslyn symbols
/// </summary>
public static class MustacheSymbolValidator
{
    /// <summary>
    /// Parse template and validate all variable/section paths exist in contextSymbol.
    /// Returns error list, each error describes an invalid path.
    /// </summary>
    public static List<string> ValidateTemplate(string template, INamedTypeSymbol contextSymbol)
    {
        var errors = new List<string>();

        try
        {
            // Use Stubble's parser
            var parser = new InstanceMustacheParser();
            var rootNode = parser.Parse(template);

            // Traverse AST
            foreach (var item in rootNode.Children)
            {
                ValidateNode(item, contextSymbol, errors, contextSymbol);
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse template: {ex.Message}");
        }

        return errors;
    }

    /// <summary>
    /// Extract root-level placeholder properties from the template (no recursion)
    /// </summary>
    public static HashSet<string> ExtractPlaceholders(string template)
    {
        var placeholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var parser = new InstanceMustacheParser();
            var rootNode = parser.Parse(template);

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
        }

        return placeholders;
    }

    private static void ValidateNode(MustacheToken node, INamedTypeSymbol currentSymbol, List<string> errors, INamedTypeSymbol rootSymbol)
    {
        var path = GetPathStringFromToken(node);

        if (!string.IsNullOrEmpty(path))
        {
            if (node is SectionToken)
            {
                // For sections, validate and potentially change context
                var sectionSymbol = GetPathSymbol(path, rootSymbol, out var isValid);
                if (!isValid || sectionSymbol == null)
                {
                    errors.Add($"Property path '{path}' not found in type {rootSymbol.Name}");
                }
                else
                {
                    // For sections, check if it's enumerable
                    var elementSymbol = GetEnumerableElementSymbol(sectionSymbol);
                    var childContextSymbol = elementSymbol ?? sectionSymbol;

                    // Validate children with the section's context type
                    var children = GetChildrenFromToken(node);
                    if (children != null && children.Count > 0)
                    {
                        foreach (var child in children)
                        {
                            if (childContextSymbol is INamedTypeSymbol namedChildSymbol)
                            {
                                ValidateNode(child, namedChildSymbol, errors, namedChildSymbol);
                            }
                            else
                            {
                                // If not a named type, validate with root context
                                ValidateNode(child, rootSymbol, errors, rootSymbol);
                            }
                        }
                    }
                }
            }
            else if (node is InterpolationToken || node is InvertedSectionToken)
            {
                // For interpolations and inverted sections, just validate the path
                ValidatePath(path, rootSymbol, errors);

                // For inverted sections, validate children with same context
                if (node is InvertedSectionToken)
                {
                    var children = GetChildrenFromToken(node);
                    if (children != null && children.Count > 0)
                    {
                        foreach (var child in children)
                        {
                            ValidateNode(child, currentSymbol, errors, rootSymbol);
                        }
                    }
                }
            }
        }
        else
        {
            // Literal or partial tokens - validate children if any
            var children = GetChildrenFromToken(node);
            if (children != null && children.Count > 0)
            {
                foreach (var child in children)
                {
                    ValidateNode(child, currentSymbol, errors, rootSymbol);
                }
            }
        }
    }

    private static void ValidatePath(string path, INamedTypeSymbol currentSymbol, List<string> errors)
    {
        var segments = path.Split('.');
        ITypeSymbol? symbol = currentSymbol;

        foreach (var seg in segments)
        {
            if (symbol == null) break;

            if (TryGetMemberSymbol(symbol, seg, out ITypeSymbol? memberSymbol))
            {
                symbol = memberSymbol;
            }
            else
            {
                errors.Add($"Property path '{path}' not found in type {currentSymbol.Name}");
                return;
            }
        }
    }

    private static ITypeSymbol? GetPathSymbol(string path, ITypeSymbol currentSymbol, out bool isValid)
    {
        var segments = path.Split('.');
        ITypeSymbol? symbol = currentSymbol;
        isValid = true;

        foreach (var seg in segments)
        {
            if (symbol == null)
            {
                isValid = false;
                return null;
            }

            if (TryGetMemberSymbol(symbol, seg, out ITypeSymbol? memberSymbol))
            {
                symbol = memberSymbol;
            }
            else
            {
                isValid = false;
                return null;
            }
        }

        return symbol;
    }

    private static bool TryGetMemberSymbol(ITypeSymbol typeSymbol, string name, out ITypeSymbol? memberSymbol)
    {
        // Try to find property or field with case-insensitive match
        var members = typeSymbol.GetMembers()
            .Where(m => m.DeclaredAccessibility == Accessibility.Public)
            .Where(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));

        foreach (var member in members)
        {
            switch (member)
            {
                case IPropertySymbol prop:
                    memberSymbol = prop.Type;
                    return true;
                case IFieldSymbol field:
                    memberSymbol = field.Type;
                    return true;
            }
        }

        memberSymbol = null;
        return false;
    }

    private static ITypeSymbol? GetEnumerableElementSymbol(ITypeSymbol typeSymbol)
    {
        // Check if it's an array
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType;
        }

        // Check if it implements IEnumerable<T>
        if (typeSymbol is INamedTypeSymbol namedType)
        {
            if (namedType.IsGenericType)
            {
                var originalDef = namedType.OriginalDefinition;
                var fullName = originalDef.ToDisplayString();

                // Check for common collection types
                if (fullName.StartsWith("System.Collections.Generic.IEnumerable<") ||
                    fullName.StartsWith("System.Collections.Generic.ICollection<") ||
                    fullName.StartsWith("System.Collections.Generic.IList<") ||
                    fullName.StartsWith("System.Collections.Generic.List<"))
                {
                    return namedType.TypeArguments[0];
                }
            }

            // Check implemented interfaces
            foreach (var iface in namedType.AllInterfaces)
            {
                if (iface.IsGenericType)
                {
                    var ifaceOriginal = iface.OriginalDefinition.ToDisplayString();
                    if (ifaceOriginal == "System.Collections.Generic.IEnumerable<T>")
                    {
                        return iface.TypeArguments[0];
                    }
                }
            }
        }

        return null;
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

    private static List<MustacheToken>? GetChildrenFromToken(MustacheToken token)
    {
        // Use reflection to get Children property
        var type = token.GetType();
        var prop = type.GetProperty("Children");

        if (prop != null)
        {
            return prop.GetValue(token) as List<MustacheToken>;
        }

        return null;
    }
}
