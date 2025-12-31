using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Tests for preserving class default values during deserialization.
/// When properties are missing from YAML, class property initializers and constructor defaults should be preserved.
/// </summary>
public class DefaultValueSerializationTests
{
    [Fact]
    public void Deserialize_MissingProperty_PreservesStringDefault()
    {
        var yaml = """
            name: Test
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ClassWithPropertyDefaults);

        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal("default-value", result.DefaultedProperty);
    }

    [Fact]
    public void Deserialize_MissingProperty_PreservesIntDefault()
    {
        var yaml = """
            name: Test
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ClassWithPropertyDefaults);

        Assert.NotNull(result);
        Assert.Equal(42, result.DefaultedNumber);
    }

    [Fact]
    public void Deserialize_ExplicitValue_OverridesStringDefault()
    {
        var yaml = """
            name: Test
            defaulted-property: custom-value
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ClassWithPropertyDefaults);

        Assert.NotNull(result);
        Assert.Equal("custom-value", result.DefaultedProperty);
    }

    [Fact]
    public void Deserialize_ExplicitValue_OverridesIntDefault()
    {
        var yaml = """
            name: Test
            defaulted-number: 100
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ClassWithPropertyDefaults);

        Assert.NotNull(result);
        Assert.Equal(100, result.DefaultedNumber);
    }

    [Fact]
    public void Deserialize_NestedEmptyObject_PreservesNestedDefaults()
    {
        var yaml = """
            name: Container
            config: {}
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ContainerWithNestedDefaults);

        Assert.NotNull(result);
        Assert.Equal("Container", result.Name);
        Assert.NotNull(result.Config);
        Assert.Equal("default-region", result.Config.Region);
        Assert.Equal(30, result.Config.Timeout);
    }

    [Fact]
    public void Deserialize_NestedPartialObject_PreservesUnspecifiedDefaults()
    {
        var yaml = """
            name: Container
            config:
              region: custom-region
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ContainerWithNestedDefaults);

        Assert.NotNull(result);
        Assert.NotNull(result.Config);
        Assert.Equal("custom-region", result.Config.Region);
        Assert.Equal(30, result.Config.Timeout);
    }

    [Fact]
    public void Deserialize_NestedFullObject_OverridesAllDefaults()
    {
        var yaml = """
            name: Container
            config:
              region: custom-region
              timeout: 60
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ContainerWithNestedDefaults);

        Assert.NotNull(result);
        Assert.NotNull(result.Config);
        Assert.Equal("custom-region", result.Config.Region);
        Assert.Equal(60, result.Config.Timeout);
    }

    [Fact]
    public void Deserialize_MissingNestedObject_ReturnsNull()
    {
        var yaml = """
            name: Container
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ContainerWithNestedDefaults);

        Assert.NotNull(result);
        Assert.Equal("Container", result.Name);
        Assert.Null(result.Config);
    }

    [Fact]
    public void Deserialize_BooleanDefault_PreservesTrue()
    {
        var yaml = """
            name: Test
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ClassWithBooleanDefaults);

        Assert.NotNull(result);
        Assert.True(result.IsEnabled);
        Assert.False(result.IsDisabled);
    }

    [Fact]
    public void Deserialize_BooleanExplicit_OverridesDefaults()
    {
        var yaml = """
            name: Test
            is-enabled: false
            is-disabled: true
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ClassWithBooleanDefaults);

        Assert.NotNull(result);
        Assert.False(result.IsEnabled);
        Assert.True(result.IsDisabled);
    }

    [Fact]
    public void RoundTrip_PreservesDefaultValues()
    {
        var original = new ClassWithPropertyDefaults { Name = "Test" };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.ClassWithPropertyDefaults);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ClassWithPropertyDefaults);

        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.DefaultedProperty, result.DefaultedProperty);
        Assert.Equal(original.DefaultedNumber, result.DefaultedNumber);
    }
}

/// <summary>
/// Test class with property initializers (defaults).
/// </summary>
public class ClassWithPropertyDefaults
{
    public string? Name { get; set; }
    public string DefaultedProperty { get; set; } = "default-value";
    public int DefaultedNumber { get; set; } = 42;
}

/// <summary>
/// Test class with boolean property defaults.
/// </summary>
public class ClassWithBooleanDefaults
{
    public string? Name { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsDisabled { get; set; } = false;
}

/// <summary>
/// Test class simulating configuration with defaults.
/// </summary>
public class ConfigWithDefaults
{
    public string Region { get; set; } = "default-region";
    public int Timeout { get; set; } = 30;
    public string? OptionalSetting { get; set; }
}

/// <summary>
/// Container class with nested object that has defaults.
/// </summary>
public class ContainerWithNestedDefaults
{
    public string? Name { get; set; }
    public ConfigWithDefaults? Config { get; set; }
}
