using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using PromptEngine.Core.Models;
using PromptEngine.Core.Parsers;
using Stubble.Core.Parser;
using Stubble.Core.Tokens;

namespace PromptEngine.Core.Runtime;

/// <summary>
/// Runtime validator for prompt templates and metadata
/// </summary>
public static class PromptRuntimeValidator
{
    public static ValidationResult ValidateAll()
    {
        var result = new ValidationResult();
        foreach (var meta in PromptMetadataRegistry.All)
        {
            var r = ValidateTemplate(meta);
            if (!r.IsValid)
                result.Errors.AddRange(r.Errors);
            result.Warnings.AddRange(r.Warnings);
        }
        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    public static ValidationResult ValidateTemplate(PromptMetadata metadata)
    {
        var result = new ValidationResult();

        // Check if template file exists
        if (!File.Exists(metadata.TemplatePath))
        {
            result.Errors.Add($"Template file not found: {metadata.TemplatePath}");
            result.IsValid = false;
            return result;
        }

        // Load template content
        var templateContent = File.ReadAllText(metadata.TemplatePath);

        // Resolve context type using reflection
        var contextType = ResolveType(metadata.ContextTypeName);
        if (contextType == null)
        {
            result.Errors.Add($"Context type '{metadata.ContextTypeName}' could not be resolved for template '{metadata.TemplateName}'");
            result.IsValid = false;
            return result;
        }

        // Validate template using reflection-based validator
        try
        {
            var errors = ValidateTemplateWithReflection(templateContent, contextType);
            result.Errors.AddRange(errors.Select(e => $"Template '{metadata.TemplateName}': {e}"));
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to validate template '{metadata.TemplateName}': {ex.Message}");
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    /// <summary>
    /// Resolve a type by its fully qualified name from all loaded assemblies
    /// </summary>
    private static Type? ResolveType(string typeName)
    {
        // First try direct Type.GetType
        var type = Type.GetType(typeName);
        if (type != null)
            return type;

        // Search in all loaded assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
                return type;
        }

        return null;
    }

    /// <summary>
    /// Validate template using reflection-based type checking
    /// </summary>
    private static List<string> ValidateTemplateWithReflection(string template, Type contextType)
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
                ValidateNode(item, contextType, errors, contextType);
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse template: {ex.Message}");
        }

        return errors;
    }

    private static void ValidateNode(MustacheToken node, Type currentType, List<string> errors, Type rootType)
    {
        var path = GetPathStringFromToken(node);

        if (!string.IsNullOrEmpty(path))
        {
            if (node is SectionToken)
            {
                // For sections, validate and potentially change context
                var sectionType = GetPathType(path, rootType, out var isValid);
                if (!isValid || sectionType == null)
                {
                    errors.Add($"Property path '{path}' not found in type {rootType.Name}");
                }
                else
                {
                    // For sections, check if it's enumerable
                    var elementType = GetEnumerableElementType(sectionType);
                    var childContextType = elementType ?? sectionType;

                    // Validate children with the section's context type
                    var children = GetChildrenFromToken(node);
                    if (children != null && children.Count > 0)
                    {
                        foreach (var child in children)
                        {
                            ValidateNode(child, childContextType, errors, childContextType);
                        }
                    }
                }
            }
            else if (node is InterpolationToken || node is InvertedSectionToken)
            {
                // For interpolations and inverted sections, just validate the path
                ValidatePath(path, rootType, errors);

                // For inverted sections, validate children with same context
                if (node is InvertedSectionToken)
                {
                    var children = GetChildrenFromToken(node);
                    if (children != null && children.Count > 0)
                    {
                        foreach (var child in children)
                        {
                            ValidateNode(child, currentType, errors, rootType);
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
                    ValidateNode(child, currentType, errors, rootType);
                }
            }
        }
    }

    private static void ValidatePath(string path, Type currentType, List<string> errors)
    {
        var segments = path.Split('.');
        Type? type = currentType;

        foreach (var seg in segments)
        {
            if (type == null) break;

            if (TryGetMemberType(type, seg, out Type? memberType))
            {
                type = memberType;
            }
            else
            {
                errors.Add($"Property path '{path}' not found in type {currentType.Name}");
                return;
            }
        }
    }

    private static Type? GetPathType(string path, Type currentType, out bool isValid)
    {
        var segments = path.Split('.');
        Type? type = currentType;
        isValid = true;

        foreach (var seg in segments)
        {
            if (type == null)
            {
                isValid = false;
                return null;
            }

            if (TryGetMemberType(type, seg, out Type? memberType))
            {
                type = memberType;
            }
            else
            {
                isValid = false;
                return null;
            }
        }

        return type;
    }

    private static bool TryGetMemberType(Type type, string name, out Type? memberType)
    {
        // Try to find property or field with case-insensitive match
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

        // Try property first
        var property = type.GetProperty(name, bindingFlags);
        if (property != null)
        {
            memberType = property.PropertyType;
            return true;
        }

        // Try field
        var field = type.GetField(name, bindingFlags);
        if (field != null)
        {
            memberType = field.FieldType;
            return true;
        }

        memberType = null;
        return false;
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        // Check if it's an array
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        // Check if it implements IEnumerable<T>
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();

            // Check for common collection types
            if (genericTypeDef == typeof(IEnumerable<>) ||
             genericTypeDef == typeof(ICollection<>) ||
     genericTypeDef == typeof(IList<>) ||
             genericTypeDef == typeof(List<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        // Check implemented interfaces
        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return iface.GetGenericArguments()[0];
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
