using System.Text;
using System.Text.Json;
using PromptEngine.Core.Models;
using PromptEngine.Editor.Models;
using PromptEngine.Core.Runtime;

namespace PromptEngine.Editor.Generators;

/// <summary>
/// Editor hint generator
/// </summary>
public class EditorHintGenerator
{
    /// <summary>
    /// Generate editor metadata from PromptMetadata
    /// </summary>
    public List<EditorMetadata> GenerateEditorMetadata(List<PromptMetadata> promptMetadata)
    {
        var editorMetadata = new List<EditorMetadata>();

        foreach (var meta in promptMetadata)
        {
            var editor = new EditorMetadata
            {
                TemplateName = meta.TemplateName,
                TemplatePath = meta.TemplatePath,
                ContextTypeName = meta.ContextTypeName,
                AvailableVariables = meta.ContextProperties.Select(p => new VariableInfo
                {
                    Name = p,
                    Type = "string", // default type, actual type should be obtained from type info
                    IsRequired = meta.Placeholders.Contains(p, StringComparer.OrdinalIgnoreCase)
                }).ToList()
            };

            editorMetadata.Add(editor);
        }

        return editorMetadata;
    }

    /// <summary>
    /// Generate editor metadata from the currently registered runtime metadata.
    /// </summary>
    public List<EditorMetadata> GenerateEditorMetadataFromRegistry()
    {
        var all = PromptMetadataRegistry.All.ToList();
        return GenerateEditorMetadata(all);
    }

    /// <summary>
    /// Generate editor hint file in JSON format
    /// </summary>
    public string GenerateJsonHints(List<PromptMetadata> promptMetadata)
    {
        var editorMetadata = GenerateEditorMetadata(promptMetadata);

        return JsonSerializer.Serialize(editorMetadata, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Generate JSON hints from the currently registered runtime metadata.
    /// </summary>
    public string GenerateJsonHintsFromRegistry()
    {
        var all = PromptMetadataRegistry.All.ToList();
        return GenerateJsonHints(all);
    }

    /// <summary>
    /// Generate TypeScript type definitions
    /// </summary>
    public string GenerateTypeScriptDefinitions(List<PromptMetadata> promptMetadata)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// Auto-generated TypeScript definitions for Prompt templates");
        sb.AppendLine("// This file provides type information for editors with TypeScript support");
        sb.AppendLine();

        foreach (var meta in promptMetadata)
        {
            var contextName = GetSimpleTypeName(meta.ContextTypeName);

            sb.AppendLine("/**");
            sb.AppendLine($" * Context for {meta.TemplateName} template");
            sb.AppendLine($" * Template path: {meta.TemplatePath}");
            sb.AppendLine(" */");
            sb.AppendLine($"export interface {contextName} {{");

            foreach (var prop in meta.ContextProperties)
            {
                var isUsed = meta.Placeholders.Contains(prop, StringComparer.OrdinalIgnoreCase);
                var optional = isUsed ? "" : "?";

                sb.AppendLine($" /** {(isUsed ? "Required" : "Optional")} property */");
                sb.AppendLine($" {prop}{optional}: string;");
            }

            sb.AppendLine("}");
            sb.AppendLine();
        }

        // Generate template mapping
        sb.AppendLine("/**");
        sb.AppendLine(" * Available prompt templates");
        sb.AppendLine(" */");
        sb.AppendLine("export interface PromptTemplates {");

        foreach (var meta in promptMetadata)
        {
            var contextName = GetSimpleTypeName(meta.ContextTypeName);
            sb.AppendLine($" '{meta.TemplateName}': {contextName};");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generate TypeScript definitions from the currently registered runtime metadata.
    /// </summary>
    public string GenerateTypeScriptDefinitionsFromRegistry()
    {
        var all = PromptMetadataRegistry.All.ToList();
        return GenerateTypeScriptDefinitions(all);
    }

    /// <summary>
    /// Save editor hint files to the specified directory
    /// </summary>
    public void SaveEditorHints(List<PromptMetadata> promptMetadata, string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // Save JSON file
        var jsonHints = GenerateJsonHints(promptMetadata);
        var jsonPath = Path.Combine(outputDirectory, "prompt-hints.json");
        File.WriteAllText(jsonPath, jsonHints);

        // Save TypeScript definition file
        var tsDefinitions = GenerateTypeScriptDefinitions(promptMetadata);
        var tsPath = Path.Combine(outputDirectory, "prompt-types.d.ts");
        File.WriteAllText(tsPath, tsDefinitions);
    }

    /// <summary>
    /// Save editor hints generated from runtime registry to the specified directory.
    /// </summary>
    public void SaveEditorHintsFromRegistry(string outputDirectory)
    {
        var all = PromptMetadataRegistry.All.ToList();
        SaveEditorHints(all, outputDirectory);
    }

    private string GetSimpleTypeName(string fullTypeName)
    {
        var lastDot = fullTypeName.LastIndexOf('.');
        return lastDot >= 0 ? fullTypeName.Substring(lastDot + 1) : fullTypeName;
    }
}
