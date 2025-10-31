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
 /// List of placeholders in the template
 /// </summary>
 public List<string> Placeholders { get; set; } = new();

 /// <summary>
 /// List of context property names
 /// </summary>
 public List<string> ContextProperties { get; set; } = new();

 /// <summary>
 /// Template content (optional)
 /// </summary>
 public string? TemplateContent { get; set; }
}
