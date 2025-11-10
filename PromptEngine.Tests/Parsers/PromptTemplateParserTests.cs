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

        // Assert - only extracts 'Items' (root-level)
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

        // Assert - only extracts root 'User'
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
    public void ExtractPlaceholders_ImplicitIterator_TopLevel_Ignored()
    {
        // Arrange
        var template = "Value: {{.}}";

        // Act
        var placeholders = MustacheTemplateParser.ExtractPlaceholders(template);

        // Assert - implicit iterator at root should not be counted as a placeholder
        Assert.Empty(placeholders);
    }

    [Fact]
    public void ExtractPlaceholders_ImplicitIterator_InsideSection_OnlySectionRootExtracted()
    {
        // Arrange
        var template = "{{#Items}}{{.}}{{/Items}}";

        // Act
        var placeholders = MustacheTemplateParser.ExtractPlaceholders(template);

        // Assert - only 'Items' is extracted at root level
        Assert.Single(placeholders);
        Assert.Contains("Items", placeholders);
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
    public void ValidateTemplate_CaseSensitive_FailsOnDifferentCasing()
    {
        // Arrange
        var placeholders = new HashSet<string> { "username", "userage" }; // wrong casing
        var properties = new HashSet<string> { "UserName", "UserAge" };

        // Act
        var (isValid, missing, unused) = MustacheTemplateParser.ValidateTemplate(placeholders, properties);

        // Assert
        Assert.False(isValid);
        Assert.Equal(2, missing.Count);
        Assert.Contains("username", missing);
        Assert.Contains("userage", missing);
    }

    [Fact]
    public void ValidateTemplate_NestedPath_ValidatesRootProperty()
    {
        // Arrange
        var placeholders = new HashSet<string> { "User" }; // only validate root property
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
