using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PromptEngine.Agent.Extensions;
using PromptEngine.Sample.Contexts;

namespace PromptEngine.Sample;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== PromptEngine Sample Application ===\n");

        // 创建 Host 并配置服务
        var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // 注册 PromptEngine 服务
        services.AddPromptEngine();
    })
     .Build();

        // 运行示例
        await RunSamples(host.Services);

        await host.RunAsync();
    }

    static async Task RunSamples(IServiceProvider services)
    {
        Console.WriteLine("1. Summarize Prompt Example");
        Console.WriteLine("--------------------------------");

        // 示例 1: Summarize
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

        // 使用生成的 Prompt Builder
        var summarizePrompt = SummarizePromptBuilder.Build(summarizeContext);
        Console.WriteLine(summarizePrompt);
        Console.WriteLine("\n");

        // 示例 2: Translate
        Console.WriteLine("2. Translate Prompt Example");
        Console.WriteLine("--------------------------------");

        var translateContext = new TranslateContext
        {
            SourceLanguage = "English",
            TargetLanguage = "Chinese",
            Text = "Hello, how are you today?",
            Style = "casual"
        };

        var translatePrompt = TranslatePromptBuilder.Build(translateContext);
        Console.WriteLine(translatePrompt);
        Console.WriteLine("\n");

        // 示例 3: 获取模板内容
        Console.WriteLine("3. Template Content Example");
        Console.WriteLine("--------------------------------");
        Console.WriteLine("Summarize Template:");
        Console.WriteLine(SummarizePromptBuilder.GetTemplate());
        Console.WriteLine("\n");

        await Task.CompletedTask;
    }
}
