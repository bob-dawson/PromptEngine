using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PromptEngine.Analyzer;
using Xunit;

namespace PromptEngine.Tests.Analyzer;

public class MustacheSymbolValidatorTests
{
    [Fact]
    public void ExtractPlaceholders_ValidMustacheTemplate_ReturnsAllPlaceholders()
    {
        // Arrange
        var template = "Hello {{Name}}, your age is {{Age}} and email is {{Email}}";

        // Act
        var placeholders = MustacheSymbolValidator.ExtractPlaceholders(template);

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
        var placeholders = MustacheSymbolValidator.ExtractPlaceholders(template);

        // Assert - 只提取顶层属性 Items（不递归到 Section 内部）
        Assert.Single(placeholders);
        Assert.Contains("Items", placeholders);
    }

    [Fact]
    public void ExtractPlaceholders_InvertedSection_ExtractsPlaceholder()
    {
        // Arrange
        var template = "{{^HasItems}}No items{{/HasItems}}";

        // Act
        var placeholders = MustacheSymbolValidator.ExtractPlaceholders(template);

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
        var placeholders = MustacheSymbolValidator.ExtractPlaceholders(template);

        // Assert - 只提取根属性 "User"，不跟踪子属性
        Assert.Single(placeholders);
        Assert.Contains("User", placeholders);
    }

    [Fact]
    public void ExtractPlaceholders_EmptyTemplate_ReturnsEmpty()
    {
        // Arrange
        var template = "";

        // Act
        var placeholders = MustacheSymbolValidator.ExtractPlaceholders(template);

        // Assert
        Assert.Empty(placeholders);
    }

    [Fact]
    public void ExtractPlaceholders_NoPlaceholders_ReturnsEmpty()
    {
        // Arrange
        var template = "Hello World, no placeholders here";

        // Act
        var placeholders = MustacheSymbolValidator.ExtractPlaceholders(template);

        // Assert
        Assert.Empty(placeholders);
    }

    [Fact]
    public void ValidateTemplate_ValidSimpleProperties_ReturnsNoErrors()
    {
        // Arrange
        var code = @"
namespace Test
{
    public class TestContext
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}";
        var template = "Hello {{Name}}, you are {{Age}} years old";
        var symbol = GetTypeSymbol(code, "TestContext");

        // Act
        var errors = MustacheSymbolValidator.ValidateTemplate(template, symbol);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateTemplate_MissingProperty_ReturnsError()
    {
        // Arrange
        var code = @"
namespace Test
{
    public class TestContext
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}";
        var template = "Hello {{Name}}, your email is {{Email}}";
        var symbol = GetTypeSymbol(code, "TestContext");

        // Act
        var errors = MustacheSymbolValidator.ValidateTemplate(template, symbol);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Email", errors[0]);
        Assert.Contains("not found", errors[0]);
    }

    [Fact]
    public void ValidateTemplate_NestedProperties_ValidatesCorrectly()
    {
        // Arrange
        var code = @"
namespace Test
{
    public class Address
    {
        public string City { get; set; }
        public string Street { get; set; }
    }

    public class TestContext
    {
        public string Name { get; set; }
        public Address Address { get; set; }
    }
}";
        var template = "{{Name}} lives in {{Address.City}} on {{Address.Street}}";
        var symbol = GetTypeSymbol(code, "TestContext");

        // Act
        var errors = MustacheSymbolValidator.ValidateTemplate(template, symbol);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateTemplate_NestedPropertiesMissing_ReturnsError()
    {
        // Arrange
        var code = @"
namespace Test
{
    public class Address
    {
        public string City { get; set; }
    }

    public class TestContext
    {
        public string Name { get; set; }
        public Address Address { get; set; }
    }
}";
        var template = "{{Name}} lives in {{Address.City}} on {{Address.Street}}";
        var symbol = GetTypeSymbol(code, "TestContext");

        // Act
        var errors = MustacheSymbolValidator.ValidateTemplate(template, symbol);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Address.Street", errors[0]);
    }

    [Fact]
    public void ValidateTemplate_SectionWithList_ValidatesCorrectly()
    {
        // Arrange
        var code = @"
using System.Collections.Generic;

namespace Test
{
    public class Item
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public class TestContext
    {
      public List<Item> Items { get; set; }
    }
}";
        var template = "{{#Items}}{{Name}}: ${{Price}}{{/Items}}";
        var symbol = GetTypeSymbol(code, "TestContext");

        // Act
        var errors = MustacheSymbolValidator.ValidateTemplate(template, symbol);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateTemplate_SectionWithListMissingProperty_ReturnsError()
    {
        // Arrange
        var code = @"
using System.Collections.Generic;

namespace Test
{
    public class Item
    {
        public string Name { get; set; }
    }

    public class TestContext
    {
        public List<Item> Items { get; set; }
    }
}";
        var template = "{{#Items}}{{Name}}: ${{Price}}{{/Items}}";
        var symbol = GetTypeSymbol(code, "TestContext");

        // Act
        var errors = MustacheSymbolValidator.ValidateTemplate(template, symbol);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Price", errors[0]);
    }

    [Fact]
    public void ValidateTemplate_SectionMissingProperty_ReturnsError()
    {
        // Arrange
        var code = @"
namespace Test
{
    public class TestContext
    {
        public string Name { get; set; }
    }
}";
        var template = "{{#Items}}{{Name}}{{/Items}}";
        var symbol = GetTypeSymbol(code, "TestContext");

        // Act
        var errors = MustacheSymbolValidator.ValidateTemplate(template, symbol);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Items", errors[0]);
        Assert.Contains("not found", errors[0]);
    }

    [Fact]
    public void ValidateTemplate_InvertedSection_ValidatesCorrectly()
    {
        // Arrange
        var code = @"
namespace Test
{
    public class TestContext
    {
        public bool HasItems { get; set; }
    }
}";
        var template = "{{^HasItems}}No items available{{/HasItems}}";
        var symbol = GetTypeSymbol(code, "TestContext");

        // Act
        var errors = MustacheSymbolValidator.ValidateTemplate(template, symbol);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateTemplate_CaseInsensitive_ValidatesCorrectly()
    {
        // Arrange
        var code = @"
namespace Test
{
    public class TestContext
    {
        public string UserName { get; set; }
        public int UserAge { get; set; }
    }
}";
        var template = "Hello {{username}}, you are {{userage}} years old";
        var symbol = GetTypeSymbol(code, "TestContext");

        // Act
        var errors = MustacheSymbolValidator.ValidateTemplate(template, symbol);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateTemplate_ComplexNestedStructure_ValidatesCorrectly()
    {
        // Arrange
        var code = @"
using System.Collections.Generic;

namespace Test
{
    public class Tag
    {
        public string Name { get; set; }
        public string Color { get; set; }
    }

    public class Post
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public List<Tag> Tags { get; set; }
    }

    public class TestContext
    {
        public string Author { get; set; }
        public List<Post> Posts { get; set; }
    }
}";
        var template = @"
Author: {{Author}}
{{#Posts}}
Post: {{Title}}
{{Content}}
Tags: {{#Tags}}{{Name}} ({{Color}}){{/Tags}}
{{/Posts}}";
        var symbol = GetTypeSymbol(code, "TestContext");

        // Act
        var errors = MustacheSymbolValidator.ValidateTemplate(template, symbol);

        // Assert - 验证仍然检查所有嵌套路径的有效性
        Assert.Empty(errors);
    }

    [Fact]
    public void ExtractPlaceholders_ComplexNestedStructure_ExtractsRootPropertiesOnly()
    {
        // Arrange
        var template = @"
Author: {{Author}}
{{#Posts}}
Post: {{Title}}
{{Content}}
Tags: {{#Tags}}{{Name}} ({{Color}}){{/Tags}}
{{/Posts}}";

        // Act
        var placeholders = MustacheSymbolValidator.ExtractPlaceholders(template);

        // Assert - 只提取顶层根属性：Author 和 Posts（不递归到 Section 内部）
        Assert.Equal(2, placeholders.Count);
        Assert.Contains("Author", placeholders);
        Assert.Contains("Posts", placeholders);
    }

    /// <summary>
    /// Helper method to create a type symbol from C# code
    /// </summary>
    private INamedTypeSymbol GetTypeSymbol(string code, string typeName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();

        // Find the type declaration
        var typeDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>()
            .First(t => t.Identifier.Text == typeName);

        var symbol = semanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;

        if (symbol == null)
            throw new InvalidOperationException($"Could not find type symbol for {typeName}");

        return symbol;
    }
}
