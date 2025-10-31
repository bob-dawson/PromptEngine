using PromptEngine.Agent.Services;
using PromptEngine.Agent.Abstractions;
using Xunit;

namespace PromptEngine.Tests.Agent;

public class PromptManagerTests
{
    private class TestContext
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    private class TestPromptBuilder : IPromptBuilder<TestContext>
    {
        public string Build(TestContext context)
        {
            return $"Name: {context.Name}, Value: {context.Value}";
        }

        public string GetTemplate()
        {
            return "Name: {Name}, Value: {Value}";
        }

        public string GetTemplateName()
        {
            return "Test";
        }
    }

    [Fact]
    public void RegisterPromptBuilder_ValidInput_Succeeds()
    {
        // Arrange
        var manager = new PromptManager();
        var builder = new TestPromptBuilder();

        // Act
        manager.RegisterPromptBuilder("Test", builder);

        // Assert
        var registered = manager.GetPromptBuilder<TestContext>("Test");
        Assert.NotNull(registered);
    }

    [Fact]
    public void RegisterPromptBuilder_NullTemplateName_ThrowsException()
    {
        // Arrange
        var manager = new PromptManager();
        var builder = new TestPromptBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => manager.RegisterPromptBuilder<TestContext>(null!, builder));
    }

    [Fact]
    public void RegisterPromptBuilder_NullBuilder_ThrowsException()
    {
        // Arrange
        var manager = new PromptManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => manager.RegisterPromptBuilder<TestContext>("Test", null!));
    }

    [Fact]
    public void GetPromptBuilder_RegisteredTemplate_ReturnsBuilder()
    {
        // Arrange
        var manager = new PromptManager();
        var builder = new TestPromptBuilder();
        manager.RegisterPromptBuilder("Test", builder);

        // Act
        var result = manager.GetPromptBuilder<TestContext>("Test");

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    public void GetPromptBuilder_UnregisteredTemplate_ReturnsNull()
    {
        // Arrange
        var manager = new PromptManager();

        // Act
        var result = manager.GetPromptBuilder<TestContext>("NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void BuildPrompt_RegisteredTemplate_BuildsPrompt()
    {
        // Arrange
        var manager = new PromptManager();
        var builder = new TestPromptBuilder();
        manager.RegisterPromptBuilder("Test", builder);

        var context = new TestContext
        {
            Name = "Alice",
            Value = "123"
        };

        // Act
        var prompt = manager.BuildPrompt("Test", context);

        // Assert
        Assert.Equal("Name: Alice, Value: 123", prompt);
    }

    [Fact]
    public void BuildPrompt_UnregisteredTemplate_ThrowsException()
    {
        // Arrange
        var manager = new PromptManager();
        var context = new TestContext();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => manager.BuildPrompt("NonExistent", context));
    }

    [Fact]
    public void GetRegisteredTemplates_MultipleTemplates_ReturnsAll()
    {
        // Arrange
        var manager = new PromptManager();
        manager.RegisterPromptBuilder("Test1", new TestPromptBuilder());
        manager.RegisterPromptBuilder("Test2", new TestPromptBuilder());

        // Act
        var templates = manager.GetRegisteredTemplates().ToList();

        // Assert
        Assert.Equal(2, templates.Count);
        Assert.Contains("Test1", templates);
        Assert.Contains("Test2", templates);
    }

    [Fact]
    public void GetRegisteredTemplates_NoTemplates_ReturnsEmpty()
    {
        // Arrange
        var manager = new PromptManager();

        // Act
        var templates = manager.GetRegisteredTemplates();

        // Assert
        Assert.Empty(templates);
    }
}
