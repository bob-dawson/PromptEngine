using PromptEngine.Core.Attributes;

namespace PromptEngine.Sample.Contexts;

/// <summary>
/// Summarize Prompt 的上下文
/// </summary>
[PromptContext("Prompts/Summarize.prompt.txt", TemplateName = "Summarize")]
public class SummarizeContext
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 输入文本
 /// </summary>
    public string InputText { get; set; } = string.Empty;

    /// <summary>
    /// 最大词数
    /// </summary>
  public string MaxWords { get; set; } = "100";

    /// <summary>
    /// 附加指令
    /// </summary>
    public string Instructions { get; set; } = "Focus on key points";
}
