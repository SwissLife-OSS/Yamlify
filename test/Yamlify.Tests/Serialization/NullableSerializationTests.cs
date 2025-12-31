using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Class with nullable value types.
/// </summary>
public class NullableTypesClass
{
    public int? NullableInt { get; set; }
    public double? NullableDouble { get; set; }
    public bool? NullableBool { get; set; }
    public string? NullableString { get; set; }
}

/// <summary>
/// Tests for serializing and deserializing nullable types.
/// </summary>
public class NullableSerializationTests
{
    [Fact]
    public void SerializeNullableWithValues()
    {
        var obj = new NullableTypesClass
        {
            NullableInt = 42,
            NullableDouble = 3.14,
            NullableBool = true,
            NullableString = "test"
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.NullableTypesClass);

        Assert.Contains("42", yaml);
        Assert.Contains("3.14", yaml);
        Assert.Contains("true", yaml);
        Assert.Contains("test", yaml);
    }

    [Fact]
    public void SerializeNullableWithNullValues()
    {
        var obj = new NullableTypesClass
        {
            NullableInt = null,
            NullableDouble = null,
            NullableBool = null,
            NullableString = null
        };
        var options = new YamlSerializerOptions { IgnoreNullValues = false };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.NullableTypesClass, options);

        Assert.Contains("null", yaml);
    }

    [Fact]
    public void DeserializeNullableWithValues()
    {
        var yaml = """
            nullable-int: 123
            nullable-double: 9.99
            nullable-bool: false
            nullable-string: hello
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.NullableTypesClass);

        Assert.NotNull(result);
        Assert.Equal(123, result.NullableInt);
        Assert.Equal(9.99, result.NullableDouble);
        Assert.False(result.NullableBool);
        Assert.Equal("hello", result.NullableString);
    }

    [Fact]
    public void DeserializeNullableWithNullValues()
    {
        // YAML null literal (unquoted "null") represents null value for all types
        var yaml = """
            nullable-int: null
            nullable-double: null
            nullable-bool: null
            nullable-string: null
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.NullableTypesClass);

        Assert.NotNull(result);
        // All nullable types get null when YAML value is the null literal
        Assert.Null(result.NullableInt);
        Assert.Null(result.NullableDouble);
        Assert.Null(result.NullableBool);
        Assert.Null(result.NullableString);
    }

    [Fact]
    public void DeserializeNullableWithMissingProperties()
    {
        var yaml = """
            nullable-int: 42
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.NullableTypesClass);

        Assert.NotNull(result);
        Assert.Equal(42, result.NullableInt);
        Assert.Null(result.NullableDouble);
        Assert.Null(result.NullableBool);
        Assert.Null(result.NullableString);
    }

    [Fact]
    public void RoundTripNullableWithValues()
    {
        var original = new NullableTypesClass
        {
            NullableInt = 999,
            NullableDouble = 1.5,
            NullableBool = true,
            NullableString = "roundtrip"
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.NullableTypesClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.NullableTypesClass);

        Assert.NotNull(result);
        Assert.Equal(original.NullableInt, result.NullableInt);
        Assert.Equal(original.NullableDouble, result.NullableDouble);
        Assert.Equal(original.NullableBool, result.NullableBool);
        Assert.Equal(original.NullableString, result.NullableString);
    }

    [Fact]
    public void RoundTripNullableWithNullValues()
    {
        // All nullable types including strings should correctly roundtrip null values.
        // YAML "null" keyword is properly recognized and deserialized as null.
        var original = new NullableTypesClass
        {
            NullableInt = null,
            NullableDouble = null,
            NullableBool = null,
            NullableString = null
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.NullableTypesClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.NullableTypesClass);

        Assert.NotNull(result);
        // All nullable types correctly roundtrip null
        Assert.Null(result.NullableInt);
        Assert.Null(result.NullableDouble);
        Assert.Null(result.NullableBool);
        Assert.Null(result.NullableString);
    }

    [Fact]
    public void SerializeWithNullProperties()
    {
        var obj = new MixedTypesClass
        {
            Name = null,
            Count = 0,
            Ratio = null,
            Created = DateTime.MinValue,
            Id = Guid.Empty,
            Tags = null,
            Scores = null,
            Nested = null
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.MixedTypesClass);

        Assert.Contains("count:", yaml);
        Assert.Contains("id:", yaml);
    }

    [Fact]
    public void DeserializeWithMissingProperties()
    {
        var yaml = """
            name: Partial
            count: 5
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.MixedTypesClass);

        Assert.NotNull(obj);
        Assert.Equal("Partial", obj.Name);
        Assert.Equal(5, obj.Count);
        Assert.Null(obj.Ratio);
        Assert.Null(obj.Tags);
        Assert.Null(obj.Nested);
    }
}
