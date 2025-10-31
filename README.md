# PromptEngine - Prompt Engineering Framework for C#

A comprehensive prompt engineering framework for C# Agent/LLM development with compile-time validation, runtime safety checks, and editor intelligence.

## Features

- Compile-time validation (Analyzer) - Catch template errors during build
- Runtime validation - Verify template changes in production or CI
- Editor hints - IDE support with variable autocomplete
- Type-safe - Strong typing for context and templates
- Agent Framework integration - Works with Microsoft Agent Framework and Semantic Kernel
- Multi-template support - Manage multiple prompts in one project
- CLI tools - Validate and manage templates from command line

## Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| `PromptEngine` | Unified runtime library: common models, parsers, runtime validation, Agent integration, and editor hint generation | [![NuGet](https://img.shields.io/nuget/v/PromptEngine.svg)](https://www.nuget.org/packages/PromptEngine/) |
| `PromptEngine.Analyzer` | Roslyn analyzer + source generator (compile-time) | [![NuGet](https://img.shields.io/nuget/v/PromptEngine.Analyzer.svg)](https://www.nuget.org/packages/PromptEngine.Analyzer/) |
| `PromptEngine.Tools` | CLI validation tool | [![NuGet](https://img.shields.io/nuget/v/PromptEngine.Tools.svg)](https://www.nuget.org/packages/PromptEngine.Tools/) |

## Quick Start

###1. Install Packages

```bash
# Unified runtime (models, parsers, runtime validation, agent integration, editor support)
dotnet add package PromptEngine

# Analyzer + source generator (compile-time validation and builder generation)
dotnet add package PromptEngine.Analyzer
```

Add your prompt files to the project as additional files so the generator can find them. 
1. You can use Visual Studio to set each prompt file build action as `C# analyzer additional file` in the property window or
2. You can edit the project file:

```xml
<ItemGroup>
 <AdditionalFiles Include="Prompts/**/*.prompt.md" />
</ItemGroup>
```
Then all files under `Prompts/` with `.prompt.md` extension will be included.

###2. Create a Prompt Template (Markdown)

Create a file `Prompts/Summarize.prompt.md`:

```markdown
# Summarize Request for {UserName}

## Input

```text
{InputText}
```

## Requirements
- Provide a concise summary in {MaxWords} words or less.
- Focus on key ideas and main points.

## Additional Instructions
{Instructions}
```

###3. Define Context Class

```csharp
using PromptEngine.Core.Attributes;

[PromptContext("Prompts/Summarize.prompt.md", TemplateName = "Summarize")]
public class SummarizeContext
{
 public string UserName { get; set; } = string.Empty;
 public string InputText { get; set; } = string.Empty;
 public string MaxWords { get; set; } = "100";
 public string Instructions { get; set; } = "Focus on key points";
}
```

###4. Build and Use

The source generator (in `PromptEngine.Analyzer`) creates a `SummarizePromptBuilder` class:

```csharp
var context = new SummarizeContext
{
 UserName = "Alice",
 InputText = "AI is transforming industries...",
 MaxWords = "50",
 Instructions = "Keep it neutral"
};

// Use the generated builder
string prompt = context.BuildSummarizePrompt();
Console.WriteLine(prompt);
```

## Compile-time Validation

With the analyzer/source-generator provided by `PromptEngine.Analyzer`, template issues are reported at build time.

If your template uses undefined placeholders, you'll get a compile error:

```text
error PE003: Template uses undefined placeholder '{UndefinedVariable}' not found in context class 'SummarizeContext'
```

If you have unused properties in your context, you'll get an info diagnostic:

```text
info PE004: Context property 'UnusedProperty' in class 'SummarizeContext' is not used in template
```

## Runtime Validation

Validate templates at runtime or in CI/CD using the unified `PromptEngine` package:

```csharp
using PromptEngine.Core.Runtime;

var validator = new PromptRuntimeValidator();
validator.LoadMetadataFromDirectory("./bin/Debug/net10.0");

var result = validator.ValidateAll();

if (!result.IsValid)
{
 Console.WriteLine(result.ToString());
 throw new InvalidOperationException("Template validation failed");
}
```

You can also load metadata from a specific assembly that contains generated metadata:

```csharp
var validator = new PromptRuntimeValidator();
validator.LoadMetadataFromAssembly("./bin/Debug/net10.0/YourProject.dll");
```

## CLI Tool

Install the global tool:

```bash
dotnet tool install -g PromptEngine.Tools
```

Validate prompts:

```bash
promptengine validate ./bin/Debug/net10.0
promptengine list ./bin/Debug/net10.0
```

## Agent Framework Integration

Register with dependency injection:

```csharp
using PromptEngine.Agent.Extensions;

var builder = Host.CreateDefaultBuilder(args)
 .ConfigureServices((context, services) =>
 {
 services.AddPromptEngine();
 });
```

Use in your agents:

```csharp
public class MyAgent
{
 private readonly IPromptManager _promptManager;

 public MyAgent(IPromptManager promptManager)
 {
 _promptManager = promptManager;
 }

 public async Task SummarizeAsync(string text)
 {
 var context = new SummarizeContext
 {
 UserName = "User",
 InputText = text,
 MaxWords = "100",
 Instructions = "Keep tone helpful"
 };

 string prompt = SummarizePromptBuilder.Build(context);
 // Use prompt with your LLM...
 }
}
```

## Editor Support

Generate editor hints for IDE autocomplete using metadata discovered at runtime:

```csharp
using PromptEngine.Core.Runtime;
using PromptEngine.Editor.Generators;

var validator = new PromptRuntimeValidator();
validator.LoadMetadataFromDirectory("./bin/Debug/net10.0");
var metadata = validator.GetLoadedMetadata().ToList();

var generator = new EditorHintGenerator();
int files = generator.SaveEditorHints(metadata, "./editor-hints");

// Outputs:
// - prompt-hints.json (for generic editors)
// - prompt-types.d.ts (for TypeScript-enabled editors)
```

## Project Structure

```
YourProject/
├── Prompts/
│ ├── Summarize.prompt.md
│ └── Translate.prompt.md
├── Contexts/
│ ├── SummarizeContext.cs
│ └── TranslateContext.cs
└── Program.cs
```

Add this to your project file to include prompt templates for the generator:

```xml
<ItemGroup>
 <AdditionalFiles Include="Prompts/**/*.prompt.md" />
</ItemGroup>
```

## Advanced Features

### Multiple Templates

```csharp
[PromptContext("Prompts/Summarize.prompt.md", TemplateName = "Summarize")]
public class SummarizeContext { /* ... */ }

[PromptContext("Prompts/Translate.prompt.md", TemplateName = "Translate")]
public class TranslateContext { /* ... */ }
```

### Custom Template Path Resolution

Templates are searched by the analyzer among MSBuild AdditionalFiles; prefer providing relative paths under your project and include them via `<AdditionalFiles />`.

### Metadata Access

Generated metadata is exposed at runtime via the C# registry type `PromptEngine.Generated.PromptMetadataRegistry`. The CLI discovers it automatically. To access it yourself:

```csharp
using PromptEngine.Core.Runtime;

var validator = new PromptRuntimeValidator();
validator.LoadMetadataFromAssembly("./bin/Debug/net10.0/YourProject.dll");
var metadata = validator.GetLoadedMetadata();
```

## Troubleshooting

- Analyzer does not find templates
 - Make sure your `.prompt.md` files are included via `<AdditionalFiles />` and paths in `PromptContext` match (relative paths are recommended). The generator (in `PromptEngine.Analyzer`) discovers these files at compile time.
- CLI shows "No metadata found"
 - Ensure you built the project first, and that the DLL you point to (or its output folder) contains the generated registry. Running `promptengine list ./bin/Debug/net10.0` after `dotnet build` should list templates.
- Placeholders not replaced at runtime
 - Confirm the context properties are public and names match exactly (case-insensitive). The analyzer enforces this at compile-time via `PE003` / `PE004`.

## Requirements

- .NET8.0 or later

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## License

MIT License - see LICENSE file for details

## Links

- Documentation: https://github.com/yourorg/promptengine/wiki
- Sample Projects: ./PromptEngine.Sample
- Issue Tracker: https://github.com/yourorg/promptengine/issues

## Support

For questions and support:
- GitHub Issues: https://github.com/yourorg/promptengine/issues
- Email: support@yourcompany.com

---

Made with love for the C# AI/Agent community
