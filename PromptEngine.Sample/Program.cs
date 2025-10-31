using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PromptEngine.Agent.Extensions;
using PromptEngine.Core.Runtime;
using PromptEngine.Sample.Contexts;

namespace PromptEngine.Sample;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== PromptEngine Sample Application ===\n");

        // Create Host and configure services
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register PromptEngine services
                services.AddPromptEngine();
            })
            .Build();

        // Run samples
        if (!await RunSamples(host.Services))
            return;

        await host.RunAsync();
    }

    static async Task<bool> RunSamples(IServiceProvider services)
    {
        //call generated register method to register all prompts
        PromptEngineRegister.Register();

        //runtime load prompts from directory
        //you can access PromptMetadataRegistry.All and load prompt from other source like database
        PromptMetadataRegistry.LoadPromptFromDir("Prompts");

        //runtime validate all registered prompts
        var result = PromptRuntimeValidator.ValidateAll();
        if (!result.IsValid)
        {
            Console.WriteLine("Prompt validation failed:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"- {error}");
            }
            return false;
        }

        Console.WriteLine("1. Summarize Prompt Example");
        Console.WriteLine("--------------------------------");      

        // Example1: Summarize
        var summarizeContext = new SummarizeContext
        {
            UserName = "Alice",
            InputText = "Artificial Intelligence (AI) is revolutionizing the way we work and live. " +
                "From healthcare to transportation, AI systems are being deployed across various industries. " +
                "Machine learning, a subset of AI, enables computers to learn from data without being explicitly programmed. " +
                "Deep learning, a more advanced form of machine learning, uses neural networks to process complex patterns.",
            MaxWords = "50",
            Instructions = "Focus on the key concepts and applications"
        };
        // Use the generated Prompt Builder
        var summarizePrompt = summarizeContext.BuildSummarizePrompt();
        Console.WriteLine(summarizePrompt);
        Console.WriteLine("\n");

        // Example2: Translate
        Console.WriteLine("2. Translate Prompt Example");
        Console.WriteLine("--------------------------------");

        var translateContext = new TranslateContext
        {
            SourceLanguage = "English",
            TargetLanguage = "Chinese",
            Text = "Hello, how are you today?",
            Style = "casual",
            UserName = "Bob"
        };

        var translatePrompt = TranslatePromptBuilder.Build(translateContext);
        Console.WriteLine(translatePrompt);
        Console.WriteLine("\n");

        // Example3: Multiple templates for one context
        Console.WriteLine("3. Translate With User Prompt Example (same context, different template)");
        Console.WriteLine("--------------------------------");
        var translateWithUserPrompt = TranslateWithUserPromptBuilder.Build(translateContext);
        Console.WriteLine(translateWithUserPrompt);
        Console.WriteLine("\n");

        // Example4: Get template content
        Console.WriteLine("4. Template Content Example");
        Console.WriteLine("--------------------------------");
        Console.WriteLine("Summarize Template:");
        Console.WriteLine(SummarizePromptBuilder.GetTemplate());
        Console.WriteLine("\n");

        return true;
    }
}
