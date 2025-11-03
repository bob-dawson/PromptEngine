using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;
using Stubble.Core;
using Stubble.Core.Settings;

namespace PromptEngine.Analyzer;

[Generator]
public class PromptContextGenerator : IIncrementalGenerator
{
    private static readonly StubbleVisitorRenderer MustacheRenderer = new(new RendererSettingsBuilder()
    .SetIgnoreCaseOnKeyLookup(true)
        .BuildSettings());

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

    private readonly struct AdditionalFileInfo(string path, SourceText? content)
    {
        public string Path { get; } = path;
        public SourceText? Content { get; } = content;
    }

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

        List<PromptBuilderRequest> requests = [];

        foreach (var classDeclaration in classes.Distinct())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDeclaration)
                is not INamedTypeSymbol classSymbol) continue;

            // Find all PromptContextAttribute usages on this class
            var attributes = classSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == "PromptEngine.Core.Attributes.PromptContextAttribute")
                .ToArray();

            if (attributes.Length == 0) continue;

            // Collect public context properties once per class
            var properties = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            var propertySymbols = classSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public)
                .Select(p => p.Name);

            foreach (var name in propertySymbols)
            {
                if (name is not null)
                {
                    properties.Add(name);
                }
            }

            // Track union of placeholders across all templates for this class
            var unionPlaceholders = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var attribute in attributes)
            {
                // Template path declared on attribute
                var templatePath = attribute.ConstructorArguments[0].Value?.ToString();
                if (string.IsNullOrWhiteSpace(templatePath))
                {
                    ReportDiagnostic(context, "PE001", DiagnosticSeverity.Error,
                        $"Template path cannot be null or empty for class '{classSymbol.Name}'",
                        GetAttributeArgumentLocation(attribute, 0) ?? classDeclaration.GetLocation());
                    continue;
                }

                // Template name, default to the class name
                var templateName = attribute.NamedArguments
                   .FirstOrDefault(a => a.Key == "TemplateName").Value.Value?.ToString()
                    ?? classSymbol.Name;

                // Resolve template from AdditionalFiles
                var templateContent = TryResolveTemplateFromAdditionalFiles(additionalFiles, templatePath!, out var resolvedPath, out var resolvedText);
                if (templateContent is null)
                {
                    ReportDiagnostic(context, "PE002", DiagnosticSeverity.Warning,
                        $"Template file '{templatePath}' not found in AdditionalFiles for class '{classSymbol.Name}'. Add it to <AdditionalFiles Include=\"...\" /> in your project.",
                        GetAttributeArgumentLocation(attribute, 0) ?? classDeclaration.GetLocation());
                    continue;
                }

                // Extract placeholders using Mustache parser
                var placeholders = MustacheSymbolValidator.ExtractPlaceholders(templateContent);
                foreach (var ph in placeholders)
                {
                    // Add root property name to union (before first dot)
                    var rootProp = ph.Split('.')[0];
                    unionPlaceholders.Add(rootProp);
                }

                // Validate using symbol-based Mustache validator
                var validationErrors = MustacheSymbolValidator.ValidateTemplate(templateContent, classSymbol);

                foreach (var error in validationErrors)
                {
                    Location? loc = null;
                    if (resolvedText is not null && resolvedPath is not null)
                    {
                        // Try to find the location of the error in the template
                        var errorMatch = System.Text.RegularExpressions.Regex.Match(error, @"'([^']+)'");
                        if (errorMatch.Success)
                        {
                            var pathValue = errorMatch.Groups[1].Value;
                            // Try both Mustache syntax {{path}} and plain {path}
                            var needles = new[] { "{{" + pathValue + "}}", "{" + pathValue + "}" };
                            foreach (var needle in needles)
                            {
                                var idx = templateContent.IndexOf(needle, System.StringComparison.Ordinal);
                                if (idx >= 0)
                                {
                                    var span = new TextSpan(idx, needle.Length);
                                    var lineSpan = resolvedText.Lines.GetLinePositionSpan(span);
                                    loc = Location.Create(resolvedPath, span, lineSpan);
                                    break;
                                }
                            }
                        }
                    }

                    ReportDiagnostic(context, "PE003", DiagnosticSeverity.Error,
                       error,
                 loc ?? classDeclaration.GetLocation());
                }

                if (validationErrors.Count > 0) continue;

                var request = new PromptBuilderRequest
                {
                    ContextClass = classSymbol,
                    TemplateName = templateName,
                    TemplateContent = templateContent,
                    Placeholders = placeholders,
                    ContextProperties = properties,
                    TemplatePath = templatePath
                };
                requests.Add(request);
                var builderSource = GeneratePromptBuilderWithMustache(request);
                var hintName = $"{classSymbol.Name}_{Sanitize(templateName)}_PromptBuilder.g.cs";
                context.AddSource(hintName, SourceText.From(builderSource, Encoding.UTF8));
            }

            // After processing all templates for this class, warn about properties unused across all templates
            var unusedOverall = properties.Except(unionPlaceholders, System.StringComparer.OrdinalIgnoreCase);
            foreach (var u in unusedOverall)
            {
                Location? loc = null;
                var propSymbol = classSymbol.GetMembers()
               .OfType<IPropertySymbol>()
               .FirstOrDefault(p => p.Name == u);
                var propLoc = propSymbol?.Locations.FirstOrDefault();
                if (propLoc is not null)
                {
                    loc = propLoc;
                }
                ReportDiagnostic(context, "PE004", DiagnosticSeverity.Warning,
                      $"Context property '{u}' in class '{classSymbol.Name}' is not used in any template",
                         loc ?? classDeclaration.GetLocation());
            }
        }

        // Emit one static registrar that calls each builder Register()
        var builderSourceRegister = GenerateRegisterWithMustache(compilation, requests);
        var hintNameRegister = "PromptEngineRegister.g.cs";
        context.AddSource(hintNameRegister, SourceText.From(builderSourceRegister, Encoding.UTF8));
    }

    private static string GenerateRegisterWithMustache(Compilation compilation, List<PromptBuilderRequest> requests)
    {
        var templateText = LoadEmbeddedTemplate("Register.mus");

        var modelItems = requests.Select(r => new
        {
            namespace_name = r.ContextClass.ContainingNamespace.ToDisplayString(),
            builder_class_name = $"{r.TemplateName}PromptBuilder"
        }).ToList();

        var assemblyName = compilation.AssemblyName ?? compilation.Assembly.Identity.Name;
        var model = new { assemblyName, items = modelItems };
        return MustacheRenderer.Render(templateText, model);
    }

    private static void ReportDiagnostic(SourceProductionContext context, string id, DiagnosticSeverity severity, string message, Location? location)
    {
        // DiagnosticDescriptor.MessageFormat is a composite string and must escape braces.
        static string EscapeBraces(string s) => s.Replace("{", "{{").Replace("}", "}}");

        var safeMessage = EscapeBraces(message);
        var descriptor = new DiagnosticDescriptor(id, safeMessage, safeMessage, "PromptEngine", severity, true);
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location));
    }

    private static string? TryResolveTemplateFromAdditionalFiles(ImmutableArray<AdditionalFileInfo> additionalFiles, string templatePath, out string? resolvedPath, out SourceText? resolvedText)
    {
        resolvedPath = null;
        resolvedText = null;
        var normNeedle = Normalize(templatePath);

        foreach (var file in additionalFiles)
        {
            var normHay = Normalize(file.Path);
            if (normHay.EndsWith(normNeedle) ||
                string.Equals(System.IO.Path.GetFileName(normHay), System.IO.Path.GetFileName(normNeedle), System.StringComparison.OrdinalIgnoreCase))
            {
                resolvedPath = file.Path;
                resolvedText = file.Content;
                return file.Content?.ToString();
            }
        }

        return null;
    }

    private static Location? GetAttributeArgumentLocation(AttributeData attribute, int argIndex)
    {
        var syntax = attribute.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
        var arg = syntax?.ArgumentList?.Arguments.ElementAtOrDefault(argIndex);
        return arg?.GetLocation();
    }

    private static string Normalize(string path)
        => path.Replace('\\', '/').TrimStart('.', '/');

    private static string Sanitize(string name)
    {
        if (string.IsNullOrEmpty(name)) return "_";
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
            else sb.Append('_');
        }
        return sb.ToString();
    }

    // DTO to carry generation parameters
    private sealed class PromptBuilderRequest
    {
        public INamedTypeSymbol ContextClass { get; set; }
        public string TemplateName { get; set; }
        public string TemplateContent { get; set; }
        public HashSet<string> Placeholders { get; set; }
        public HashSet<string> ContextProperties { get; set; }
        public string TemplatePath { get; set; }
    }

    private static string GeneratePromptBuilderWithMustache(PromptBuilderRequest request)
    {
        var namespaceName = request.ContextClass.ContainingNamespace.ToDisplayString();
        var className = request.ContextClass.Name;
        var fullTypeName = request.ContextClass.ToDisplayString();
        var builderClassName = $"{request.TemplateName}PromptBuilder";

        // Escape template for C# string
        var escapedTemplate = EscapeForCSharp(request.TemplateContent);

        var templateText = LoadEmbeddedTemplate("PromptBuilder.mus");

        var model = new
        {
            namespace_name = namespaceName,
            builder_class_name = builderClassName,
            template_name = request.TemplateName,
            context_class_name = className,
            context_full_type_name = fullTypeName,
            template_path = Esc(request.TemplatePath),
            placeholders = request.Placeholders.Select(Esc).ToList(),
            context_properties = request.ContextProperties.Select(Esc).ToList(),
            template_content = Esc(request.TemplateContent),
            escaped_template = escapedTemplate
        };
        return MustacheRenderer.Render(templateText, model);
    }

    private static string Esc(string v) => EscapeForCSharp(v ?? string.Empty);

    private static string LoadEmbeddedTemplate(string fileName)
    {
        var asm = typeof(PromptContextGenerator).GetTypeInfo().Assembly;
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith($"Templates.{fileName}", System.StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
        {
            throw new System.InvalidOperationException($"Embedded template '{fileName}' not found in resources.");
        }
        using var stream = asm.GetManifestResourceStream(resourceName)!;
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    private static string EscapeForCSharp(string s)
        => (s ?? string.Empty)
        .Replace("\\", "\\\\")
        .Replace("\"", "\\\"")
        .Replace("\r\n", "\n")
        .Replace("\r", "")
        .Replace("\n", "\\n");
}
