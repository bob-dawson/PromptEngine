using PromptEngine.Core.Models;
using PromptEngine.Core.Parsers;
using System.Collections.Concurrent;

namespace PromptEngine.Core.Runtime;

/// <summary>
/// Central runtime registry for prompt metadata. Thread-safe and process-wide.
/// </summary>
public static class PromptMetadataRegistry
{
    // Key: ContextTypeName + "::" + TemplateName
    private static readonly ConcurrentDictionary<string, PromptMetadata> _items = new(StringComparer.Ordinal);

    private static string KeyOf(string contextTypeName, string templateName)
    => $"{contextTypeName}::{templateName}";

    /// <summary>
    /// Get a snapshot of all registered metadata.
    /// </summary>
    public static IReadOnlyList<PromptMetadata> All
    => _items.Values.ToList().AsReadOnly();

    /// <summary>
    /// Returns true if a metadata entry with the given context and template name is already registered.
    /// </summary>
    public static bool IsRegistered(string contextTypeName, string templateName)
    => _items.ContainsKey(KeyOf(contextTypeName, templateName));

    /// <summary>
    /// Try get a metadata entry by context type name and template name.
    /// </summary>
    public static bool TryGet(string contextTypeName, string templateName, out PromptMetadata? metadata)
    {
        var ok = _items.TryGetValue(KeyOf(contextTypeName, templateName), out var value);
        metadata = value;
        return ok;
    }

    /// <summary>
    /// Register or update a metadata entry. If an entry with the same key exists it will be replaced.
    /// </summary>
    public static void Register(PromptMetadata metadata)
    {
        if (metadata == null) return;
        var key = KeyOf(metadata.ContextTypeName, metadata.TemplateName);
        _items.AddOrUpdate(key, metadata, (_, __) => metadata);
    }

    public static void LoadPromptFromDir(string directory)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Directory not found: {directory}");

        foreach (var meta in All)
        {
            var templatePath = Path.Combine(directory, meta.TemplatePath);
            if (File.Exists(templatePath))
            {
                meta.TemplateContent = File.ReadAllText(templatePath);
            }
        }

    }
}
