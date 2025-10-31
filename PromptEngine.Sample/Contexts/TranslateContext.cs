using PromptEngine.Core.Attributes;

namespace PromptEngine.Sample.Contexts;

/// <summary>
/// Translate Prompt Context
/// </summary>
[PromptContext("Prompts/Translate.prompt.md", TemplateName = "Translate")]
[PromptContext("Prompts/TranslateWithUser.prompt.md", TemplateName = "TranslateWithUser")] // multiple templates for one context
public class TranslateContext
{
    /// <summary>
    /// Source Language
    /// </summary>
    public string SourceLanguage { get; set; } = string.Empty;

    /// <summary>
    /// Target Language
    /// </summary>
    public string TargetLanguage { get; set; } = string.Empty;

    /// <summary>
    /// Text to Translate
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Translation Style
    /// </summary>
    public string Style { get; set; } = "professional";

    /// <summary>
    /// Optional user display name for prompt personalization
    /// </summary>
    public string UserName { get; set; } = string.Empty;
}
