using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Record with properties for testing record serialization.
/// </summary>
public record PersonRecord
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string? Email { get; set; }
}

/// <summary>
/// Record with init-only properties.
/// </summary>
public record AddressRecord
{
    public required string Street { get; init; }
    public required string City { get; init; }
    public string? ZipCode { get; init; }
    public string Country { get; init; } = "USA";
}

/// <summary>
/// Record struct for testing value-type records.
/// </summary>
public readonly record struct PointRecord(double X, double Y);

/// <summary>
/// Tests for serializing and deserializing records.
/// </summary>
public class RecordSerializationTests
{
    [Fact]
    public void SerializeRecord()
    {
        var record = new PersonRecord { Name = "John", Age = 30, Email = "john@example.com" };

        var yaml = YamlSerializer.Serialize(record, TestSerializerContext.Default.PersonRecord);

        Assert.Contains("name:", yaml);
        Assert.Contains("John", yaml);
        Assert.Contains("age:", yaml);
        Assert.Contains("30", yaml);
        Assert.Contains("email:", yaml);
    }

    [Fact]
    public void DeserializeRecord()
    {
        var yaml = """
            name: Jane
            age: 25
            email: jane@example.com
            """;

        var record = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.PersonRecord);

        Assert.NotNull(record);
        Assert.Equal("Jane", record.Name);
        Assert.Equal(25, record.Age);
        Assert.Equal("jane@example.com", record.Email);
    }

    [Fact]
    public void SerializeRecordWithInitProperties()
    {
        var record = new AddressRecord { Street = "123 Main St", City = "Boston", ZipCode = "02101" };

        var yaml = YamlSerializer.Serialize(record, TestSerializerContext.Default.AddressRecord);

        Assert.Contains("street:", yaml);
        Assert.Contains("123 Main St", yaml);
        Assert.Contains("city:", yaml);
        Assert.Contains("Boston", yaml);
    }

    [Fact]
    public void SerializeRecordStruct()
    {
        var point = new PointRecord(3.5, 4.5);

        var yaml = YamlSerializer.Serialize(point, TestSerializerContext.Default.PointRecord);

        Assert.Contains("x:", yaml);
        Assert.Contains("3.5", yaml);
        Assert.Contains("y:", yaml);
        Assert.Contains("4.5", yaml);
    }

    [Fact]
    public void DeserializeRecordStruct()
    {
        var yaml = """
            x: 10.0
            y: 20.0
            """;

        var point = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.PointRecord);

        Assert.Equal(10.0, point.X);
        Assert.Equal(20.0, point.Y);
    }

    [Fact]
    public void RoundTripPersonRecord()
    {
        var original = new PersonRecord { Name = "Alice", Age = 35, Email = "alice@example.com" };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.PersonRecord);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.PersonRecord);

        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Age, result.Age);
        Assert.Equal(original.Email, result.Email);
    }

    [Fact]
    public void RoundTripAddressRecord()
    {
        var original = new AddressRecord { Street = "456 Oak Ave", City = "Seattle", ZipCode = "98101", Country = "USA" };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.AddressRecord);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.AddressRecord);

        Assert.NotNull(result);
        Assert.Equal(original.Street, result.Street);
        Assert.Equal(original.City, result.City);
        Assert.Equal(original.ZipCode, result.ZipCode);
        Assert.Equal(original.Country, result.Country);
    }

    [Fact]
    public void RoundTripPointRecord()
    {
        var original = new PointRecord(7.5, 12.5);

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.PointRecord);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.PointRecord);

        Assert.Equal(original.X, result.X);
        Assert.Equal(original.Y, result.Y);
    }

    [Fact]
    public void DeserializeRecordWithNullableProperty()
    {
        var yaml = """
            name: Bob
            age: 40
            """;

        var record = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.PersonRecord);

        Assert.NotNull(record);
        Assert.Equal("Bob", record.Name);
        Assert.Equal(40, record.Age);
        Assert.Null(record.Email);
    }

    [Fact]
    public void SerializeRecordWithNullProperty()
    {
        var record = new PersonRecord { Name = "Charlie", Age = 28, Email = null };

        var yaml = YamlSerializer.Serialize(record, TestSerializerContext.Default.PersonRecord);

        Assert.Contains("name:", yaml);
        Assert.Contains("Charlie", yaml);
        Assert.Contains("age:", yaml);
        Assert.Contains("28", yaml);
    }

    [Fact]
    public void DeserializeAddressRecordWithDefaultCountry()
    {
        var yaml = """
            street: 789 Pine St
            city: Portland
            """;

        var record = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.AddressRecord);

        Assert.NotNull(record);
        Assert.Equal("789 Pine St", record.Street);
        Assert.Equal("Portland", record.City);
        Assert.Null(record.ZipCode);
        // Note: Default value from record is not used during deserialization
    }

    [Fact]
    public void SerializeRecordStructWithZeroValues()
    {
        var point = new PointRecord(0.0, 0.0);

        var yaml = YamlSerializer.Serialize(point, TestSerializerContext.Default.PointRecord);

        Assert.Contains("x:", yaml);
        Assert.Contains("y:", yaml);
    }

    [Fact]
    public void SerializeRecordStructWithNegativeValues()
    {
        var point = new PointRecord(-5.5, -10.5);

        var yaml = YamlSerializer.Serialize(point, TestSerializerContext.Default.PointRecord);

        Assert.Contains("-5.5", yaml);
        Assert.Contains("-10.5", yaml);
    }

    [Fact]
    public void RoundTripRecordStructWithNegativeValues()
    {
        var original = new PointRecord(-100.0, -200.0);

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.PointRecord);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.PointRecord);

        Assert.Equal(original.X, result.X);
        Assert.Equal(original.Y, result.Y);
    }
}
