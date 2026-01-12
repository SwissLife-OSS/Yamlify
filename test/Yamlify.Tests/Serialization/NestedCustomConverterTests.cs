using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Tests for custom converters on nested types.
/// Verifies that when a type with a custom converter is used as a property of another type,
/// the custom converter is properly invoked during serialization and deserialization.
/// </summary>
public class NestedCustomConverterTests
{
    #region Read Tests - Nested Custom Converter

    [Fact]
    public void Read_NestedTypeWithCustomConverter_StandardFormat_UsesCustomConverter()
    {
        // Arrange - Parent type contains a property with custom converter
        const string yaml = """
            name: my-app
            deployment:
              enabled: true
              region: us-west
            """;

        // Act
        var result = YamlSerializer.Deserialize<AppConfig>(yaml, NestedCustomConverterTestContext.Default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("my-app", result.Name);
        Assert.NotNull(result.Deployment);
        Assert.True(result.Deployment.Enabled);
        Assert.Equal("us-west", result.Deployment.Region);
    }

    [Fact]
    public void Read_NestedTypeWithCustomConverter_LegacyBooleanTrue_UsesCustomConverter()
    {
        // Arrange - Legacy format: deployment is just "true" instead of an object
        // This is the critical test - the custom converter MUST be called for nested types
        const string yaml = """
            name: my-app
            deployment: true
            """;

        // Act
        var result = YamlSerializer.Deserialize<AppConfig>(yaml, NestedCustomConverterTestContext.Default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("my-app", result.Name);
        Assert.NotNull(result.Deployment);
        Assert.True(result.Deployment.Enabled); // Must be true from legacy format!
        Assert.Null(result.Deployment.Region);
    }

    [Fact]
    public void Read_NestedTypeWithCustomConverter_LegacyBooleanFalse_UsesCustomConverter()
    {
        // Arrange - Legacy format: deployment is just "false" instead of an object
        const string yaml = """
            name: my-app
            deployment: false
            """;

        // Act
        var result = YamlSerializer.Deserialize<AppConfig>(yaml, NestedCustomConverterTestContext.Default);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Deployment);
        Assert.False(result.Deployment.Enabled); // Must be false from legacy format!
    }

    [Fact]
    public void Read_NestedTypeWithCustomConverter_StringYes_UsesCustomConverter()
    {
        // Arrange - custom converter handles "yes" string as boolean
        const string yaml = """
            name: my-app
            deployment: "yes"
            """;

        // Act
        var result = YamlSerializer.Deserialize<AppConfig>(yaml, NestedCustomConverterTestContext.Default);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Deployment);
        Assert.True(result.Deployment.Enabled);
    }

    [Fact]
    public void Read_NestedTypeWithCustomConverter_StringNo_UsesCustomConverter()
    {
        // Arrange - custom converter handles "no" string as boolean
        const string yaml = """
            name: my-app
            deployment: "no"
            """;

        // Act
        var result = YamlSerializer.Deserialize<AppConfig>(yaml, NestedCustomConverterTestContext.Default);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Deployment);
        Assert.False(result.Deployment.Enabled);
    }

    #endregion

    #region Write Tests - Nested Custom Converter

    [Fact]
    public void Write_NestedTypeWithCustomConverter_UsesCustomConverter()
    {
        // Arrange
        var config = new AppConfig
        {
            Name = "my-app",
            Deployment = new DeploymentConfig
            {
                Enabled = true,
                Region = "us-west"
            }
        };

        // Act
        var yaml = YamlSerializer.Serialize(config, NestedCustomConverterTestContext.Default);

        // Assert
        Assert.Contains("name: my-app", yaml);
        Assert.Contains("deployment:", yaml);
        Assert.Contains("enabled: true", yaml);
        Assert.Contains("region: us-west", yaml);
    }

    [Fact]
    public void Write_NestedTypeWithCustomConverter_EnabledOnly_SerializesCorrectly()
    {
        // Arrange
        var config = new AppConfig
        {
            Name = "my-app",
            Deployment = new DeploymentConfig { Enabled = true }
        };

        // Act
        var yaml = YamlSerializer.Serialize(config, NestedCustomConverterTestContext.Default);

        // Assert
        Assert.Contains("deployment:", yaml);
        Assert.Contains("enabled: true", yaml);
    }

    #endregion

    #region RoundTrip Tests

    [Fact]
    public void RoundTrip_NestedTypeWithCustomConverter_PreservesData()
    {
        // Arrange
        var original = new AppConfig
        {
            Name = "my-app",
            Deployment = new DeploymentConfig
            {
                Enabled = true,
                Region = "production"
            }
        };

        // Act
        var yaml = YamlSerializer.Serialize(original, NestedCustomConverterTestContext.Default);
        var deserialized = YamlSerializer.Deserialize<AppConfig>(yaml, NestedCustomConverterTestContext.Default);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.NotNull(deserialized.Deployment);
        Assert.Equal(original.Deployment.Enabled, deserialized.Deployment.Enabled);
        Assert.Equal(original.Deployment.Region, deserialized.Deployment.Region);
    }

    [Fact]
    public void RoundTrip_NestedTypeWithCustomConverter_DisabledConfig_PreservesData()
    {
        // Arrange
        var original = new AppConfig
        {
            Name = "my-app",
            Deployment = new DeploymentConfig { Enabled = false }
        };

        // Act
        var yaml = YamlSerializer.Serialize(original, NestedCustomConverterTestContext.Default);
        var deserialized = YamlSerializer.Deserialize<AppConfig>(yaml, NestedCustomConverterTestContext.Default);

        // Assert
        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.Deployment);
        Assert.False(deserialized.Deployment.Enabled);
    }

    #endregion

    #region Collection Tests - Nested Custom Converter in Arrays

    [Fact]
    public void Read_CollectionOfTypesWithCustomConverter_LegacyFormat_UsesCustomConverter()
    {
        // Arrange - Array of configs using legacy boolean format
        const string yaml = """
            name: multi-app
            stages:
            - true
            - false
            - enabled: true
              region: eu-central
            """;

        // Act
        var result = YamlSerializer.Deserialize<AppWithStages>(yaml, NestedCustomConverterTestContext.Default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Stages.Length);
        Assert.True(result.Stages[0].Enabled);   // Legacy true
        Assert.False(result.Stages[1].Enabled);  // Legacy false
        Assert.True(result.Stages[2].Enabled);   // Standard format
        Assert.Equal("eu-central", result.Stages[2].Region);
    }

    [Fact]
    public void Write_CollectionOfTypesWithCustomConverter_SerializesCorrectly()
    {
        // Arrange
        var config = new AppWithStages
        {
            Name = "multi-app",
            Stages =
            [
                new DeploymentConfig { Enabled = true },
                new DeploymentConfig { Enabled = false, Region = "dev" }
            ]
        };

        // Act
        var yaml = YamlSerializer.Serialize(config, NestedCustomConverterTestContext.Default);

        // Assert
        Assert.Contains("stages:", yaml);
        Assert.Contains("enabled: true", yaml);
        Assert.Contains("enabled: false", yaml);
        Assert.Contains("region: dev", yaml);
    }

    [Fact]
    public void RoundTrip_CollectionOfTypesWithCustomConverter_PreservesData()
    {
        // Arrange
        var original = new AppWithStages
        {
            Name = "multi-app",
            Stages =
            [
                new DeploymentConfig { Enabled = true, Region = "prod" },
                new DeploymentConfig { Enabled = false }
            ]
        };

        // Act
        var yaml = YamlSerializer.Serialize(original, NestedCustomConverterTestContext.Default);
        var deserialized = YamlSerializer.Deserialize<AppWithStages>(yaml, NestedCustomConverterTestContext.Default);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Stages.Length);
        Assert.True(deserialized.Stages[0].Enabled);
        Assert.Equal("prod", deserialized.Stages[0].Region);
        Assert.False(deserialized.Stages[1].Enabled);
    }

    #endregion

    #region Deeply Nested Tests

    [Fact]
    public void Read_DeeplyNestedTypeWithCustomConverter_UsesCustomConverter()
    {
        // Arrange - Parent -> Child -> DeploymentConfig (with custom converter)
        const string yaml = """
            name: root
            child:
              name: child
              deployment: true
            """;

        // Act
        var result = YamlSerializer.Deserialize<ParentAppConfig>(yaml, NestedCustomConverterTestContext.Default);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Child);
        Assert.NotNull(result.Child.Deployment);
        Assert.True(result.Child.Deployment.Enabled); // Must be true from legacy format!
    }

    [Fact]
    public void Write_DeeplyNestedTypeWithCustomConverter_SerializesCorrectly()
    {
        // Arrange
        var parent = new ParentAppConfig
        {
            Name = "root",
            Child = new AppConfig
            {
                Name = "child",
                Deployment = new DeploymentConfig { Enabled = true, Region = "nested" }
            }
        };

        // Act
        var yaml = YamlSerializer.Serialize(parent, NestedCustomConverterTestContext.Default);

        // Assert
        Assert.Contains("child:", yaml);
        Assert.Contains("deployment:", yaml);
        Assert.Contains("region: nested", yaml);
    }

    #endregion

    #region Safety Feature Tests

    [Fact]
    public void Read_BuggyConverterThatDoesNotAdvanceReader_ThrowsWithHelpfulMessage()
    {
        // Arrange - This test demonstrates that Yamlify catches buggy converters
        // that forget to call reader.Read() after processing scalar values
        const string yaml = """
            value: test
            """;

        // Act & Assert
        var ex = Assert.Throws<YamlException>(() => 
            YamlSerializer.Deserialize<BuggyConfigWrapper>(yaml, BuggyConverterTestContext.Default));
        
        Assert.Contains("did not advance the reader", ex.Message);
        Assert.Contains("BuggyConverter", ex.Message);
        Assert.Contains("reader.Read()", ex.Message);
    }

    #endregion
}

#region Models

/// <summary>
/// Custom converter that handles legacy boolean format for DeploymentConfig.
/// This simulates backward compatibility for configuration files.
/// </summary>
public class DeploymentConfigConverter : YamlConverter<DeploymentConfig>
{
    public override DeploymentConfig? Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        // Handle legacy format: just a boolean instead of an object
        if (reader.TokenType == YamlTokenType.Scalar)
        {
            var result = new DeploymentConfig { Enabled = false };
            if (reader.TryGetBoolean(out var enabled))
            {
                result.Enabled = enabled;
            }
            else
            {
                // Handle string representations of booleans (yes/no, on/off)
                var str = reader.GetString();
                result.Enabled = str?.ToLowerInvariant() is "yes" or "on" or "1";
            }

            // IMPORTANT: Must advance the reader past the scalar token
            reader.Read();
            return result;
        }

        // Standard object format - delegate to generated code
        return GeneratedRead(ref reader, options);
    }

    public override void Write(Utf8YamlWriter writer, DeploymentConfig value, YamlSerializerOptions options)
    {
        // Always use the standard format for writing
        GeneratedWrite(writer, value, options);
    }
}

/// <summary>
/// Configuration type with a custom converter for backward compatibility.
/// </summary>
[YamlConverter(typeof(DeploymentConfigConverter))]
public class DeploymentConfig
{
    public bool Enabled { get; set; }
    public string? Region { get; set; }
}

/// <summary>
/// Parent type that contains a nested DeploymentConfig with custom converter.
/// </summary>
public class AppConfig
{
    public string? Name { get; set; }
    public DeploymentConfig? Deployment { get; set; }
}

/// <summary>
/// Parent with a collection of types that have custom converters.
/// </summary>
public class AppWithStages
{
    public string? Name { get; set; }
    public DeploymentConfig[] Stages { get; set; } = [];
}

/// <summary>
/// Deeply nested structure to test custom converters at multiple levels.
/// </summary>
public class ParentAppConfig
{
    public string? Name { get; set; }
    public AppConfig? Child { get; set; }
}

#endregion

#region Context

/// <summary>
/// Context for nested custom converter tests.
/// </summary>
[YamlSerializable<DeploymentConfig>]
[YamlSerializable<AppConfig>]
[YamlSerializable<AppWithStages>]
[YamlSerializable<ParentAppConfig>]
public partial class NestedCustomConverterTestContext : YamlSerializerContext { }

#endregion

#region Buggy Converter Test Models

/// <summary>
/// A deliberately buggy converter that forgets to call reader.Read().
/// This demonstrates the safety feature that catches such bugs.
/// </summary>
public class BuggyConverter : YamlConverter<BuggyConfig>
{
    public override BuggyConfig? Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        if (reader.TokenType == YamlTokenType.Scalar)
        {
            // BUG: Forgot to call reader.Read() after reading the scalar!
            // This would cause an infinite loop without the safety check.
            return new BuggyConfig { Value = reader.GetString() };
        }
        return GeneratedRead(ref reader, options);
    }

    public override void Write(Utf8YamlWriter writer, BuggyConfig value, YamlSerializerOptions options)
    {
        GeneratedWrite(writer, value, options);
    }
}

[YamlConverter(typeof(BuggyConverter))]
public class BuggyConfig
{
    public string? Value { get; set; }
}

public class BuggyConfigWrapper
{
    public BuggyConfig? Value { get; set; }
}

[YamlSerializable<BuggyConfig>]
[YamlSerializable<BuggyConfigWrapper>]
public partial class BuggyConverterTestContext : YamlSerializerContext { }

#endregion
