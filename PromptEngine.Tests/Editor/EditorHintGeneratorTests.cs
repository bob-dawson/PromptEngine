using System.Text.Json;
using PromptEngine.Core.Models;
using PromptEngine.Editor.Generators;
using Xunit;

namespace PromptEngine.Tests.Editor;

public class EditorHintGeneratorTests
{
    [Fact]
    public void GenerateEditorMetadata_ValidInput_CreatesMetadata()
    {
        // Arrange
        var generator = new EditorHintGenerator();
        var promptMetadata = new List<PromptMetadata>
        {
            new PromptMetadata
            {
                TemplateName = "Test",
                TemplatePath = "test.prompt.txt",
                ContextTypeName = "TestContext",
                Placeholders = new List<string> { "Name", "Age" },
                ContextProperties = new List<string> { "Name", "Age", "Email" }
            }
        };

        // Act
        var result = generator.GenerateEditorMetadata(promptMetadata);

        // Assert
        Assert.Single(result);
        var meta = result[0];
        Assert.Equal("Test", meta.TemplateName);
        Assert.Equal("test.prompt.txt", meta.TemplatePath);
        Assert.Equal(3, meta.AvailableVariables.Count);
    }

    [Fact]
    public void GenerateJsonHints_ValidInput_CreatesValidJson()
    {
        // Arrange
        var generator = new EditorHintGenerator();
        var promptMetadata = new List<PromptMetadata>
        {
            new PromptMetadata
            {
                TemplateName = "Test",
                TemplatePath = "test.prompt.txt",
                ContextTypeName = "TestContext",
                Placeholders = new List<string> { "Name" },
                ContextProperties = new List<string> { "Name", "Age" }
            }
         };

        // Act
        var json = generator.GenerateJsonHints(promptMetadata);

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);

        // Should be valid JSON
        var parsed = JsonSerializer.Deserialize<JsonDocument>(json);
        Assert.NotNull(parsed);
    }

    [Fact]
    public void GenerateTypeScriptDefinitions_ValidInput_CreatesTypeScript()
    {
        // Arrange
        var generator = new EditorHintGenerator();
        var promptMetadata = new List<PromptMetadata>
        {
            new PromptMetadata
            {
                TemplateName = "Test",
                TemplatePath = "test.prompt.txt",
                ContextTypeName = "Namespace.TestContext",
                Placeholders = new List<string> { "Name" },
                ContextProperties = new List<string> { "Name", "Age" }
            }
        };

        // Act
        var typescript = generator.GenerateTypeScriptDefinitions(promptMetadata);

        // Assert
        Assert.NotNull(typescript);
        Assert.Contains("export interface TestContext", typescript);
        Assert.Contains("Name", typescript);
        Assert.Contains("Age", typescript);
        Assert.Contains("PromptTemplates", typescript);
    }

    [Fact]
    public void GenerateTypeScriptDefinitions_RequiredProperties_MarkedCorrectly()
    {
        // Arrange
        var generator = new EditorHintGenerator();
        var promptMetadata = new List<PromptMetadata>
        {
            new PromptMetadata
            {
                TemplateName = "Test",
                TemplatePath = "test.prompt.txt",
                ContextTypeName = "TestContext",
                Placeholders = new List<string> { "Name" }, // Only Name is used
                ContextProperties = new List<string> { "Name", "Age" }
            }
        };

        // Act
        var typescript = generator.GenerateTypeScriptDefinitions(promptMetadata);

        // Assert
        // Name should be required (no ?)
        Assert.Contains("Name: string;", typescript);
        // Age should be optional (has ?)
        Assert.Contains("Age?: string;", typescript);
    }

    [Fact]
    public void SaveEditorHints_ValidInput_CreatesFiles()
    {
        // Arrange
        var generator = new EditorHintGenerator();
        var promptMetadata = new List<PromptMetadata>
        {
            new PromptMetadata
            {
                TemplateName = "Test",
                TemplatePath = "test.prompt.txt",
                ContextTypeName = "TestContext",
                Placeholders = new List<string> { "Name" },
                ContextProperties = new List<string> { "Name" }
            }
        };

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            // Act
            generator.SaveEditorHints(promptMetadata, tempDir);

            // Assert
            var jsonPath = Path.Combine(tempDir, "prompt-hints.json");
            var tsPath = Path.Combine(tempDir, "prompt-types.d.ts");

            Assert.True(File.Exists(jsonPath));
            Assert.True(File.Exists(tsPath));

            var jsonContent = File.ReadAllText(jsonPath);
            var tsContent = File.ReadAllText(tsPath);

            Assert.NotEmpty(jsonContent);
            Assert.NotEmpty(tsContent);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
