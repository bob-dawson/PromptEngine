using PromptEngine.Agent.Abstractions;

namespace PromptEngine.Agent.Services;

/// <summary>
/// Default prompt manager implementation
/// </summary>
public class PromptManager : IPromptManager
{
    private readonly Dictionary<string, object> _builders = new();

    /// <inheritdoc/>
    public void RegisterPromptBuilder<TContext>(string templateName, IPromptBuilder<TContext> builder)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));

        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        _builders[templateName] = builder;
    }

    /// <inheritdoc/>
    public IPromptBuilder<TContext>? GetPromptBuilder<TContext>(string templateName)
    {
        if (_builders.TryGetValue(templateName, out var builder))
        {
            return builder as IPromptBuilder<TContext>;
        }

        return null;
    }

    /// <inheritdoc/>
    public string BuildPrompt<TContext>(string templateName, TContext context)
    {
        var builder = GetPromptBuilder<TContext>(templateName);

        if (builder == null)
            throw new InvalidOperationException($"No prompt builder registered for template: {templateName}");

        return builder.Build(context);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetRegisteredTemplates()
    {
        return _builders.Keys;
    }
}
