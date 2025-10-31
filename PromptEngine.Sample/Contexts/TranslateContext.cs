using PromptEngine.Core.Attributes;

namespace PromptEngine.Sample.Contexts;

/// <summary>
/// Translate Prompt Context
/// </summary>
[PromptContext("Prompts/Translate.prompt.txt", TemplateName = "Translate")]
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
    /// </summary>
    public string Style { get; set; } = "professional";
}
