using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using PromptEngine.Core.Models;
using PromptEngine.Core.Parsers;

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
        if (!File.Exists(metadata.TemplatePath))
        {
            result.Errors.Add($"Template file not found: {metadata.TemplatePath}");
            result.IsValid = false;
            return result;
        }

        var templateContent = File.ReadAllText(metadata.TemplatePath);
        var actualPlaceholders = MustacheTemplateParser.ExtractPlaceholders(templateContent);

        var contextProperties = new HashSet<string>(metadata.ContextProperties);
        var (isValid, missing, unused) = MustacheTemplateParser.ValidateTemplate(actualPlaceholders, contextProperties);

        if (!isValid)
        {
            foreach (var m in missing)
                result.Errors.Add($"Template '{metadata.TemplateName}' uses undefined placeholder: {{{m}}}");
        }
        foreach (var u in unused)
            result.Warnings.Add($"Context property '{u}' in '{metadata.ContextTypeName}' is not used in template '{metadata.TemplateName}'");

        result.IsValid = result.Errors.Count == 0;
        return result;
    }
}
