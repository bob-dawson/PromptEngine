namespace PromptEngine.Editor.Models;

/// <summary>
/// Editor metadata model
/// </summary>
public class EditorMetadata
{
    /// <summary>
    /// Template name
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Template path
    /// </summary>
    public string TemplatePath { get; set; } = string.Empty;

    /// <summary>
    /// List of available placeholders
    /// </summary>
    public List<VariableInfo> AvailableVariables { get; set; } = new();

    /// <summary>
    /// Context type name
    /// </summary>
    public string ContextTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Variable information
/// </summary>
public class VariableInfo
{
    /// <summary>
    /// Variable name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Variable type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Variable description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Is required
    /// </summary>
    public bool IsRequired { get; set; } = true;
}
