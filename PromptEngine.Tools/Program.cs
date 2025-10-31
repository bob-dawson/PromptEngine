using PromptEngine.Core.Runtime;

namespace PromptEngine.Tools;

/// <summary>
/// CLI tool for runtime validation (self-contained, does not depend on analyzer packages)
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("=== PromptEngine Validation Tool ===\n");

        if (args.Length == 0)
        {
            return ShowHelp();
        }

        var command = args[0].ToLowerInvariant();

        try
        {
            return command switch
            {
                "validate" => ValidateCommand([.. args.Skip(1)]),
                "list" => ListCommand([.. args.Skip(1)]),
                "help" or "--help" or "-h" => ShowHelp(),
                _ => InvalidCommand(command)
            };
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    static int ValidateCommand(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: promptengine validate <directory>");
            return 1;
        }

        var directory = args[0];
        Console.WriteLine($"Validating prompts in: {directory}\n");

        var validator = new PromptRuntimeValidator();

        try
        {
            validator.LoadMetadataFromDirectory(directory);
            var result = validator.ValidateAll();

            Console.WriteLine("\n" + result.ToString());

            if (result.IsValid)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ All validations passed!");
                Console.ResetColor();
                return 0;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n✗ Validation failed!");
                Console.ResetColor();
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Validation error: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    static int ListCommand(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: promptengine list <directory>");
            return 1;
        }

        var directory = args[0];
        Console.WriteLine($"Listing prompts in: {directory}\n");

        var validator = new PromptRuntimeValidator();
        validator.LoadMetadataFromDirectory(directory);
        var list = validator.GetLoadedMetadata();

        if (list.Count == 0)
        {
            Console.WriteLine("No metadata found");
            return 0;
        }

        foreach (var meta in list)
        {
            Console.WriteLine($"Template: {meta.TemplateName}");
            Console.WriteLine($" Path: {meta.TemplatePath}");
            Console.WriteLine($" Context: {meta.ContextTypeName}");
            Console.WriteLine($" Placeholders: {string.Join(", ", meta.Placeholders)}");
            Console.WriteLine();
        }

        return 0;
    }

    static int ShowHelp()
    {
        Console.WriteLine("PromptEngine CLI Tool");
        Console.WriteLine("\nUsage:");
        Console.WriteLine(" promptengine validate <directory> - Validate all prompts in directory");
        Console.WriteLine(" promptengine list <directory> - List all registered prompts");
        Console.WriteLine(" promptengine help - Show this help message");
        Console.WriteLine("\nExamples:");
        Console.WriteLine(" promptengine validate ./bin/Debug/net10.0");
        Console.WriteLine(" promptengine list ./bin/Debug/net10.0");
        return 0;
    }

    static int InvalidCommand(string command)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Unknown command: {command}");
        Console.ResetColor();
        Console.WriteLine("\nUse 'promptengine help' for usage information");
        return 1;
    }
}
