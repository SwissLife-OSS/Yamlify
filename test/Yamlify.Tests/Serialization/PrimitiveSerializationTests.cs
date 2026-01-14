using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Class with common primitive types supported by the serializer.
/// </summary>
public class AllPrimitivesClass
{
    public int IntValue { get; set; }
    public long LongValue { get; set; }
    public float FloatValue { get; set; }
    public double DoubleValue { get; set; }
    public decimal DecimalValue { get; set; }
    public bool BoolValue { get; set; }
    public string? StringValue { get; set; }
}

/// <summary>
/// Class for testing special float values.
/// </summary>
public class SpecialNumbersClass
{
    public double Infinity { get; set; }
    public double NegativeInfinity { get; set; }
    public double NaN { get; set; }
}

/// <summary>
/// Tests for serializing and deserializing primitive types.
/// </summary>
public class PrimitiveSerializationTests
{
    [Fact]
    public void SerializePrimitives()
    {
        var obj = new AllPrimitivesClass
        {
            IntValue = 42,
            LongValue = 9223372036854775807,
            FloatValue = 3.14f,
            DoubleValue = 3.14159265359,
            DecimalValue = 123.456m,
            BoolValue = true,
            StringValue = "Hello World"
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.AllPrimitivesClass);

        Assert.Contains("42", yaml);
        Assert.Contains("true", yaml);
        Assert.Contains("Hello World", yaml);
    }

    [Fact]
    public void DeserializePrimitives()
    {
        var yaml = """
            int-value: 123456
            long-value: 9999999999
            float-value: 2.5
            double-value: 3.14159
            decimal-value: 99.99
            bool-value: true
            string-value: Test String
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.AllPrimitivesClass);

        Assert.NotNull(obj);
        Assert.Equal(123456, obj.IntValue);
        Assert.Equal(9999999999L, obj.LongValue);
        Assert.True(obj.BoolValue);
        Assert.Equal("Test String", obj.StringValue);
    }

    [Fact]
    public void SerializeSpecialFloatValues()
    {
        var obj = new SpecialNumbersClass
        {
            Infinity = double.PositiveInfinity,
            NegativeInfinity = double.NegativeInfinity,
            NaN = double.NaN
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SpecialNumbersClass);

        Assert.Contains(".inf", yaml);
        Assert.Contains("-.inf", yaml);
        Assert.Contains(".nan", yaml);
    }

    [Fact]
    public void DeserializeSpecialFloatValues()
    {
        var yaml = """
            infinity: .inf
            negative-infinity: -.inf
            na-n: .nan
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SpecialNumbersClass);

        Assert.NotNull(obj);
        Assert.True(double.IsPositiveInfinity(obj.Infinity));
        Assert.True(double.IsNegativeInfinity(obj.NegativeInfinity));
        Assert.True(double.IsNaN(obj.NaN));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("True", true)]
    [InlineData("False", false)]
    public void DeserializeBooleanVariants(string yamlValue, bool expected)
    {
        var yaml = $"bool-value: {yamlValue}";

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.AllPrimitivesClass);

        Assert.NotNull(obj);
        Assert.Equal(expected, obj.BoolValue);
    }

    [Fact]
    public void SerializeAllNumericTypes()
    {
        var obj = new AllNumericTypesClass
        {
            ByteValue = 255,
            SByteValue = -128,
            ShortValue = -32768,
            UShortValue = 65535,
            UIntValue = 4294967295,
            ULongValue = 18446744073709551615,
            CharValue = 'Z'
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.AllNumericTypesClass);

        Assert.Contains("255", yaml);
        Assert.Contains("-128", yaml);
        Assert.Contains("-32768", yaml);
        Assert.Contains("65535", yaml);
        Assert.Contains("4294967295", yaml);
        Assert.Contains("18446744073709551615", yaml);
        Assert.Contains("Z", yaml);
    }

    [Fact]
    public void DeserializeAllNumericTypes()
    {
        var yaml = """
            byte-value: 200
            s-byte-value: -100
            short-value: -1000
            u-short-value: 60000
            u-int-value: 3000000000
            u-long-value: 10000000000000000000
            char-value: X
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.AllNumericTypesClass);

        Assert.NotNull(obj);
        Assert.Equal((byte)200, obj.ByteValue);
        Assert.Equal((sbyte)-100, obj.SByteValue);
        Assert.Equal((short)-1000, obj.ShortValue);
        Assert.Equal((ushort)60000, obj.UShortValue);
        Assert.Equal(3000000000U, obj.UIntValue);
        Assert.Equal(10000000000000000000UL, obj.ULongValue);
        Assert.Equal('X', obj.CharValue);
    }

    [Fact]
    public void RoundTripAllNumericTypes()
    {
        var original = new AllNumericTypesClass
        {
            ByteValue = 123,
            SByteValue = -64,
            ShortValue = 12345,
            UShortValue = 54321,
            UIntValue = 1234567890,
            ULongValue = 12345678901234567890,
            CharValue = 'A'
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.AllNumericTypesClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.AllNumericTypesClass);

        Assert.NotNull(result);
        Assert.Equal(original.ByteValue, result.ByteValue);
        Assert.Equal(original.SByteValue, result.SByteValue);
        Assert.Equal(original.ShortValue, result.ShortValue);
        Assert.Equal(original.UShortValue, result.UShortValue);
        Assert.Equal(original.UIntValue, result.UIntValue);
        Assert.Equal(original.ULongValue, result.ULongValue);
        Assert.Equal(original.CharValue, result.CharValue);
    }

    [Fact]
    public void RoundTripAllPrimitives()
    {
        var original = new AllPrimitivesClass
        {
            IntValue = int.MaxValue,
            LongValue = long.MaxValue,
            FloatValue = 1.5f,
            DoubleValue = 2.5,
            DecimalValue = 99.99m,
            BoolValue = true,
            StringValue = "RoundTrip Test"
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.AllPrimitivesClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.AllPrimitivesClass);

        Assert.NotNull(result);
        Assert.Equal(original.IntValue, result.IntValue);
        Assert.Equal(original.LongValue, result.LongValue);
        Assert.Equal(original.BoolValue, result.BoolValue);
        Assert.Equal(original.StringValue, result.StringValue);
    }

    [Fact]
    public void SerializeNegativeNumbers()
    {
        var obj = new AllPrimitivesClass
        {
            IntValue = -42,
            LongValue = -9223372036854775808,
            FloatValue = -3.14f,
            DoubleValue = -2.71828,
            DecimalValue = -123.456m,
            BoolValue = false,
            StringValue = null
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.AllPrimitivesClass);

        Assert.Contains("-42", yaml);
        Assert.Contains("-3.14", yaml);
        Assert.Contains("-2.71828", yaml);
    }

    [Fact]
    public void DeserializeNegativeNumbers()
    {
        var yaml = """
            int-value: -999
            long-value: -999999999999
            float-value: -1.5
            double-value: -2.5
            decimal-value: -50.25
            bool-value: false
            string-value: negative
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.AllPrimitivesClass);

        Assert.NotNull(obj);
        Assert.Equal(-999, obj.IntValue);
        Assert.Equal(-999999999999L, obj.LongValue);
        Assert.False(obj.BoolValue);
    }

    [Fact]
    public void SerializeZeroValues()
    {
        var obj = new AllPrimitivesClass
        {
            IntValue = 0,
            LongValue = 0,
            FloatValue = 0f,
            DoubleValue = 0.0,
            DecimalValue = 0m,
            BoolValue = false,
            StringValue = ""
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.AllPrimitivesClass);

        Assert.Contains("int-value:", yaml);
        Assert.Contains("bool-value:", yaml);
    }

    [Fact]
    public void RoundTripSpecialFloatValues()
    {
        var original = new SpecialNumbersClass
        {
            Infinity = double.PositiveInfinity,
            NegativeInfinity = double.NegativeInfinity,
            NaN = double.NaN
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.SpecialNumbersClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SpecialNumbersClass);

        Assert.NotNull(result);
        Assert.True(double.IsPositiveInfinity(result.Infinity));
        Assert.True(double.IsNegativeInfinity(result.NegativeInfinity));
        Assert.True(double.IsNaN(result.NaN));
    }

    #region String Edge Cases

    [Fact]
    public void SerializeWithEmptyStrings()
    {
        var obj = new SimpleClass { Name = "", Value = 0, IsActive = false };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SimpleClass);

        Assert.Contains("name:", yaml);
        Assert.Contains("value:", yaml);
        Assert.Contains("is-active:", yaml);
    }

    [Fact]
    public void DeserializeWithEmptyString()
    {
        var yaml = """
            name: ""
            value: 0
            is-active: false
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(obj);
        Assert.Equal("", obj.Name);
        Assert.Equal(0, obj.Value);
        Assert.False(obj.IsActive);
    }

    [Fact]
    public void RoundTripEmptyStrings()
    {
        var original = new SimpleClass { Name = "", Value = 0, IsActive = false };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.SimpleClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Value, result.Value);
        Assert.Equal(original.IsActive, result.IsActive);
    }

    [Fact]
    public void SerializeWithSpecialCharactersInString()
    {
        var obj = new SimpleClass { Name = "Hello: World!", Value = 1, IsActive = true };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SimpleClass);

        Assert.Contains("Hello: World!", yaml);
    }

    [Fact]
    public void RoundTripWithSpecialCharacters()
    {
        var original = new SimpleClass { Name = "Test: with colon and [brackets]", Value = 123, IsActive = true };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.SimpleClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
    }

    [Fact]
    public void SerializeWithMultilineString()
    {
        var obj = new SimpleClass { Name = "Line1\nLine2\nLine3", Value = 42, IsActive = true };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(yaml);
    }

    [Fact]
    public void DeserializeMultilinePlainScalar_FoldsToSingleLine()
    {
        // YAML multiline plain scalar - line break + indentation should fold to single space
        var yaml = """
            name: This is a long description that spans
              multiple lines in the YAML file
            value: 42
            is-active: true
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(obj);
        // Line breaks should be folded into single spaces per YAML spec
        Assert.Equal("This is a long description that spans multiple lines in the YAML file", obj.Name);
    }

    [Fact]
    public void DeserializeSingleLinePlainScalar_RemainsUnchanged()
    {
        var yaml = """
            name: Simple single line value
            value: 42
            is-active: true
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(obj);
        Assert.Equal("Simple single line value", obj.Name);
    }

    [Fact]
    public void DeserializeMultilinePlainScalar_WithMultipleContinuationLines()
    {
        // Three continuation lines should all fold to spaces
        var yaml = """
            name: First line
              second line
              third line
              fourth line
            value: 42
            is-active: true
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(obj);
        Assert.Equal("First line second line third line fourth line", obj.Name);
    }

    [Fact]
    public void DeserializeMultilinePlainScalar_WithBlankLines_PreservesNewlines()
    {
        // Blank lines (multiple consecutive line breaks) should preserve newlines
        var yaml = """
            name: First paragraph

              Second paragraph after blank line
            value: 42
            is-active: true
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(obj);
        // One blank line = two line breaks = one preserved newline + one folded space
        Assert.Contains("\n", obj.Name);
    }

    [Fact]
    public void DeserializeSingleQuotedMultilineScalar_FoldsToSingleLine()
    {
        var yaml = """
            name: 'This is a single quoted
              multiline string'
            value: 42
            is-active: true
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(obj);
        Assert.Equal("This is a single quoted multiline string", obj.Name);
    }

    [Fact]
    public void DeserializeSingleQuotedScalar_WithEscapedQuotes()
    {
        var yaml = """
            name: 'It''s a test with ''quotes'''
            value: 42
            is-active: true
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(obj);
        Assert.Equal("It's a test with 'quotes'", obj.Name);
    }

    [Fact]
    public void DeserializeSingleQuotedMultiline_WithEscapedQuotes()
    {
        var yaml = """
            name: 'First line with ''quote''
              and second line'
            value: 42
            is-active: true
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(obj);
        Assert.Equal("First line with 'quote' and second line", obj.Name);
    }

    [Fact]
    public void DeserializeDoubleQuotedMultilineScalar_FoldsToSingleLine()
    {
        var yaml = """
            name: "This is a double quoted
              multiline string"
            value: 42
            is-active: true
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(obj);
        Assert.Equal("This is a double quoted multiline string", obj.Name);
    }

    [Fact]
    public void DeserializePlainScalar_VeryLongSingleLine_NoUnwantedWrapping()
    {
        // A very long single line should remain as-is, no line breaks inserted
        var longValue = new string('x', 200);
        var yaml = $"""
            name: {longValue}
            value: 42
            is-active: true
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(obj);
        Assert.Equal(longValue, obj.Name);
        Assert.DoesNotContain("\n", obj.Name);
        Assert.DoesNotContain("\r", obj.Name);
    }

    [Fact]
    public void DeserializePlainScalar_WithTabsInContinuation_ThrowsException()
    {
        // YAML spec forbids tabs for indentation - parser should throw
        var yaml = "name: First line\n\tsecond with tab\nvalue: 42\nis-active: true";

        var exception = Assert.Throws<YamlException>(() =>
            YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass));

        Assert.Contains("Tabs are not allowed", exception.Message);
    }

    [Fact]
    public void DeserializePlainScalar_SingleWordPerLine()
    {
        var yaml = """
            name: word1
              word2
              word3
            value: 42
            is-active: true
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(obj);
        Assert.Equal("word1 word2 word3", obj.Name);
    }

    [Fact]
    public void DeserializePlainScalar_WithLeadingSpacesOnContinuation()
    {
        // Extra leading spaces beyond minimum indent should be stripped for folding
        var yaml = """
            name: Start
                  deeply indented continuation
            value: 42
            is-active: true
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(obj);
        // All leading whitespace on continuation is consumed, single space inserted
        Assert.Equal("Start deeply indented continuation", obj.Name);
    }

    [Fact]
    public void DeserializePlainScalar_EmptyValue_ReturnsNull()
    {
        var yaml = """
            name:
            value: 42
            is-active: true
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(obj);
        Assert.Null(obj.Name);
    }

    [Fact]
    public void DeserializePlainScalar_OnlyWhitespace_ReturnsNull()
    {
        var yaml = """
            name:   
            value: 42
            is-active: true
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(obj);
        // Trailing whitespace after colon with no content = null
        Assert.Null(obj.Name);
    }

    #endregion

    #region Extreme Int Values

    [Fact]
    public void SerializeWithMaxIntValue()
    {
        var obj = new SimpleClass { Name = "MaxInt", Value = int.MaxValue, IsActive = true };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SimpleClass);

        Assert.Contains(int.MaxValue.ToString(), yaml);
    }

    [Fact]
    public void SerializeWithMinIntValue()
    {
        var obj = new SimpleClass { Name = "MinInt", Value = int.MinValue, IsActive = false };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SimpleClass);

        Assert.Contains(int.MinValue.ToString(), yaml);
    }

    [Fact]
    public void RoundTripWithExtremeIntValues()
    {
        var original = new SimpleClass { Name = "Extreme", Value = int.MinValue, IsActive = true };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.SimpleClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(result);
        Assert.Equal(original.Value, result.Value);
    }

    #endregion
}
