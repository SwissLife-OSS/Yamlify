using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Simple struct for value type testing.
/// </summary>
public struct SimpleStruct
{
    public int X { get; set; }
    public int Y { get; set; }
    public string? Label { get; set; }
}

/// <summary>
/// Readonly struct for immutable value type testing.
/// </summary>
public readonly struct ImmutablePoint
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Z { get; init; }
}

/// <summary>
/// Tests for serializing and deserializing structs.
/// </summary>
public class StructSerializationTests
{
    [Fact]
    public void SerializeSimpleStruct()
    {
        var s = new SimpleStruct { X = 10, Y = 20, Label = "Origin" };

        var yaml = YamlSerializer.Serialize(s, TestSerializerContext.Default.SimpleStruct);

        Assert.Contains("x:", yaml);
        Assert.Contains("10", yaml);
        Assert.Contains("y:", yaml);
        Assert.Contains("20", yaml);
        Assert.Contains("label:", yaml);
        Assert.Contains("Origin", yaml);
    }

    [Fact]
    public void DeserializeSimpleStruct()
    {
        var yaml = """
            x: 5
            y: 15
            label: Point A
            """;

        var s = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleStruct);

        Assert.Equal(5, s.X);
        Assert.Equal(15, s.Y);
        Assert.Equal("Point A", s.Label);
    }

    [Fact]
    public void SerializeReadonlyStruct()
    {
        var point = new ImmutablePoint { X = 1.1, Y = 2.2, Z = 3.3 };

        var yaml = YamlSerializer.Serialize(point, TestSerializerContext.Default.ImmutablePoint);

        Assert.Contains("x:", yaml);
        Assert.Contains("1.1", yaml);
        Assert.Contains("y:", yaml);
        Assert.Contains("2.2", yaml);
        Assert.Contains("z:", yaml);
        Assert.Contains("3.3", yaml);
    }

    [Fact]
    public void DeserializeReadonlyStruct()
    {
        var yaml = """
            x: 100.5
            y: 200.5
            z: 300.5
            """;

        var point = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ImmutablePoint);

        Assert.Equal(100.5, point.X);
        Assert.Equal(200.5, point.Y);
        Assert.Equal(300.5, point.Z);
    }

    [Fact]
    public void RoundTripSimpleStruct()
    {
        var original = new SimpleStruct { X = 42, Y = 84, Label = "TestLabel" };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.SimpleStruct);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleStruct);

        Assert.Equal(original.X, result.X);
        Assert.Equal(original.Y, result.Y);
        Assert.Equal(original.Label, result.Label);
    }

    [Fact]
    public void RoundTripImmutablePoint()
    {
        var original = new ImmutablePoint { X = 99.9, Y = 88.8, Z = 77.7 };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.ImmutablePoint);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ImmutablePoint);

        Assert.Equal(original.X, result.X);
        Assert.Equal(original.Y, result.Y);
        Assert.Equal(original.Z, result.Z);
    }

    [Fact]
    public void SerializeStructWithNullLabel()
    {
        var s = new SimpleStruct { X = 1, Y = 2, Label = null };

        var yaml = YamlSerializer.Serialize(s, TestSerializerContext.Default.SimpleStruct);

        Assert.Contains("x:", yaml);
        Assert.Contains("y:", yaml);
        Assert.Contains("label:", yaml);
    }

    [Fact]
    public void DeserializeStructWithDefaultValues()
    {
        var yaml = """
            x: 0
            y: 0
            """;

        var s = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleStruct);

        Assert.Equal(0, s.X);
        Assert.Equal(0, s.Y);
        Assert.Null(s.Label);
    }

    [Fact]
    public void SerializeStructWithNegativeValues()
    {
        var s = new SimpleStruct { X = -100, Y = -200, Label = "Negative" };

        var yaml = YamlSerializer.Serialize(s, TestSerializerContext.Default.SimpleStruct);

        Assert.Contains("-100", yaml);
        Assert.Contains("-200", yaml);
    }

    [Fact]
    public void DeserializeImmutablePointWithZeroValues()
    {
        var yaml = """
            x: 0
            y: 0
            z: 0
            """;

        var point = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ImmutablePoint);

        Assert.Equal(0, point.X);
        Assert.Equal(0, point.Y);
        Assert.Equal(0, point.Z);
    }
}
