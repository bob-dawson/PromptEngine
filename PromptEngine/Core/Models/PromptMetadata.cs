namespace PromptEngine.Core.Models;

/// <summary>
/// Prompt template metadata
/// </summary>
public class PromptMetadata
{
    /// <summary>
    /// Template name
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Template file path
    /// </summary>
    public string TemplatePath { get; set; } = string.Empty;

    /// <summary>
    /// Fully qualified context type name
    /// </summary>
    public string ContextTypeName { get; set; } = string.Empty;
    /// <summary>
    /// Template content (optional)
    /// </summary>
    public string TemplateContent { get; set; } = string.Empty;
}
