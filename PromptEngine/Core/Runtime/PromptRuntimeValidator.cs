using System.Reflection;
using System.Text.Json;
using PromptEngine.Core.Models;
using PromptEngine.Core.Parsers;

namespace PromptEngine.Core.Runtime;

/// <summary>
/// Runtime validator for prompt templates and metadata
/// </summary>
public class PromptRuntimeValidator
{
    private readonly List<PromptMetadata> _metadata = new();

    public IReadOnlyList<PromptMetadata> GetLoadedMetadata() => _metadata.AsReadOnly();

    public void LoadMetadataFromJson(string metadataFilePath)
    {
        var json = File.ReadAllText(metadataFilePath);
        var metadata = JsonSerializer.Deserialize<List<PromptMetadata>>(json);
        if (metadata != null)
        {
            _metadata.AddRange(metadata);
        }
    }

    public void LoadMetadata(IEnumerable<PromptMetadata> items)
    {
        if (items == null) return;
        _metadata.AddRange(items);
    }

    public void LoadMetadataFromAssembly(string assemblyPath)
    {
        var asm = Assembly.LoadFrom(assemblyPath);
        var registryType = asm.GetType("PromptEngine.Generated.PromptMetadataRegistry", throwOnError: false);
        if (registryType == null) return;

        var allProp = registryType.GetProperty("All", BindingFlags.Public | BindingFlags.Static);
        if (allProp == null) return;
        var value = allProp.GetValue(null);
        if (value is System.Collections.IEnumerable list)
        {
            foreach (var item in list)
            {
                var md = MapMetadata(item);
                if (md != null) _metadata.Add(md);
            }
        }
    }

    public void LoadMetadataFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Directory not found: {directory}");

        // Try JSON files for compatibility
        var jsonFiles = Directory.GetFiles(directory, "*.prompt.meta.json", SearchOption.AllDirectories)
        .Concat(Directory.GetFiles(directory, "*.g.json", SearchOption.AllDirectories));
        foreach (var file in jsonFiles)
        {
            try { LoadMetadataFromJson(file); } catch { /* ignore */ }
        }

        // Try to load from assemblies that contain generated registry
        var assemblies = Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories)
        .Concat(Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories));
        foreach (var asmPath in assemblies)
        {
            try { LoadMetadataFromAssembly(asmPath); } catch { /* ignore */ }
        }
    }

    public ValidationResult ValidateAll()
    {
        var result = new ValidationResult();
        foreach (var meta in _metadata)
        {
            var r = ValidateTemplate(meta);
            if (!r.IsValid) result.Errors.AddRange(r.Errors);
            result.Warnings.AddRange(r.Warnings);
        }
        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    public ValidationResult ValidateTemplate(PromptMetadata metadata)
    {
        var result = new ValidationResult();
        if (!File.Exists(metadata.TemplatePath))
        {
            result.Errors.Add($"Template file not found: {metadata.TemplatePath}");
            result.IsValid = false;
            return result;
        }

        var templateContent = File.ReadAllText(metadata.TemplatePath);
        var actualPlaceholders = PromptTemplateParser.ExtractPlaceholders(templateContent);

        var contextProperties = new HashSet<string>(metadata.ContextProperties, StringComparer.OrdinalIgnoreCase);
        var (isValid, missing, unused) = PromptTemplateParser.ValidateTemplate(actualPlaceholders, contextProperties);

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

    private static PromptMetadata? MapMetadata(object src)
    {
        try
        {
            var t = src.GetType();
            var md = new PromptMetadata
            {
                TemplateName = t.GetProperty("TemplateName")?.GetValue(src)?.ToString() ?? string.Empty,
                TemplatePath = t.GetProperty("TemplatePath")?.GetValue(src)?.ToString() ?? string.Empty,
                ContextTypeName = t.GetProperty("ContextTypeName")?.GetValue(src)?.ToString() ?? string.Empty,
                TemplateContent = t.GetProperty("TemplateContent")?.GetValue(src)?.ToString()
            };

            var placeholders = t.GetProperty("Placeholders")?.GetValue(src) as System.Collections.IEnumerable;
            if (placeholders != null)
            {
                foreach (var p in placeholders)
                    md.Placeholders.Add(p?.ToString() ?? string.Empty);
            }
            var props = t.GetProperty("ContextProperties")?.GetValue(src) as System.Collections.IEnumerable;
            if (props != null)
            {
                foreach (var p in props)
                    md.ContextProperties.Add(p?.ToString() ?? string.Empty);
            }
            return md;
        }
        catch { return null; }
    }
}
