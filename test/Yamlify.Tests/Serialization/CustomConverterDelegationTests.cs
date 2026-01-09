using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Tests for custom converters that delegate to generated code via GeneratedRead/GeneratedWrite.
/// </summary>
public class CustomConverterDelegationTests
{
    #region Basic Delegation Tests

    [Fact]
    public void Deserialize_WithCustomConverter_StandardFormat_DelegatesToGenerated()
    {
        // Arrange - Standard format uses the generated Read logic
        const string yaml = """
            enabled: true
            settings:
              max-retries: 5
              timeout-seconds: 30
            """;

        // Act
        var result = YamlSerializer.Deserialize<FeatureToggle>(yaml, CustomConverterTestContext.Default);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Enabled);
        Assert.NotNull(result.Settings);
        Assert.Equal(5, result.Settings.MaxRetries);
        Assert.Equal(30, result.Settings.TimeoutSeconds);
    }

    [Fact]
    public void Deserialize_WithCustomConverter_LegacyBooleanFormat_HandledByCustomLogic()
    {
        // Arrange - Legacy format: just "true" instead of object
        const string yaml = "true";

        // Act
        var result = YamlSerializer.Deserialize<FeatureToggle>(yaml, CustomConverterTestContext.Default);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Enabled);
        Assert.Null(result.Settings);
    }

    [Fact]
    public void Deserialize_WithCustomConverter_LegacyBooleanFalse_HandledByCustomLogic()
    {
        // Arrange - Legacy format: just "false" instead of object
        const string yaml = "false";

        // Act
        var result = YamlSerializer.Deserialize<FeatureToggle>(yaml, CustomConverterTestContext.Default);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Enabled);
        Assert.Null(result.Settings);
    }

    [Fact]
    public void Serialize_WithCustomConverter_DelegatesToGenerated()
    {
        // Arrange
        var config = new FeatureToggle
        {
            Enabled = true,
            Settings = new FeatureSettings
            {
                MaxRetries = 5,
                TimeoutSeconds = 30
            }
        };

        // Act
        var yaml = YamlSerializer.Serialize(config, CustomConverterTestContext.Default);

        // Assert
        Assert.Contains("enabled: true", yaml);
        Assert.Contains("max-retries: 5", yaml);
        Assert.Contains("timeout-seconds: 30", yaml);
    }

    [Fact]
    public void RoundTrip_WithCustomConverter_PreservesData()
    {
        // Arrange
        var original = new FeatureToggle
        {
            Enabled = true,
            Settings = new FeatureSettings
            {
                MaxRetries = 10,
                TimeoutSeconds = 60
            }
        };

        // Act
        var yaml = YamlSerializer.Serialize(original, CustomConverterTestContext.Default);
        var deserialized = YamlSerializer.Deserialize<FeatureToggle>(yaml, CustomConverterTestContext.Default);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Enabled, deserialized.Enabled);
        Assert.NotNull(deserialized.Settings);
        Assert.Equal(original.Settings.MaxRetries, deserialized.Settings.MaxRetries);
        Assert.Equal(original.Settings.TimeoutSeconds, deserialized.Settings.TimeoutSeconds);
    }

    #endregion

    #region IgnoreEmptyObjects Tests (Custom Converter + Nested Nullable Objects)

    /// <summary>
    /// Scenario: Custom converter type with nested nullable object should be serialized when IgnoreEmptyObjects is true.
    /// Even when Settings is null, the FeatureToggle should still be written because it has a custom converter.
    /// </summary>
    [Fact]
    public void Serialize_CustomConverterWithNullNested_IgnoreEmptyObjects_WritesProperty()
    {
        // Arrange - FeatureToggle with enabled=true but no Settings (null)
        var config = new FeatureToggle { Enabled = true };

        // Act - Use context with IgnoreEmptyObjects = true
        var yaml = YamlSerializer.Serialize(config, CustomConverterIgnoreEmptyContext.Default);

        // Assert - Should still write the property even though nested Settings is null
        Assert.Contains("enabled: true", yaml);
    }

    [Fact]
    public void Serialize_ParentWithCustomConverterChild_IgnoreEmptyObjects_WritesCustomConverterProperty()
    {
        // Arrange - Parent container with FeatureToggle (custom converter child with null nested object)
        var container = new FeatureContainer
        {
            Name = "my-app",
            Feature = new FeatureToggle { Enabled = true }
        };

        // Act - Use context with IgnoreEmptyObjects = true
        var yaml = YamlSerializer.Serialize(container, CustomConverterIgnoreEmptyContext.Default);

        // Assert - feature property should be written because it has a custom converter
        Assert.Contains("name: my-app", yaml);
        Assert.Contains("feature:", yaml);
        Assert.Contains("enabled: true", yaml);
    }

    [Fact]
    public void Serialize_ParentWithNullCustomConverterChild_IgnoreEmptyObjects_OmitsProperty()
    {
        // Arrange - Parent container with null FeatureToggle
        var container = new FeatureContainer
        {
            Name = "my-app",
            Feature = null
        };

        // Act - Use context with IgnoreEmptyObjects = true
        var yaml = YamlSerializer.Serialize(container, CustomConverterIgnoreEmptyContext.Default);

        // Assert - feature property should be omitted because the value is null
        Assert.Contains("name: my-app", yaml);
        Assert.DoesNotContain("feature:", yaml);
    }

    [Fact]
    public void Serialize_CustomConverterWithFullNestedObject_IgnoreEmptyObjects_WritesAllProperties()
    {
        // Arrange - FeatureToggle with full Settings configuration
        var config = new FeatureToggle
        {
            Enabled = true,
            Settings = new FeatureSettings
            {
                MaxRetries = 5,
                TimeoutSeconds = 30
            }
        };

        // Act
        var yaml = YamlSerializer.Serialize(config, CustomConverterIgnoreEmptyContext.Default);

        // Assert
        Assert.Contains("enabled: true", yaml);
        Assert.Contains("settings:", yaml);
        Assert.Contains("max-retries: 5", yaml);
        Assert.Contains("timeout-seconds: 30", yaml);
    }

    [Fact]
    public void RoundTrip_CustomConverterWithNullNested_IgnoreEmptyObjects_PreservesEnabled()
    {
        // Arrange
        var original = new FeatureToggle { Enabled = true };

        // Act
        var yaml = YamlSerializer.Serialize(original, CustomConverterIgnoreEmptyContext.Default);
        var deserialized = YamlSerializer.Deserialize<FeatureToggle>(yaml, CustomConverterIgnoreEmptyContext.Default);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Enabled);
    }

    [Fact]
    public void RoundTrip_ContainerWithCustomConverterChild_IgnoreEmptyObjects_PreservesData()
    {
        // Arrange
        var original = new FeatureContainer
        {
            Name = "my-app",
            Feature = new FeatureToggle { Enabled = true }
        };

        // Act
        var yaml = YamlSerializer.Serialize(original, CustomConverterIgnoreEmptyContext.Default);
        var deserialized = YamlSerializer.Deserialize<FeatureContainer>(yaml, CustomConverterIgnoreEmptyContext.Default);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("my-app", deserialized.Name);
        Assert.NotNull(deserialized.Feature);
        Assert.True(deserialized.Feature.Enabled);
    }

    #endregion
}

#region Models

/// <summary>
/// Custom converter that handles legacy boolean format while delegating standard format to generated code.
/// </summary>
public class FeatureToggleConverter : YamlConverter<FeatureToggle>
{
    public override FeatureToggle? Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        // Handle legacy format: just a boolean instead of an object
        if (reader.TokenType == YamlTokenType.Scalar)
        {
            var value = reader.GetString();
            reader.Read(); // Consume the scalar

            // Parse as boolean for legacy support
            if (bool.TryParse(value, out var enabled))
            {
                return new FeatureToggle { Enabled = enabled };
            }

            // If not a boolean, treat as false
            return new FeatureToggle { Enabled = false };
        }

        // Standard object format - delegate to generated code
        return GeneratedRead(ref reader, options);
    }

    public override void Write(Utf8YamlWriter writer, FeatureToggle value, YamlSerializerOptions options)
    {
        // Always use the standard format for writing
        GeneratedWrite(writer, value, options);
    }
}

/// <summary>
/// Model with a custom converter for migration support.
/// Has a nested nullable Settings object.
/// </summary>
[YamlConverter(typeof(FeatureToggleConverter))]
public class FeatureToggle
{
    public bool Enabled { get; set; }
    public FeatureSettings? Settings { get; set; }
}

/// <summary>
/// Nested settings object.
/// </summary>
public class FeatureSettings
{
    public int MaxRetries { get; set; }
    public int TimeoutSeconds { get; set; }
}

/// <summary>
/// Parent container that holds a FeatureToggle with custom converter.
/// </summary>
public class FeatureContainer
{
    public string? Name { get; set; }
    public FeatureToggle? Feature { get; set; }
}

#endregion

#region Contexts

/// <summary>
/// Basic context without IgnoreEmptyObjects.
/// </summary>
[YamlSerializable<FeatureToggle>]
[YamlSerializable<FeatureSettings>]
public partial class CustomConverterTestContext : YamlSerializerContext { }

/// <summary>
/// Context with IgnoreEmptyObjects = true to test custom converters with nested nullable objects.
/// </summary>
[YamlSerializable<FeatureToggle>]
[YamlSerializable<FeatureSettings>]
[YamlSerializable<FeatureContainer>]
[YamlSourceGenerationOptions(IgnoreNullValues = true, IgnoreEmptyObjects = true)]
public partial class CustomConverterIgnoreEmptyContext : YamlSerializerContext { }

#endregion
