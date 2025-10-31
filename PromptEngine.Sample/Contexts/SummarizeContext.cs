using PromptEngine.Core.Attributes;

namespace PromptEngine.Sample.Contexts;

/// <summary>
/// Summarize Prompt Context
/// </summary>
[PromptContext("Prompts/Summarize.prompt.md", TemplateName = "Summarize")]
public class SummarizeContext
{
    /// <summary>
    /// User Name
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Input Text
    /// </summary>
    public string InputText { get; set; } = string.Empty;

    /// <summary>
    /// Max Words
    /// </summary>
    public string MaxWords { get; set; } = "100";

    /// <summary>
    /// Instructions
    /// </summary>
    public string Instructions { get; set; } = "Focus on key points";
}
