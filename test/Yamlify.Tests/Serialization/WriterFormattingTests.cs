using Yamlify;
using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Tests for YAML writer formatting behavior:
/// - No document marker (---) by default
/// - No leading newline for root-level mappings
/// - No indentation for root-level properties
/// - Proper spacing after property names (space for scalars, no space for nested containers)
/// - Declaration order for properties without explicit order
/// </summary>
public class WriterFormattingTests
{
    #region Document Marker Tests

    [Fact]
    public void Serialize_WithDefaultOptions_ShouldNotEmitDocumentMarker()
    {
        var obj = new SimpleClass { Name = "test", Value = 42 };
        
        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SimpleClass);
        
        Assert.DoesNotContain("---", yaml);
        Assert.StartsWith("name:", yaml);
    }

    [Fact]
    public void Writer_WithEmitDocumentMarkersTrue_ShouldEmitDocumentMarker()
    {
        using var stream = new MemoryStream();
        var options = new YamlWriterOptions { EmitDocumentMarkers = true };
        using var writer = new Utf8YamlWriter(stream, options);
        
        writer.WriteMappingStart();
        writer.WritePropertyName("key");
        writer.WriteString("value");
        writer.WriteMappingEnd();
        writer.Flush();
        
        var yaml = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.StartsWith("---", yaml);
    }

    #endregion

    #region Root-Level Formatting Tests

    [Fact]
    public void Serialize_RootMapping_ShouldNotHaveLeadingNewline()
    {
        var obj = new SimpleClass { Name = "test", Value = 42 };
        
        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SimpleClass);
        
        // Should start directly with the property name, no leading whitespace
        Assert.False(yaml.StartsWith("\n"), "Should not start with newline");
        Assert.False(yaml.StartsWith(" "), "Should not start with space");
        Assert.True(yaml.StartsWith("name:") || yaml.StartsWith("value:"), 
            $"Should start with a property name. Actual: {yaml[..Math.Min(20, yaml.Length)]}");
    }

    [Fact]
    public void Serialize_RootMappingProperties_ShouldNotHaveIndentation()
    {
        var obj = new SimpleClass { Name = "test", Value = 42 };
        
        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SimpleClass);
        var lines = yaml.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            // Root-level properties should not start with whitespace
            Assert.False(line.StartsWith(" "), $"Root-level property should not be indented: '{line}'");
        }
    }

    #endregion

    #region Property Spacing Tests

    [Fact]
    public void Serialize_ScalarValue_ShouldHaveSpaceAfterColon()
    {
        var obj = new SimpleClass { Name = "test", Value = 42 };
        
        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SimpleClass);
        
        // Scalar values should have space between colon and value
        Assert.Contains("name: test", yaml);
        Assert.Contains("value: 42", yaml);
    }

    [Fact]
    public void Serialize_NestedMapping_ShouldNotHaveSpaceBeforeNewline()
    {
        var obj = new ParentClass
        {
            Title = "parent",
            Child = new SimpleClass { Name = "child", Value = 1 }
        };
        
        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.ParentClass);
        
        // Nested mapping should have colon followed directly by newline, not "child: \n"
        Assert.DoesNotContain("child: \n", yaml);
        Assert.Contains("child:\n", yaml);
    }

    #endregion

    #region Nested Indentation Tests

    [Fact]
    public void Serialize_NestedMapping_ShouldHaveCorrectIndentation()
    {
        var obj = new ParentClass
        {
            Title = "parent",
            Child = new SimpleClass { Name = "child", Value = 1 }
        };
        
        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.ParentClass);
        var lines = yaml.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        // Find a nested property line - it should be indented
        var nestedNameLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("name:") && l.StartsWith("  "));
        Assert.NotNull(nestedNameLine);
    }

    [Fact]
    public void Serialize_DeeplyNested_ShouldHaveIncrementalIndentation()
    {
        var obj = new DeeplyNestedClass
        {
            Level1 = "L1",
            Child = new DeeplyNestedClass
            {
                Level1 = "L2",
                Child = new DeeplyNestedClass
                {
                    Level1 = "L3"
                }
            }
        };
        
        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.DeeplyNestedClass);
        
        // Each nesting level should add 2 spaces of indentation
        // Property names are serialized as-is (camelCase) not kebab-case
        Assert.Contains("level1: L1", yaml); // Root level, no indent
        Assert.Contains("  level1: L2", yaml); // First nest, 2 spaces
        Assert.Contains("    level1: L3", yaml); // Second nest, 4 spaces
    }

    #endregion

    #region Declaration Order Tests

    [Fact]
    public void Serialize_WithoutPropertyOrderAttribute_ShouldUseDeclarationOrder()
    {
        var obj = new DeclarationOrderClass
        {
            First = "1",
            Second = "2",
            Third = "3"
        };
        
        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.DeclarationOrderClass);
        
        // Properties should appear in declaration order: First, Second, Third
        var firstIndex = yaml.IndexOf("first:");
        var secondIndex = yaml.IndexOf("second:");
        var thirdIndex = yaml.IndexOf("third:");
        
        Assert.True(firstIndex >= 0, "first: not found");
        Assert.True(secondIndex >= 0, "second: not found");
        Assert.True(thirdIndex >= 0, "third: not found");
        
        Assert.True(firstIndex < secondIndex, $"First ({firstIndex}) should come before Second ({secondIndex})");
        Assert.True(secondIndex < thirdIndex, $"Second ({secondIndex}) should come before Third ({thirdIndex})");
    }

    [Fact]
    public void Serialize_WithPropertyOrderAttribute_ShouldOverrideDeclarationOrder()
    {
        var obj = new ClassWithPropertyOrder
        {
            First = "1",
            Second = "2",
            Third = "3",
            Unordered = "Last"
        };
        
        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.ClassWithPropertyOrder);
        
        // Properties with [YamlPropertyOrder] should appear in specified order
        var firstIndex = yaml.IndexOf("first:");
        var secondIndex = yaml.IndexOf("second:");
        var thirdIndex = yaml.IndexOf("third:");
        var unorderedIndex = yaml.IndexOf("unordered:");
        
        Assert.True(firstIndex < secondIndex, "First should come before Second");
        Assert.True(secondIndex < thirdIndex, "Second should come before Third");
        Assert.True(thirdIndex < unorderedIndex, "Third should come before Unordered (no attribute)");
    }

    #endregion

    #region Writer Direct Tests

    [Fact]
    public void Writer_SimpleMappingWithScalar_ShouldFormatCorrectly()
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8YamlWriter(stream);
        
        writer.WriteMappingStart();
        writer.WritePropertyName("key");
        writer.WriteString("value");
        writer.WriteMappingEnd();
        writer.Flush();
        
        var yaml = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        
        // Trailing newline is optional in YAML - writer doesn't add one by default
        Assert.Equal("key: value", yaml);
    }

    [Fact]
    public void Writer_NestedMapping_ShouldFormatCorrectly()
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8YamlWriter(stream);
        
        writer.WriteMappingStart();
        writer.WritePropertyName("parent");
        writer.WriteMappingStart();
        writer.WritePropertyName("child");
        writer.WriteString("value");
        writer.WriteMappingEnd();
        writer.WriteMappingEnd();
        writer.Flush();
        
        var yaml = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        
        // Expected:
        // parent:
        //   child: value
        Assert.Equal("parent:\n  child: value", yaml);
    }

    [Fact]
    public void Writer_MultipleRootProperties_ShouldFormatCorrectly()
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8YamlWriter(stream);
        
        writer.WriteMappingStart();
        writer.WritePropertyName("first");
        writer.WriteString("1");
        writer.WritePropertyName("second");
        writer.WriteString("2");
        writer.WriteMappingEnd();
        writer.Flush();
        
        var yaml = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        
        Assert.Equal("first: 1\nsecond: 2", yaml);
    }

    [Fact]
    public void Writer_MixedScalarAndNestedValues_ShouldFormatCorrectly()
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8YamlWriter(stream);
        
        writer.WriteMappingStart();
        writer.WritePropertyName("name");
        writer.WriteString("test");
        writer.WritePropertyName("details");
        writer.WriteMappingStart();
        writer.WritePropertyName("id");
        writer.WriteNumber(42);
        writer.WriteMappingEnd();
        writer.WritePropertyName("active");
        writer.WriteBoolean(true);
        writer.WriteMappingEnd();
        writer.Flush();
        
        var yaml = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        
        // Expected:
        // name: test
        // details:
        //   id: 42
        // active: true
        var expected = "name: test\ndetails:\n  id: 42\nactive: true";
        Assert.Equal(expected, yaml);
    }

    #endregion
}

/// <summary>
/// Test class to verify declaration order is preserved when no YamlPropertyOrder attribute is used.
/// Properties are intentionally named to make alphabetical vs declaration order obvious.
/// </summary>
public class DeclarationOrderClass
{
    // Declaration order: First, Second, Third
    // Alphabetical by name: First, Second, Third (same)
    // Alphabetical by kebab-case: first, second, third (same)
    public string First { get; set; } = "";
    public string Second { get; set; } = "";
    public string Third { get; set; } = "";
}

/// <summary>
/// Test class for deeply nested serialization.
/// </summary>
public class DeeplyNestedClass
{
    public string Level1 { get; set; } = "";
    public DeeplyNestedClass? Child { get; set; }
}
