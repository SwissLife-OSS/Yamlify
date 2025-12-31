using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Tests for serializer options and configuration.
/// </summary>
public class SerializerOptionsTests
{
    [Fact]
    public void UsesKebabCaseByDefault()
    {
        var obj = new SimpleClass { Name = "Test", Value = 42, IsActive = true };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SimpleClass);

        Assert.Contains("is-active:", yaml);
    }

    [Fact]
    public void SerializeWithCamelCaseNaming()
    {
        var obj = new SimpleClass { Name = "Test", Value = 42, IsActive = true };
        var options = new YamlSerializerOptions { PropertyNamingPolicy = YamlNamingPolicy.CamelCase };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SimpleClass, options);

        Assert.Contains("name:", yaml);
        Assert.Contains("value:", yaml);
        Assert.Contains("isActive:", yaml);
    }

    [Fact]
    public void SerializeWithSnakeCaseNaming()
    {
        var obj = new SimpleClass { Name = "Test", Value = 42, IsActive = true };
        var options = new YamlSerializerOptions { PropertyNamingPolicy = YamlNamingPolicy.SnakeCase };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SimpleClass, options);

        Assert.Contains("name:", yaml);
        Assert.Contains("value:", yaml);
        Assert.Contains("is_active:", yaml);
    }

    [Fact]
    public void SerializeToBytes()
    {
        var obj = new SimpleClass { Name = "Test", Value = 42, IsActive = true };

        var bytes = YamlSerializer.SerializeToUtf8Bytes(obj, TestSerializerContext.Default.SimpleClass);

        Assert.NotEmpty(bytes);
        var yaml = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.Contains("name:", yaml);
    }

    [Fact]
    public void SerializeToStream()
    {
        var obj = new SimpleClass { Name = "Test", Value = 42, IsActive = true };
        using var stream = new MemoryStream();

        YamlSerializer.Serialize(stream, obj, TestSerializerContext.Default.SimpleClass);
        stream.Position = 0;

        using var reader = new StreamReader(stream);
        var yaml = reader.ReadToEnd();
        Assert.Contains("name:", yaml);
    }

    [Fact]
    public void DeserializeFromStream()
    {
        var yaml = """
            name: Test
            value: 42
            is-active: true
            """;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(yaml));

        var obj = YamlSerializer.Deserialize(stream, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(obj);
        Assert.Equal("Test", obj.Name);
    }
}
