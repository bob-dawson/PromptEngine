namespace PromptEngine.Agent.Abstractions;

/// <summary>
/// Prompt manager interface
/// </summary>
public interface IPromptManager
{
    /// <summary>
    /// Register a prompt builder
    /// </summary>
    void RegisterPromptBuilder<TContext>(string templateName, IPromptBuilder<TContext> builder);

    /// <summary>
    /// Get a prompt builder
    /// </summary>
    IPromptBuilder<TContext>? GetPromptBuilder<TContext>(string templateName);

    /// <summary>
    /// Build a prompt
    /// </summary>
    string BuildPrompt<TContext>(string templateName, TContext context);

    /// <summary>
    /// Get all registered template names
    /// </summary>
    IEnumerable<string> GetRegisteredTemplates();
}
