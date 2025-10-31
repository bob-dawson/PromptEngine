using PromptEngine.Core.Parsers;
using Xunit;

namespace PromptEngine.Tests.Parsers;

public class PromptTemplateParserTests
{
    [Fact]
    public void ExtractPlaceholders_ValidTemplate_ReturnsAllPlaceholders()
    {
        // Arrange
        var template = "Hello {Name}, your age is {Age} and email is {Email}";

        // Act
        var placeholders = PromptTemplateParser.ExtractPlaceholders(template);

        // Assert
        Assert.Equal(3, placeholders.Count);
        Assert.Contains("Name", placeholders);
        Assert.Contains("Age", placeholders);
        Assert.Contains("Email", placeholders);
    }

    [Fact]
    public void ExtractPlaceholders_EmptyTemplate_ReturnsEmpty()
    {
        // Arrange
        var template = "";

        // Act
        var placeholders = PromptTemplateParser.ExtractPlaceholders(template);

        // Assert
        Assert.Empty(placeholders);
    }

    [Fact]
    public void ExtractPlaceholders_NoPlaceholders_ReturnsEmpty()
    {
        // Arrange
        var template = "Hello World, no placeholders here";

        // Act
        var placeholders = PromptTemplateParser.ExtractPlaceholders(template);

        // Assert
        Assert.Empty(placeholders);
    }

    [Fact]
    public void ExtractPlaceholders_DuplicatePlaceholders_ReturnsUnique()
    {
        // Arrange
        var template = "Hello {Name}, {Name} is great!";

        // Act
        var placeholders = PromptTemplateParser.ExtractPlaceholders(template);

        // Assert
        Assert.Single(placeholders);
        Assert.Contains("Name", placeholders);
    }

    [Fact]
    public void ValidateTemplate_AllPropertiesUsed_ReturnsValid()
    {
        // Arrange
        var placeholders = new HashSet<string> { "Name", "Age" };
        var properties = new HashSet<string> { "Name", "Age" };

        // Act
        var (isValid, missing, unused) = PromptTemplateParser.ValidateTemplate(placeholders, properties);

        // Assert
        Assert.True(isValid);
        Assert.Empty(missing);
        Assert.Empty(unused);
    }

    [Fact]
    public void ValidateTemplate_MissingProperties_ReturnsInvalid()
    {
        // Arrange
        var placeholders = new HashSet<string> { "Name", "Age", "Email" };
        var properties = new HashSet<string> { "Name", "Age" };

        // Act
        var (isValid, missing, unused) = PromptTemplateParser.ValidateTemplate(placeholders, properties);

        // Assert
        Assert.False(isValid);
        Assert.Single(missing);
        Assert.Contains("Email", missing);
        Assert.Empty(unused);
    }

    [Fact]
    public void ValidateTemplate_UnusedProperties_ReturnsWarnings()
    {
        // Arrange
        var placeholders = new HashSet<string> { "Name" };
        var properties = new HashSet<string> { "Name", "Age", "Email" };

        // Act
        var (isValid, missing, unused) = PromptTemplateParser.ValidateTemplate(placeholders, properties);

        // Assert
        Assert.True(isValid);
        Assert.Empty(missing);
        Assert.Equal(2, unused.Count);
        Assert.Contains("Age", unused);
        Assert.Contains("Email", unused);
    }

    [Fact]
    public void ReplacePlaceholders_ValidInput_ReplacesCorrectly()
    {
        // Arrange
        var template = "Hello {Name}, you are {Age} years old";
        var values = new Dictionary<string, string?>
        {
            { "Name", "Alice" },
        { "Age", "30" }
    };

        // Act
        var result = PromptTemplateParser.ReplacePlaceholders(template, values);

        // Assert
        Assert.Equal("Hello Alice, you are 30 years old", result);
    }

    [Fact]
    public void ReplacePlaceholders_MissingValue_KeepsPlaceholder()
    {
        // Arrange
        var template = "Hello {Name}, you are {Age} years old";
        var values = new Dictionary<string, string?>
        {
            { "Name", "Alice" }
        };

        // Act
        var result = PromptTemplateParser.ReplacePlaceholders(template, values);

        // Assert
        Assert.Equal("Hello Alice, you are {Age} years old", result);
    }

    [Fact]
    public void ReplacePlaceholders_NullValue_ReplacesWithEmpty()
    {
        // Arrange
        var template = "Hello {Name}";
        var values = new Dictionary<string, string?>
        {
           { "Name", null }
        };

        // Act
        var result = PromptTemplateParser.ReplacePlaceholders(template, values);

        // Assert
        Assert.Equal("Hello ", result);
    }
}
