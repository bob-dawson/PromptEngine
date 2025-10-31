using Microsoft.Extensions.DependencyInjection;
using PromptEngine.Agent.Abstractions;
using PromptEngine.Agent.Services;

namespace PromptEngine.Agent.Extensions;

/// <summary>
/// Extensions for registering PromptEngine services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add PromptEngine services
    /// </summary>
    public static IServiceCollection AddPromptEngine(this IServiceCollection services)
    {
        services.AddSingleton<IPromptManager, PromptManager>();
        return services;
    }

    /// <summary>
    /// Add PromptEngine services with configuration
    /// </summary>
    public static IServiceCollection AddPromptEngine(
        this IServiceCollection services,
        Action<IPromptManager> configure)
    {
        services.AddSingleton<IPromptManager>(sp =>
        {
            var manager = new PromptManager();
            configure(manager);
            return manager;
        });

        return services;
    }
}
