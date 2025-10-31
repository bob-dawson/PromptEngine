namespace PromptEngine.Core.Attributes;

/// <summary>
/// Marks a class as a prompt context associated with a template
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PromptContextAttribute : Attribute
{
    /// <summary>
    /// Template file path declared on the attribute
    /// </summary>
    public string TemplatePath { get; }

    /// <summary>
    /// Template name (optional, defaults to the class name)
    /// </summary>
    public string? TemplateName { get; set; }

    public PromptContextAttribute(string templatePath)
    {
        TemplatePath = templatePath ?? throw new ArgumentNullException(nameof(templatePath));
    }
}
