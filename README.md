# PromptEngine - Prompt Engineering Framework for C#

A comprehensive prompt engineering framework for C# Agent/LLM development with compile-time validation, code generation and runtime safety checks.

## Features

- Compile-time validation (Analyzer) - Catch template errors during build
- Runtime validation - Verify template changes in production or CI
- Type-safe - Strong typing for context and templates
- **Mustache templating** - Industry-standard template syntax with logic support
- Agent Framework integration - Works with Microsoft Agent Framework and Semantic Kernel
- Multi-template support - Manage multiple prompts in one project

## Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| `PromptEngine` | Complete package including runtime library, Roslyn analyzer and source generator | [![NuGet](https://img.shields.io/nuget/v/PromptEngine.svg)](https://www.nuget.org/packages/PromptEngine/) |

## Quick Start

### 1. Install Package

```bash
# Single package includes everything: runtime library, analyzer, and source generator
dotnet add package PromptEngine
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

### 2. Create a Prompt Template (Markdown)

Create a file `Prompts/Summarize.prompt.md`:

````markdown
# Summarize Request for {{{UserName}}}

## Input

```text
{{{InputText}}}
```

## Requirements
- Provide a concise summary in {{{MaxWords}}} words or less.
- Focus on key ideas and main points.

## Additional Instructions
{{{Instructions}}}
````

**Note:** PromptEngine uses **Mustache template syntax** 

### 3. Define Context Class

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

### 4. Build and Use

The source generator creates a `SummarizePromptBuilder` class:

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

## Template Syntax

PromptEngine uses **Mustache** template syntax, a logic-less templating system that is widely used across different programming languages.

### Basic Variables

Reference context properties using double curly braces:

```markdown
Hello {{UserName}}, welcome!
```

### Sections (Conditionals and Loops)

Use sections to conditionally render content or iterate over collections:

```markdown
{{#HasItems}}
  You have items:
  {{#Items}}
    - {{Name}}: {{Description}}
  {{/Items}}
{{/HasItems}}
```

### Inverted Sections

Render content when a value is false or empty:

```markdown
{{^HasItems}}
  No items available.
{{/HasItems}}
```

### Nested Properties

Access nested object properties using dot notation:

```markdown
User: {{User.Name}} ({{User.Email}})
```

### Comments

Add comments that won't appear in the output:

```markdown
{{! This is a comment and will not be rendered }}
```

### Case Insensitivity

Property lookups are case-insensitive by default, so `{{UserName}}`, `{{username}}`, and `{{USERNAME}}` all reference the same property.

### Escaping

All variables are HTML-escaped by default. Use triple braces for unescaped output (use with caution):

```markdown
Escaped: {{Content}}
Unescaped: {{{RawHtmlContent}}}
```

## Compile-time Validation

The analyzer provides compile-time template validation during build.

If your template uses undefined placeholders, you'll get a compile error:

```text
error PE003: Template uses undefined placeholder '{{UndefinedVariable}}' not found in context class 'SummarizeContext'
```

If you have unused properties in your context, you'll get an info diagnostic:

```text
info PE004: Context property 'UnusedProperty' in class 'SummarizeContext' is not used in template
```

## Runtime Validation

Validate templates at runtime or in CI/CD using the `PromptEngine` package:

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


## Troubleshooting

- Analyzer does not find templates
 - Make sure your `.prompt.md` files are included via `<AdditionalFiles />` and paths in `PromptContext` match (relative paths are recommended). The generator discovers these files at compile time.
- Placeholders not replaced at runtime
 - Confirm the context properties are public and names match (case-sensitive by default). Use Mustache syntax with double curly braces `{{PropertyName}}`. The analyzer enforces this at compile-time via `PE003` / `PE004`.
- Template syntax errors
 - Ensure you're using proper Mustache syntax: `{{Variable}}` for properties, `{{#Section}}...{{/Section}}` for conditionals/loops, `{{^Inverted}}...{{/Inverted}}` for inverted sections.

## Requirements

- .NET 8.0 or later

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
