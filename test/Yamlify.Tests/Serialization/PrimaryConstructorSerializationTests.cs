using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Class with primary constructor.
/// </summary>
public class PrimaryConstructorClass(string name, int value)
{
    public string Name { get; private set; } = name;
    public int Value { get; private set; } = value;
}

/// <summary>
/// Class with primary constructor and additional property.
/// </summary>
public class PrimaryConstructorWithExtraProperty(string name, int value)
{
    public string Name { get; private set; } = name;
    public int Value { get; private set; } = value;
    public string? Description { get; set; }
}

/// <summary>
/// Class with primary constructor with default values.
/// </summary>
public class PrimaryConstructorWithDefaults(string name, int value = 42, bool isActive = true)
{
    public string Name { get; } = name;
    public int Value { get; } = value;
    public bool IsActive { get; } = isActive;
}

/// <summary>
/// Positional record (primary constructor syntax).
/// </summary>
public record PositionalRecord(string FirstName, string LastName, int Age);

/// <summary>
/// Record with optional parameters.
/// </summary>
public record RecordWithDefaults(string Name, string Email = "default@example.com");

/// <summary>
/// Nested class with primary constructor.
/// </summary>
public class OuterPrimaryClass(string title, InnerPrimaryClass inner)
{
    public string Title { get; } = title;
    public InnerPrimaryClass Inner { get; } = inner;
}

public class InnerPrimaryClass(string data)
{
    public string Data { get; } = data;
}

/// <summary>
/// Tests for primary constructor serialization and deserialization.
/// </summary>
public class PrimaryConstructorSerializationTests
{
    [Fact]
    public void SerializePrimaryConstructorClass()
    {
        var obj = new PrimaryConstructorClass("Test", 42);

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.PrimaryConstructorClass);

        Assert.Contains("name:", yaml);
        Assert.Contains("Test", yaml);
        Assert.Contains("value:", yaml);
        Assert.Contains("42", yaml);
    }

    [Fact]
    public void DeserializePrimaryConstructorClass()
    {
        var yaml = """
            name: Test
            value: 42
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.PrimaryConstructorClass);

        Assert.NotNull(obj);
        Assert.Equal("Test", obj.Name);
        Assert.Equal(42, obj.Value);
    }

    [Fact]
    public void DeserializePrimaryConstructorWithExtraProperty()
    {
        var yaml = """
            name: Test
            value: 100
            description: Extra info
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.PrimaryConstructorWithExtraProperty);

        Assert.NotNull(obj);
        Assert.Equal("Test", obj.Name);
        Assert.Equal(100, obj.Value);
        Assert.Equal("Extra info", obj.Description);
    }

    [Fact]
    public void DeserializePrimaryConstructorWithDefaults()
    {
        var yaml = """
            name: OnlyName
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.PrimaryConstructorWithDefaults);

        Assert.NotNull(obj);
        Assert.Equal("OnlyName", obj.Name);
        Assert.Equal(42, obj.Value);
        Assert.True(obj.IsActive);
    }

    [Fact]
    public void DeserializePrimaryConstructorOverrideDefaults()
    {
        var yaml = """
            name: FullConfig
            value: 99
            is-active: false
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.PrimaryConstructorWithDefaults);

        Assert.NotNull(obj);
        Assert.Equal("FullConfig", obj.Name);
        Assert.Equal(99, obj.Value);
        Assert.False(obj.IsActive);
    }

    [Fact]
    public void SerializePositionalRecord()
    {
        var record = new PositionalRecord("John", "Doe", 30);

        var yaml = YamlSerializer.Serialize(record, TestSerializerContext.Default.PositionalRecord);

        Assert.Contains("first-name:", yaml);
        Assert.Contains("John", yaml);
        Assert.Contains("last-name:", yaml);
        Assert.Contains("Doe", yaml);
        Assert.Contains("age:", yaml);
        Assert.Contains("30", yaml);
    }

    [Fact]
    public void DeserializePositionalRecord()
    {
        var yaml = """
            first-name: Jane
            last-name: Smith
            age: 25
            """;

        var record = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.PositionalRecord);

        Assert.NotNull(record);
        Assert.Equal("Jane", record.FirstName);
        Assert.Equal("Smith", record.LastName);
        Assert.Equal(25, record.Age);
    }

    [Fact]
    public void DeserializeRecordWithDefaults()
    {
        var yaml = """
            name: TestUser
            """;

        var record = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.RecordWithDefaults);

        Assert.NotNull(record);
        Assert.Equal("TestUser", record.Name);
        // Default value should be used when not specified
        Assert.Equal("default@example.com", record.Email);
    }

    [Fact]
    public void DeserializeRecordOverrideDefaults()
    {
        var yaml = """
            name: TestUser
            email: custom@example.com
            """;

        var record = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.RecordWithDefaults);

        Assert.NotNull(record);
        Assert.Equal("TestUser", record.Name);
        Assert.Equal("custom@example.com", record.Email);
    }

    [Fact]
    public void RoundTripPrimaryConstructorClass()
    {
        var original = new PrimaryConstructorClass("RoundTrip", 123);

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.PrimaryConstructorClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.PrimaryConstructorClass);

        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Value, result.Value);
    }

    [Fact]
    public void RoundTripPositionalRecord()
    {
        var original = new PositionalRecord("Alice", "Wonder", 28);

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.PositionalRecord);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.PositionalRecord);

        Assert.NotNull(result);
        Assert.Equal(original.FirstName, result.FirstName);
        Assert.Equal(original.LastName, result.LastName);
        Assert.Equal(original.Age, result.Age);
    }

    [Fact]
    public void DeserializeNestedPrimaryConstructorClasses()
    {
        var yaml = """
            title: Outer
            inner:
              data: Inner Data
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.OuterPrimaryClass);

        Assert.NotNull(obj);
        Assert.Equal("Outer", obj.Title);
        Assert.NotNull(obj.Inner);
        Assert.Equal("Inner Data", obj.Inner.Data);
    }

    [Fact]
    public void SerializeNestedPrimaryConstructorClasses()
    {
        var obj = new OuterPrimaryClass("Outer Title", new InnerPrimaryClass("Inner Content"));

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.OuterPrimaryClass);

        Assert.Contains("title:", yaml);
        Assert.Contains("Outer Title", yaml);
        Assert.Contains("inner:", yaml);
        Assert.Contains("data:", yaml);
        Assert.Contains("Inner Content", yaml);
    }

    [Fact]
    public void RoundTripNestedPrimaryConstructorClasses()
    {
        var original = new OuterPrimaryClass("RoundTrip Outer", new InnerPrimaryClass("RoundTrip Inner"));

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.OuterPrimaryClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.OuterPrimaryClass);

        Assert.NotNull(result);
        Assert.Equal(original.Title, result.Title);
        Assert.NotNull(result.Inner);
        Assert.Equal(original.Inner.Data, result.Inner.Data);
    }

    [Fact]
    public void RoundTripPrimaryConstructorWithExtraProperty()
    {
        var original = new PrimaryConstructorWithExtraProperty("Test", 50)
        {
            Description = "Extra description"
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.PrimaryConstructorWithExtraProperty);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.PrimaryConstructorWithExtraProperty);

        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Value, result.Value);
        Assert.Equal(original.Description, result.Description);
    }

    [Fact]
    public void RoundTripRecordWithDefaults()
    {
        var original = new RecordWithDefaults("CustomUser", "user@test.com");

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.RecordWithDefaults);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.RecordWithDefaults);

        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Email, result.Email);
    }

    [Fact]
    public void SerializePrimaryConstructorWithDefaults()
    {
        var obj = new PrimaryConstructorWithDefaults("OnlyName");

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.PrimaryConstructorWithDefaults);

        Assert.Contains("name:", yaml);
        Assert.Contains("OnlyName", yaml);
        Assert.Contains("value:", yaml);
        Assert.Contains("42", yaml);
        Assert.Contains("is-active:", yaml);
        Assert.Contains("true", yaml);
    }
}
