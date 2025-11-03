using PromptEngine.Core.Parsers;
using Xunit;

namespace PromptEngine.Tests.Parsers;

public class MustacheTemplateParserTests
{
    [Fact]
    public void ExtractPlaceholders_ValidMustacheTemplate_ReturnsAllPlaceholders()
    {
        // Arrange
        var template = "Hello {{Name}}, your age is {{Age}} and email is {{Email}}";

        // Act
        var placeholders = MustacheTemplateParser.ExtractPlaceholders(template);

        // Assert
        Assert.Equal(3, placeholders.Count);
        Assert.Contains("Name", placeholders);
        Assert.Contains("Age", placeholders);
        Assert.Contains("Email", placeholders);
    }

    [Fact]
    public void ExtractPlaceholders_SectionSyntax_ExtractsPlaceholder()
    {
        // Arrange
        var template = "{{#Items}}Item: {{Name}}{{/Items}}";

        // Act
        var placeholders = MustacheTemplateParser.ExtractPlaceholders(template);

        // Assert - 只提取顶层属性 Items
        Assert.Single(placeholders);
        Assert.Contains("Items", placeholders);
    }

    [Fact]
    public void ExtractPlaceholders_InvertedSection_ExtractsPlaceholder()
    {
        // Arrange
        var template = "{{^HasItems}}No items{{/HasItems}}";

        // Act
        var placeholders = MustacheTemplateParser.ExtractPlaceholders(template);

        // Assert
        Assert.Single(placeholders);
        Assert.Contains("HasItems", placeholders);
    }

    [Fact]
    public void ExtractPlaceholders_NestedPaths_ExtractsPlaceholders()
    {
        // Arrange
        var template = "Hello {{User.Name}}, your email is {{User.Email}}";

        // Act
        var placeholders = MustacheTemplateParser.ExtractPlaceholders(template);

        // Assert - 只提取根属性 "User"
        Assert.Single(placeholders);
        Assert.Contains("User", placeholders);
    }

    [Fact]
    public void ExtractPlaceholders_EmptyTemplate_ReturnsEmpty()
    {
        // Arrange
        var template = "";

        // Act
        var placeholders = MustacheTemplateParser.ExtractPlaceholders(template);

        // Assert
        Assert.Empty(placeholders);
    }

    [Fact]
    public void ExtractPlaceholders_NoPlaceholders_ReturnsEmpty()
    {
        // Arrange
        var template = "Hello World, no placeholders here";

        // Act
        var placeholders = MustacheTemplateParser.ExtractPlaceholders(template);

        // Assert
        Assert.Empty(placeholders);
    }

    [Fact]
    public void ExtractPlaceholders_DuplicatePlaceholders_ReturnsUnique()
    {
        // Arrange
        var template = "Hello {{Name}}, {{Name}} is great!";

        // Act
        var placeholders = MustacheTemplateParser.ExtractPlaceholders(template);

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
        var (isValid, missing, unused) = MustacheTemplateParser.ValidateTemplate(placeholders, properties);

        // Assert
        Assert.True(isValid);
        Assert.Empty(missing);
        Assert.Empty(unused);
    }

    [Fact]
    public void ValidateTemplate_NestedPath_ValidatesRootProperty()
    {
        // Arrange
        var placeholders = new HashSet<string> { "User" }; // 只包含根属性
        var properties = new HashSet<string> { "User" };

        // Act
        var (isValid, missing, unused) = MustacheTemplateParser.ValidateTemplate(placeholders, properties);

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
        var (isValid, missing, unused) = MustacheTemplateParser.ValidateTemplate(placeholders, properties);

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
        var (isValid, missing, unused) = MustacheTemplateParser.ValidateTemplate(placeholders, properties);

        // Assert
        Assert.True(isValid);
        Assert.Empty(missing);
        Assert.Equal(2, unused.Count);
        Assert.Contains("Age", unused);
        Assert.Contains("Email", unused);
    }
}
