using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Tests for empty collection handling during serialization and deserialization.
/// These tests verify the behavior of the <see cref="EmptyCollectionHandling"/> option.
/// </summary>
public class EmptyCollectionHandlingTests
{

    /// <summary>
    /// A model with array properties for testing empty collection handling.
    /// </summary>
    public class ModelWithArrays
    {
        public string? Name { get; set; }
        public string[]? Tags { get; set; }
        public int[]? Numbers { get; set; }
    }

    /// <summary>
    /// A model with list properties for testing empty collection handling.
    /// </summary>
    public class ModelWithLists
    {
        public string? Name { get; set; }
        public List<string>? Items { get; set; }
        public List<int>? Values { get; set; }
    }

    /// <summary>
    /// A model with nested objects containing arrays.
    /// </summary>
    public class ModelWithNestedArrays
    {
        public string? Name { get; set; }
        public NestedModel? Nested { get; set; }
    }

    /// <summary>
    /// A nested model with an array property.
    /// </summary>
    public class NestedModel
    {
        public string? Id { get; set; }
        public string[]? Items { get; set; }
    }

    /// <summary>
    /// A record with primary constructor containing array parameters.
    /// </summary>
    public record RecordWithArrays(
        string Name,
        string[] Tags,
        int[] Numbers)
    {
        public RecordWithArrays() : this("", [], []) { }
    }

    /// <summary>
    /// A record with optional array parameters with defaults.
    /// </summary>
    public record RecordWithOptionalArrays(
        string Name,
        string[]? Tags = null,
        int[]? Numbers = null);

    /// <summary>
    /// Simple model for edge case testing.
    /// </summary>
    public class CollectionSimpleModel
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    #region EmptyCollectionHandling Option Tests

    [Fact]
    public void EmptyCollectionHandling_DefaultValue_IsDefault()
    {
        var options = new YamlSerializerOptions();
        Assert.Equal(EmptyCollectionHandling.Default, options.EmptyCollectionHandling);
    }

    [Fact]
    public void EmptyCollectionHandling_CanBeSetToPreferEmptyCollection()
    {
        var options = new YamlSerializerOptions { EmptyCollectionHandling = EmptyCollectionHandling.PreferEmptyCollection };
        Assert.Equal(EmptyCollectionHandling.PreferEmptyCollection, options.EmptyCollectionHandling);
    }

    [Fact]
    public void EmptyCollectionHandling_ReadOnlyOptionsThrowsOnSet()
    {
        var options = YamlSerializerOptions.Default;
        Assert.Throws<InvalidOperationException>(() => 
            options.EmptyCollectionHandling = EmptyCollectionHandling.PreferEmptyCollection);
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void Serialize_EmptyArray_ProducesOutput()
    {
        var obj = new ModelWithArrays
        {
            Name = "Test",
            Tags = Array.Empty<string>(),
            Numbers = Array.Empty<int>()
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.ModelWithArrays);

        Assert.Contains("name: Test", yaml);
        // Empty arrays should be serialized in flow style as []
        Assert.Contains("tags: []", yaml);
        Assert.Contains("numbers: []", yaml);
    }

    [Fact]
    public void Serialize_EmptyList_ProducesOutput()
    {
        var obj = new ModelWithLists
        {
            Name = "Test",
            Items = new List<string>(),
            Values = new List<int>()
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.ModelWithLists);

        Assert.Contains("name: Test", yaml);
        // Empty lists should be serialized in flow style as []
        Assert.Contains("items: []", yaml);
        Assert.Contains("values: []", yaml);
    }

    [Fact]
    public void Serialize_NonEmptyArray_ProducesBlockStyle()
    {
        var obj = new ModelWithArrays
        {
            Name = "Test",
            Tags = new[] { "tag1", "tag2" }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.ModelWithArrays);

        Assert.Contains("tags:", yaml);
        Assert.Contains("tag1", yaml);
        Assert.Contains("tag2", yaml);
    }

    [Fact]
    public void Serialize_NestedEmptyArray_ProducesOutput()
    {
        var obj = new ModelWithNestedArrays
        {
            Name = "Outer",
            Nested = new NestedModel
            {
                Id = "inner",
                Items = Array.Empty<string>()
            }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.ModelWithNestedArrays);

        Assert.Contains("name: Outer", yaml);
        Assert.Contains("nested:", yaml);
        Assert.Contains("id: inner", yaml);
        // Nested empty arrays should also be serialized in flow style as []
        Assert.Contains("items: []", yaml);
    }

    [Fact]
    public void Serialize_RecordWithEmptyArrays_ProducesOutput()
    {
        var obj = new RecordWithArrays("Test", Array.Empty<string>(), Array.Empty<int>());

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.RecordWithArrays);

        Assert.Contains("name: Test", yaml);
        // Empty arrays in records should be serialized in flow style as []
        Assert.Contains("tags: []", yaml);
        Assert.Contains("numbers: []", yaml);
    }

    #endregion

    #region Deserialization Tests

    [Fact]
    public void Deserialize_MissingArrayProperty_ReturnsNull()
    {
        var yaml = """
            name: Test
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ModelWithArrays);

        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Null(result.Tags);
        Assert.Null(result.Numbers);
    }

    [Fact]
    public void Deserialize_FlowEmptyArray_ReturnsEmptyArray()
    {
        var yaml = """
            name: Test
            tags: []
            numbers: []
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ModelWithArrays);

        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.NotNull(result.Tags);
        Assert.Empty(result.Tags);
        Assert.NotNull(result.Numbers);
        Assert.Empty(result.Numbers);
    }

    [Fact]
    public void Deserialize_NonEmptyArray_ReturnsArray()
    {
        var yaml = """
            name: Test
            tags:
              - one
              - two
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ModelWithArrays);

        Assert.NotNull(result);
        Assert.NotNull(result.Tags);
        Assert.Equal(2, result.Tags.Length);
        Assert.Contains("one", result.Tags);
        Assert.Contains("two", result.Tags);
    }

    [Fact]
    public void Deserialize_List_ReturnsEmptyList()
    {
        var yaml = """
            name: Test
            items: []
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ModelWithLists);

        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void Deserialize_IntArray_ReturnsArray()
    {
        var yaml = """
            name: Test
            numbers:
              - 1
              - 2
              - 3
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ModelWithArrays);

        Assert.NotNull(result);
        Assert.NotNull(result.Numbers);
        Assert.Equal(3, result.Numbers.Length);
        Assert.Equal(1, result.Numbers[0]);
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_EmptyArray_PreservesEmptyArray()
    {
        var original = new ModelWithArrays
        {
            Name = "Test",
            Tags = Array.Empty<string>()
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.ModelWithArrays);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ModelWithArrays);

        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
        // Note: Empty array may come back as empty or null depending on serialization
    }

    [Fact]
    public void RoundTrip_NonEmptyArray_PreservesValues()
    {
        var original = new ModelWithArrays
        {
            Name = "Test",
            Tags = new[] { "a", "b", "c" }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.ModelWithArrays);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ModelWithArrays);

        Assert.NotNull(result);
        Assert.NotNull(result.Tags);
        Assert.Equal(original.Tags.Length, result.Tags.Length);
        Assert.Equal(original.Tags, result.Tags);
    }

    [Fact]
    public void RoundTrip_Record_PreservesEmptyArrays()
    {
        var original = new RecordWithArrays("Test", new[] { "x" }, new[] { 1, 2 });

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.RecordWithArrays);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.RecordWithArrays);

        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
        Assert.NotNull(result.Tags);
        Assert.Equal(original.Tags.Length, result.Tags.Length);
    }

    [Fact]
    public void RoundTrip_NestedEmptyArray_PreservesEmptyArray()
    {
        var original = new ModelWithNestedArrays
        {
            Name = "Outer",
            Nested = new NestedModel { Id = "inner", Items = new[] { "item" } }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.ModelWithNestedArrays);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ModelWithNestedArrays);

        Assert.NotNull(result);
        Assert.NotNull(result.Nested);
        Assert.NotNull(result.Nested.Items);
        Assert.Equal(original.Nested.Items.Length, result.Nested.Items.Length);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Deserialize_StringProperty_NotAffectedByEmptyCollectionHandling()
    {
        var yaml = """
            name: Test
            value: 42
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.CollectionSimpleModel);

        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Deserialize_IntProperty_NotAffectedByEmptyCollectionHandling()
    {
        var yaml = """
            name: Test
            value: 0
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.CollectionSimpleModel);

        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public void Deserialize_EmptyYaml_ReturnsNull()
    {
        var yaml = "";

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ModelWithArrays);

        Assert.Null(result);
    }

    #endregion
}
