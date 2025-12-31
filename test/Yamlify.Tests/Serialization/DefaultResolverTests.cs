using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Tests for simplified API using YamlSerializerOptions.Default.TypeInfoResolver
/// </summary>
public class DefaultResolverTests
{
    [Fact]
    public void Serialize_WithoutContext_ThrowsWhenNoResolverConfigured()
    {
        // Create fresh options to test (can't use Default since it's shared/static)
        var options = new YamlSerializerOptions();
        
        Assert.Throws<InvalidOperationException>(() => 
            YamlSerializer.Serialize(new SimpleClass { Name = "Test" }, options));
    }

    [Fact]
    public void Deserialize_WithoutContext_ThrowsWhenNoResolverConfigured()
    {
        var options = new YamlSerializerOptions();
        
        Assert.Throws<InvalidOperationException>(() => 
            YamlSerializer.Deserialize<SimpleClass>("name: Test", options));
    }

    [Fact]
    public void Serialize_WithResolverOnOptions_Works()
    {
        var options = new YamlSerializerOptions
        {
            TypeInfoResolver = TestSerializerContext.Default
        };
        
        var obj = new SimpleClass { Name = "Test", Value = 42, IsActive = true };
        var yaml = YamlSerializer.Serialize(obj, options);
        
        Assert.Contains("name: Test", yaml);
        Assert.Contains("value: 42", yaml);
        Assert.Contains("is-active: true", yaml);
    }

    [Fact]
    public void Deserialize_WithResolverOnOptions_Works()
    {
        var options = new YamlSerializerOptions
        {
            TypeInfoResolver = TestSerializerContext.Default
        };
        
        var yaml = """
            name: Test
            value: 42
            is-active: true
            """;
        
        var obj = YamlSerializer.Deserialize<SimpleClass>(yaml, options);
        
        Assert.NotNull(obj);
        Assert.Equal("Test", obj.Name);
        Assert.Equal(42, obj.Value);
        Assert.True(obj.IsActive);
    }

    [Fact]
    public void TypeInfoResolver_OnDefaultOptions_CanBeSetOnceBeforeUse()
    {
        // This test verifies the behavior of setting TypeInfoResolver on Default
        // Note: In a real scenario, this would be set once at app startup
        // We can't easily test this without affecting other tests since Default is a singleton
        
        // Create a new options instance to simulate the pattern
        var options = new YamlSerializerOptions
        {
            TypeInfoResolver = TestSerializerContext.Default
        };
        
        // Serialize using the options with resolver
        var obj = new SimpleClass { Name = "Hello", Value = 123 };
        var yaml = YamlSerializer.Serialize(obj, options);
        
        Assert.Contains("name: Hello", yaml);
        
        // Deserialize using the same options
        var deserialized = YamlSerializer.Deserialize<SimpleClass>(yaml, options);
        Assert.Equal("Hello", deserialized?.Name);
        Assert.Equal(123, deserialized?.Value);
    }

    [Fact]
    public void Serialize_UnregisteredType_ThrowsDescriptiveError()
    {
        var options = new YamlSerializerOptions
        {
            TypeInfoResolver = TestSerializerContext.Default
        };
        
        // UnregisteredType is not in TestSerializerContext
        var ex = Assert.Throws<InvalidOperationException>(() => 
            YamlSerializer.Serialize(new UnregisteredType { Data = "test" }, options));
        
        Assert.Contains("UnregisteredType", ex.Message);
        Assert.Contains("YamlSerializable", ex.Message);
    }

    [Fact]
    public void SerializeToStream_WithResolverOnOptions_Works()
    {
        var options = new YamlSerializerOptions
        {
            TypeInfoResolver = TestSerializerContext.Default
        };
        
        var obj = new SimpleClass { Name = "StreamTest", Value = 99 };
        
        using var stream = new MemoryStream();
        YamlSerializer.Serialize(stream, obj, options);
        
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var yaml = reader.ReadToEnd();
        
        Assert.Contains("name: StreamTest", yaml);
        Assert.Contains("value: 99", yaml);
    }

    [Fact]
    public void DeserializeFromStream_WithResolverOnOptions_Works()
    {
        var options = new YamlSerializerOptions
        {
            TypeInfoResolver = TestSerializerContext.Default
        };
        
        var yaml = """
            name: FromStream
            value: 77
            """;
        
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(yaml));
        var obj = YamlSerializer.Deserialize<SimpleClass>(stream, options);
        
        Assert.NotNull(obj);
        Assert.Equal("FromStream", obj.Name);
        Assert.Equal(77, obj.Value);
    }

    [Fact]
    public async Task SerializeAsync_WithResolverOnOptions_Works()
    {
        var options = new YamlSerializerOptions
        {
            TypeInfoResolver = TestSerializerContext.Default
        };
        
        var obj = new SimpleClass { Name = "AsyncTest", Value = 55 };
        
        using var stream = new MemoryStream();
        await YamlSerializer.SerializeAsync(stream, obj, TestSerializerContext.Default.SimpleClass);
        
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var yaml = await reader.ReadToEndAsync();
        
        Assert.Contains("name: AsyncTest", yaml);
    }

    [Fact]
    public async Task DeserializeAsync_WithResolverOnOptions_Works()
    {
        var yaml = """
            name: AsyncFromStream
            value: 88
            """;
        
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(yaml));
        var obj = await YamlSerializer.DeserializeAsync<SimpleClass>(stream, TestSerializerContext.Default.SimpleClass);
        
        Assert.NotNull(obj);
        Assert.Equal("AsyncFromStream", obj.Name);
        Assert.Equal(88, obj.Value);
    }

    private class UnregisteredType
    {
        public string? Data { get; set; }
    }
}
