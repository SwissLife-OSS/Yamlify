using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Tests for custom converters that delegate to generated code via GeneratedRead/GeneratedWrite.
/// </summary>
public class CustomConverterDelegationTests
{
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

    [Fact]
    public void Serialize_CustomConverterWithValueTypeOnly_NotSkippedByIgnoreEmptyObjects()
    {
        // Arrange - ArgoCDConfig only has a bool (value type) and nullable Helm
        // When Helm is null, IsEmpty would incorrectly return true if we only check nullable properties
        // But since ArgoCDConfig has a custom converter, IsEmpty should NOT be used
        var config = new WorkloadConfig
        {
            Name = "my-workload",
            ArgoCD = new ArgoCDConfig(Enabled: true, Helm: null), // Helm is null but Enabled is true
            EnableMonitoring = true
        };

        // Act
        var yaml = YamlSerializer.Serialize(config, CustomConverterWithIgnoreEmptyContext.Default);

        // Assert - argo-cd should NOT be skipped despite Helm being null
        Assert.Contains("name: my-workload", yaml);
        Assert.Contains("argo-cd:", yaml);
        Assert.Contains("enabled: true", yaml);
        Assert.Contains("enable-monitoring: true", yaml);
    }

    [Fact]
    public void Serialize_CustomConverterWithHelmConfig_IncludesHelmProperties()
    {
        // Arrange
        var config = new WorkloadConfig
        {
            Name = "my-workload",
            ArgoCD = new ArgoCDConfig(
                Enabled: true,
                Helm: new HelmConfig(ChartName: "my-chart", ChartVersion: "1.0.0")),
            EnableMonitoring = true
        };

        // Act
        var yaml = YamlSerializer.Serialize(config, CustomConverterWithIgnoreEmptyContext.Default);

        // Assert
        Assert.Contains("name: my-workload", yaml);
        Assert.Contains("argo-cd:", yaml);
        Assert.Contains("enabled: true", yaml);
        Assert.Contains("helm:", yaml);
        Assert.Contains("chart-name: my-chart", yaml);
        Assert.Contains("chart-version: 1.0.0", yaml);
    }

    [Fact]
    public void Serialize_CustomConverterWithEnabledFalse_StillNotSkipped()
    {
        // Arrange - Even when Enabled is false and Helm is null, the property should not be skipped
        var config = new WorkloadConfig
        {
            Name = "my-workload",
            ArgoCD = new ArgoCDConfig(Enabled: false, Helm: null),
            EnableMonitoring = true
        };

        // Act
        var yaml = YamlSerializer.Serialize(config, CustomConverterWithIgnoreEmptyContext.Default);

        // Assert - argo-cd should NOT be skipped
        Assert.Contains("name: my-workload", yaml);
        Assert.Contains("argo-cd:", yaml);
        Assert.Contains("enabled: false", yaml);
    }

    [Fact]
    public void Serialize_CustomConverterWithNullValue_IsSkipped()
    {
        // Arrange - When ArgoCD is actually null, it should be skipped (IgnoreNullValues)
        var config = new WorkloadConfig
        {
            Name = "my-workload",
            ArgoCD = null,
            EnableMonitoring = true
        };

        // Act
        var yaml = YamlSerializer.Serialize(config, CustomConverterWithIgnoreEmptyContext.Default);

        // Assert - argo-cd should be skipped because it's null
        Assert.Contains("name: my-workload", yaml);
        Assert.DoesNotContain("argo-cd:", yaml);
        Assert.Contains("enable-monitoring: true", yaml);
    }

    [Fact]
    public void Deserialize_CustomConverterLegacyFormat_ParsesBoolean()
    {
        // Arrange
        const string yaml = """
            name: my-workload
            argo-cd: true
            enable-monitoring: true
            """;

        // Act
        var result = YamlSerializer.Deserialize<WorkloadConfig>(yaml, CustomConverterWithIgnoreEmptyContext.Default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("my-workload", result.Name);
        Assert.NotNull(result.ArgoCD);
        Assert.True(result.ArgoCD.Enabled);
        Assert.Null(result.ArgoCD.Helm);
    }

    [Fact]
    public void Deserialize_CustomConverterNewFormat_ParsesObject()
    {
        // Arrange
        const string yaml = """
            name: my-workload
            argo-cd:
              enabled: true
              helm:
                chart-name: my-chart
                chart-version: 1.0.0
            enable-monitoring: true
            """;

        // Act
        var result = YamlSerializer.Deserialize<WorkloadConfig>(yaml, CustomConverterWithIgnoreEmptyContext.Default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("my-workload", result.Name);
        Assert.NotNull(result.ArgoCD);
        Assert.True(result.ArgoCD.Enabled);
        Assert.NotNull(result.ArgoCD.Helm);
        Assert.Equal("my-chart", result.ArgoCD.Helm.ChartName);
        Assert.Equal("1.0.0", result.ArgoCD.Helm.ChartVersion);
    }

    [Fact]
    public void RoundTrip_CustomConverterWithIgnoreEmptyObjects_PreservesData()
    {
        // Arrange
        var original = new WorkloadConfig
        {
            Name = "my-workload",
            ArgoCD = new ArgoCDConfig(Enabled: true, Helm: null),
            EnableMonitoring = true
        };

        // Act
        var yaml = YamlSerializer.Serialize(original, CustomConverterWithIgnoreEmptyContext.Default);
        var result = YamlSerializer.Deserialize<WorkloadConfig>(yaml, CustomConverterWithIgnoreEmptyContext.Default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
        Assert.NotNull(result.ArgoCD);
        Assert.Equal(original.ArgoCD.Enabled, result.ArgoCD.Enabled);
        Assert.Null(result.ArgoCD.Helm); // Helm was null in original, should still be null
        Assert.Equal(original.EnableMonitoring, result.EnableMonitoring);
    }
}

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
        return GeneratedRead!(ref reader, options);
    }

    public override void Write(Utf8YamlWriter writer, FeatureToggle value, YamlSerializerOptions options)
    {
        // Always use the standard format for writing
        GeneratedWrite!(writer, value, options);
    }
}

/// <summary>
/// Model with a custom converter for migration support.
/// </summary>
[YamlConverter(typeof(FeatureToggleConverter))]
public class FeatureToggle
{
    public bool Enabled { get; set; }
    public FeatureSettings? Settings { get; set; }
}

public class FeatureSettings
{
    public int MaxRetries { get; set; }
    public int TimeoutSeconds { get; set; }
}

[YamlSerializable<FeatureToggle>]
[YamlSerializable<FeatureSettings>]
public partial class CustomConverterTestContext : YamlSerializerContext { }

/// <summary>
/// Model representing ArgoCD configuration with a custom converter for legacy migration.
/// This model has only value-type properties that are always "set" (bool has no "null" state),
/// which previously caused issues with IgnoreEmptyObjects.
/// </summary>
[YamlConverter(typeof(ArgoCDConverter))]
public record ArgoCDConfig(
    [property: YamlPropertyName("enabled")] bool Enabled = true,
    [property: YamlPropertyName("helm")] HelmConfig? Helm = null);

/// <summary>
/// Helm configuration for ArgoCD.
/// </summary>
public record HelmConfig(
    [property: YamlPropertyName("chart-name")] string? ChartName = null,
    [property: YamlPropertyName("chart-version")] string? ChartVersion = null);

/// <summary>
/// Custom converter that handles legacy boolean format while delegating standard format to generated code.
/// </summary>
public class ArgoCDConverter : YamlConverter<ArgoCDConfig>
{
    public override ArgoCDConfig? Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        // Handle legacy format: just a boolean instead of an object
        if (reader.TokenType == YamlTokenType.Scalar)
        {
            var value = reader.GetString();
            reader.Read(); // Consume the scalar
            
            // Parse as boolean for legacy support
            if (bool.TryParse(value, out var enabled))
            {
                return new ArgoCDConfig(Enabled: enabled);
            }
            
            // If not a boolean, treat as false
            return new ArgoCDConfig(Enabled: false);
        }

        // Standard object format - delegate to generated code
        return GeneratedRead!(ref reader, options);
    }

    public override void Write(Utf8YamlWriter writer, ArgoCDConfig value, YamlSerializerOptions options)
    {
        // Always use the standard format for writing
        GeneratedWrite!(writer, value, options);
    }
}

/// <summary>
/// Container that has an optional ArgoCDConfig property.
/// This tests that IgnoreEmptyObjects doesn't incorrectly skip the custom converter type.
/// </summary>
public class WorkloadConfig
{
    public string? Name { get; set; }
    
    [YamlPropertyName("argo-cd")]
    public ArgoCDConfig? ArgoCD { get; set; }
    
    public bool EnableMonitoring { get; set; }
}

[YamlSerializable<WorkloadConfig>]
[YamlSerializable<ArgoCDConfig>]
[YamlSerializable<HelmConfig>]
[YamlSourceGenerationOptions(IgnoreNullValues = true, IgnoreEmptyObjects = true)]
public partial class CustomConverterWithIgnoreEmptyContext : YamlSerializerContext { }
