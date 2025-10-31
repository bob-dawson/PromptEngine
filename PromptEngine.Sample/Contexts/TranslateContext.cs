using PromptEngine.Core.Attributes;

namespace PromptEngine.Sample.Contexts;

/// <summary>
/// Translate Prompt 的上下文
/// </summary>
[PromptContext("Prompts/Translate.prompt.txt", TemplateName = "Translate")]
public class TranslateContext
{
    /// <summary>
    /// 源语言
    /// </summary>
    public string SourceLanguage { get; set; } = string.Empty;

    /// <summary>
    /// 目标语言
    /// </summary>
    public string TargetLanguage { get; set; } = string.Empty;

    /// <summary>
  /// 要翻译的文本
    /// </summary>
  public string Text { get; set; } = string.Empty;

    /// <summary>
/// 翻译风格
    /// </summary>
    public string Style { get; set; } = "professional";
}
