using Yamlify.Exceptions;
using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Tests for recursion depth limiting during serialization and deserialization.
/// These tests verify protection against deeply nested structures and circular references.
/// </summary>
public class RecursionDepthTests
{
    #region Test Models

    /// <summary>
    /// A recursive model that can reference itself for testing circular references.
    /// </summary>
    public class RecursiveNode
    {
        public string? Name { get; set; }
        public RecursiveNode? Child { get; set; }
    }

    /// <summary>
    /// A model with a deeply nested list structure.
    /// </summary>
    public class NestedContainer
    {
        public string? Name { get; set; }
        public List<NestedContainer>? Children { get; set; }
    }

    /// <summary>
    /// A simple model for basic tests.
    /// </summary>
    public class SimpleModel
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    #endregion

    #region MaxDepth Option Tests

    [Fact]
    public void MaxDepth_DefaultValue_IsSixtyFour()
    {
        var options = new YamlSerializerOptions();
        Assert.Equal(64, options.MaxDepth);
    }

    [Fact]
    public void MaxDepth_CanBeSetToValidValue()
    {
        var options = new YamlSerializerOptions { MaxDepth = 128 };
        Assert.Equal(128, options.MaxDepth);
    }

    [Fact]
    public void MaxDepth_CanBeSetToMinimumValue()
    {
        var options = new YamlSerializerOptions { MaxDepth = 1 };
        Assert.Equal(1, options.MaxDepth);
    }

    [Fact]
    public void MaxDepth_CanBeSetToMaxAllowedValue()
    {
        var options = new YamlSerializerOptions { MaxDepth = 1000 };
        Assert.Equal(1000, options.MaxDepth);
    }

    [Fact]
    public void MaxDepth_ThrowsForZero()
    {
        var options = new YamlSerializerOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDepth = 0);
    }

    [Fact]
    public void MaxDepth_ThrowsForNegativeValue()
    {
        var options = new YamlSerializerOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDepth = -1);
    }

    [Fact]
    public void MaxDepth_ThrowsForValueOverMaxAllowed()
    {
        var options = new YamlSerializerOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDepth = 1001);
    }

    [Fact]
    public void MaxDepth_ReadOnlyOptionsThrowsOnSet()
    {
        var options = YamlSerializerOptions.Default;
        Assert.Throws<InvalidOperationException>(() => options.MaxDepth = 100);
    }

    #endregion

    #region Serialization Depth Tests

    [Fact]
    public void Serialize_DeeplyNestedObject_Succeeds()
    {
        var root = CreateDeeplyNestedNode(10);
        var yaml = YamlSerializer.Serialize(root, TestSerializerContext.Default.RecursiveNode);
        
        Assert.Contains("name: Level1", yaml);
        Assert.Contains("child:", yaml);
    }

    [Fact]
    public void Serialize_FlatStructure_SucceedsWithMinimalDepth()
    {
        var obj = new SimpleModel { Name = "Test", Value = 42 };
        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SimpleModel);
        
        Assert.Contains("name: Test", yaml);
        Assert.Contains("value: 42", yaml);
    }

    [Fact]
    public void Serialize_NestedList_Succeeds()
    {
        var obj = new NestedContainer 
        { 
            Name = "Root",
            Children = new List<NestedContainer>
            {
                new NestedContainer { Name = "Child1" },
                new NestedContainer { Name = "Child2" }
            }
        };
        
        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.NestedContainer);
        
        Assert.Contains("name: Root", yaml);
        Assert.Contains("children:", yaml);
        Assert.Contains("Child1", yaml);
        Assert.Contains("Child2", yaml);
    }

    #endregion

    #region Deserialization Depth Tests

    [Fact]
    public void Deserialize_DeeplyNestedYaml_ThrowsForExcessiveDepth()
    {
        // Generate YAML that exceeds the default 64 depth limit
        var yaml = GenerateDeeplyNestedMappingYaml(70);
        
        Assert.ThrowsAny<Exception>(() => 
            YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.RecursiveNode));
    }

    [Fact]
    public void Deserialize_WithinDepthLimit_Succeeds()
    {
        var yaml = """
            name: Level1
            child:
              name: Level2
              child:
                name: Level3
            """;
        
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.RecursiveNode);
        
        Assert.NotNull(result);
        Assert.Equal("Level1", result.Name);
        Assert.NotNull(result.Child);
        Assert.Equal("Level2", result.Child.Name);
        Assert.NotNull(result.Child.Child);
        Assert.Equal("Level3", result.Child.Child.Name);
    }

    [Fact]
    public void Deserialize_FlatStructure_SucceedsWithAnyDepth()
    {
        var yaml = """
            name: Simple
            value: 100
            """;
        
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleModel);
        
        Assert.NotNull(result);
        Assert.Equal("Simple", result.Name);
        Assert.Equal(100, result.Value);
    }

    #endregion

    #region Exception Details Tests

    [Fact]
    public void MaxRecursionDepthExceededException_ContainsHelpfulMessage()
    {
        var ex = new MaxRecursionDepthExceededException(64, 65);
        
        Assert.Contains("64", ex.Message);
        Assert.Contains("65", ex.Message);
        Assert.Contains("depth", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MaxRecursionDepthExceededException_StoresDepthValues()
    {
        var ex = new MaxRecursionDepthExceededException(64, 65);
        
        Assert.Equal(64, ex.MaxDepth);
        Assert.Equal(65, ex.CurrentDepth);
    }

    [Fact]
    public void MaxRecursionDepthExceededException_IsYamlException()
    {
        var ex = new MaxRecursionDepthExceededException(64, 65);
        Assert.IsAssignableFrom<YamlException>(ex);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Deserialize_EmptyYaml_DoesNotThrowDepthException()
    {
        var yaml = "";
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleModel);
        // Should not throw depth exception
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_NullYaml_DoesNotThrowDepthException()
    {
        string? yaml = null;
        var result = YamlSerializer.Deserialize(yaml!, TestSerializerContext.Default.SimpleModel);
        // Should not throw depth exception
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_ScalarValue_DoesNotThrowDepthException()
    {
        var yaml = "name: Test";
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleModel);
        
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }

    #endregion

    #region Helpers

    private static string GenerateDeeplyNestedMappingYaml(int depth)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("name: Level1");
        for (int i = 1; i < depth; i++)
        {
            sb.Append(new string(' ', i * 2));
            sb.AppendLine("child:");
            sb.Append(new string(' ', (i + 1) * 2));
            sb.AppendLine($"name: Level{i + 1}");
        }
        return sb.ToString();
    }

    private static RecursiveNode CreateDeeplyNestedNode(int depth)
    {
        var root = new RecursiveNode { Name = "Level1" };
        var current = root;

        for (int i = 2; i <= depth; i++)
        {
            current.Child = new RecursiveNode { Name = $"Level{i}" };
            current = current.Child;
        }

        return root;
    }

    #endregion
}
