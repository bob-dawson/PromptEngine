using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using PromptEngine.Core.Models;
using PromptEngine.Core.Parsers;

namespace PromptEngine.Analyzer.SourceGenerators;

[Generator]
public class PromptContextGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Discover classes with PromptContextAttribute
        var classDeclarations = context.SyntaxProvider
        .CreateSyntaxProvider(
        predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
        transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
        .Where(static m => m is not null);

        // Collect AdditionalFiles (prompt templates are provided via <AdditionalFiles />)
        var additionalFiles = context.AdditionalTextsProvider
        .Select(static (text, ct) => new AdditionalFileInfo(text.Path, text.GetText(ct)))
        .Collect();

        // Combine compilation, classes and additional files
        var input = context.CompilationProvider
        .Combine(classDeclarations.Collect())
        .Combine(additionalFiles);

        context.RegisterSourceOutput(input, static (spc, source) =>
        {
            var (compilation, classes) = source.Left;
            var files = source.Right;
            Execute(compilation, classes!, files, spc);
        });
    }

    private readonly record struct AdditionalFileInfo(string Path, SourceText? Content);

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    => node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0;

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol;
                if (symbol is not IMethodSymbol attributeSymbol) continue;

                var attributeClass = attributeSymbol.ContainingType;
                var fullName = attributeClass.ToDisplayString();
                if (fullName == "PromptEngine.Core.Attributes.PromptContextAttribute")
                {
                    return classDeclaration;
                }
            }
        }
        return null;
    }

    private static void Execute(
    Compilation compilation,
    ImmutableArray<ClassDeclarationSyntax> classes,
    ImmutableArray<AdditionalFileInfo> additionalFiles,
    SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty) return;

        var metadataItems = new List<PromptMetadata>();

        foreach (var classDeclaration in classes.Distinct())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
            if (classSymbol is null) continue;

            var attribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "PromptEngine.Core.Attributes.PromptContextAttribute");
            if (attribute is null) continue;

            // Template path declared on attribute
            var templatePath = attribute.ConstructorArguments[0].Value?.ToString();
            if (string.IsNullOrWhiteSpace(templatePath))
            {
                ReportDiagnostic(context, "PE001", DiagnosticSeverity.Error,
                $"Template path cannot be null or empty for class '{classSymbol.Name}'",
                classDeclaration.GetLocation());
                continue;
            }

            // Template name, default to class name
            var templateName = attribute.NamedArguments
            .FirstOrDefault(a => a.Key == "TemplateName").Value.Value?.ToString()
            ?? classSymbol.Name;

            // Resolve template from AdditionalFiles
            var templateContent = TryResolveTemplateFromAdditionalFiles(additionalFiles, templatePath!, out var resolvedPath);
            if (templateContent is null)
            {
                ReportDiagnostic(context, "PE002", DiagnosticSeverity.Warning,
                $"Template file '{templatePath}' not found in AdditionalFiles for class '{classSymbol.Name}'. " +
                "Add it to <AdditionalFiles Include=\"...\" /> in your project.",
                classDeclaration.GetLocation());
                continue;
            }

            // Extract placeholders
            var placeholders = PromptTemplateParser.ExtractPlaceholders(templateContent);

            // Collect public context properties
            var properties = classSymbol!.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Validate
            var (isValid, missing, unused) = PromptTemplateParser.ValidateTemplate(placeholders, properties);

            foreach (var m in missing)
            {
                ReportDiagnostic(context, "PE003", DiagnosticSeverity.Error,
                $"Template uses undefined placeholder '{{{m}}}' not found in context class '{classSymbol.Name}'",
                classDeclaration.GetLocation());
            }

            foreach (var u in unused)
            {
                ReportDiagnostic(context, "PE004", DiagnosticSeverity.Info,
                $"Context property '{u}' in class '{classSymbol.Name}' is not used in template",
                classDeclaration.GetLocation());
            }

            if (!isValid) continue;

            // Generate strongly-typed builder
            var source = GeneratePromptBuilder(classSymbol!, templateName, templateContent, placeholders);
            context.AddSource($"{classSymbol.Name}_PromptBuilder.g.cs", SourceText.From(source, Encoding.UTF8));

            // Capture metadata for a generated registry
            metadataItems.Add(new PromptMetadata
            {
                TemplateName = templateName,
                TemplatePath = resolvedPath ?? templatePath!,
                ContextTypeName = classSymbol.ToDisplayString(),
                Placeholders = placeholders.ToList(),
                ContextProperties = properties.ToList(),
                TemplateContent = templateContent
            });
        }

        // Emit a C# metadata registry (no JSON files, no disk IO)
        if (metadataItems.Count > 0)
        {
            var registrySource = GenerateMetadataRegistry(metadataItems);
            context.AddSource("PromptMetadata.g.cs", SourceText.From(registrySource, Encoding.UTF8));
        }
    }

    private static void ReportDiagnostic(SourceProductionContext context, string id, DiagnosticSeverity severity, string message, Location? location)
    {
        var descriptor = new DiagnosticDescriptor(id, message, message, "PromptEngine", severity, true);
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location));
    }

    private static string? TryResolveTemplateFromAdditionalFiles(ImmutableArray<AdditionalFileInfo> additionalFiles, string templatePath, out string? resolvedPath)
    {
        resolvedPath = null;
        var normNeedle = Normalize(templatePath);

        foreach (var file in additionalFiles)
        {
            var normHay = Normalize(file.Path);
            if (normHay.EndsWith(normNeedle, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(System.IO.Path.GetFileName(normHay), System.IO.Path.GetFileName(normNeedle), StringComparison.OrdinalIgnoreCase))
            {
                resolvedPath = file.Path;
                return file.Content?.ToString();
            }
        }

        return null;
    }

    private static string Normalize(string path)
    => path.Replace('\\', '/').TrimStart('.', '/');

    private static string GeneratePromptBuilder(INamedTypeSymbol contextClass, string templateName, string templateContent, HashSet<string> placeholders)
    {
        var namespaceName = contextClass.ContainingNamespace.ToDisplayString();
        var className = contextClass.Name;
        var builderClassName = $"{templateName}PromptBuilder";

        // Escape template
        var escapedTemplate = templateContent
        .Replace("\\", "\\\\")
        .Replace("\"", "\\\"")
        .Replace("\r\n", "\n")
        .Replace("\r", "")
        .Replace("\n", "\\n");

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Prompt builder for {templateName}");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public static partial class {builderClassName}");
        sb.AppendLine("{");
        sb.AppendLine(" /// <summary>");
        sb.AppendLine($" /// Build prompt from {className} context");
        sb.AppendLine(" /// </summary>");
        sb.AppendLine($" public static string Build({className} context)");
        sb.AppendLine(" {");
        sb.AppendLine(" if (context == null) throw new System.ArgumentNullException(nameof(context));");
        sb.AppendLine();
        var interpolated = ReplaceWithInterpolations(escapedTemplate, placeholders);
        sb.AppendLine($" return $\"{interpolated}\";");
        sb.AppendLine(" }");
        sb.AppendLine();
        sb.AppendLine(" /// <summary>");
        sb.AppendLine(" /// Get template content");
        sb.AppendLine(" /// </summary>");
        sb.AppendLine(" public static string GetTemplate()");
        sb.AppendLine(" {");
        sb.AppendLine($" return \"{escapedTemplate}\";");
        sb.AppendLine(" }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string ReplaceWithInterpolations(string escapedTemplate, HashSet<string> placeholders)
    {
        var result = escapedTemplate;
        foreach (var ph in placeholders)
        {
            result = result.Replace("{" + ph + "}", "{" + $"context.{ph}" + "}");
        }
        return result;
    }

    private static string GenerateMetadataRegistry(List<PromptMetadata> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using PromptEngine.Core.Models;");
        sb.AppendLine();
        sb.AppendLine("namespace PromptEngine.Generated;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>Generated registry that exposes prompt metadata at runtime.</summary>");
        sb.AppendLine("internal static class PromptMetadataRegistry");
        sb.AppendLine("{");
        sb.AppendLine(" internal static IReadOnlyList<PromptMetadata> All { get; } = new List<PromptMetadata>");
        sb.AppendLine(" {");
        for (int i = 0; i < items.Count; i++)
        {
            var m = items[i];
            sb.AppendLine(" new PromptMetadata");
            sb.AppendLine(" {");
            sb.AppendLine($" TemplateName = \"{Escape(m.TemplateName)}\",");
            sb.AppendLine($" TemplatePath = \"{Escape(m.TemplatePath)}\",");
            sb.AppendLine($" ContextTypeName = \"{Escape(m.ContextTypeName)}\",");
            sb.AppendLine(" Placeholders = new List<string>");
            sb.AppendLine(" {");
            foreach (var p in m.Placeholders)
            {
                sb.AppendLine($" \"{Escape(p)}\",");
            }
            sb.AppendLine(" },");
            sb.AppendLine(" ContextProperties = new List<string>");
            sb.AppendLine(" {");
            foreach (var p in m.ContextProperties)
            {
                sb.AppendLine($" \"{Escape(p)}\",");
            }
            sb.AppendLine(" },");
            var templ = (m.TemplateContent ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r\n", "\n").Replace("\r", "").Replace("\n", "\\n");
            sb.AppendLine($" TemplateContent = \"{templ}\"");
            sb.AppendLine(i == items.Count - 1 ? " }" : " },");
        }
        sb.AppendLine(" };");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string Escape(string value)
    => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
