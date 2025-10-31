namespace PromptEngine.Agent.Abstractions;

/// <summary>
/// Prompt builder interface
/// </summary>
public interface IPromptBuilder<TContext>
{
 /// <summary>
 /// Build prompt from the context
 /// </summary>
 string Build(TContext context);

 /// <summary>
 /// Get the template content
 /// </summary>
 string GetTemplate();

 /// <summary>
 /// Get the template name
 /// </summary>
 string GetTemplateName();
}
