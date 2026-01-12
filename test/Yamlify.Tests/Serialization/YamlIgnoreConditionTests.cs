using Yamlify;
using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Tests for YamlIgnore attribute with different YamlIgnoreCondition values.
/// </summary>
public class YamlIgnoreConditionTests
{
    #region YamlIgnoreCondition.Always Tests

    [Fact]
    public void Serialize_PropertyWithIgnoreAlways_PropertyNotWritten()
    {
        // Arrange
        var obj = new ClassWithIgnoreAlways
        {
            Name = "Test",
            AlwaysIgnored = "Should not appear",
            Value = 42
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.ClassWithIgnoreAlways);

        // Assert
        Assert.Contains("name: Test", yaml);
        Assert.Contains("value: 42", yaml);
        Assert.DoesNotContain("always-ignored:", yaml);
        Assert.DoesNotContain("Should not appear", yaml);
    }

    [Fact]
    public void Deserialize_PropertyWithIgnoreAlways_PropertyNotRead()
    {
        // Arrange
        const string yaml = """
            name: Test
            always-ignored: This value exists in yaml
            value: 42
            """;

        // Act
        var result = YamlSerializer.Deserialize<ClassWithIgnoreAlways>(yaml, YamlIgnoreConditionContext.Default.ClassWithIgnoreAlways);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Null(result.AlwaysIgnored); // Should remain default (null)
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void RoundTrip_PropertyWithIgnoreAlways_PropertyLost()
    {
        // Arrange
        var original = new ClassWithIgnoreAlways
        {
            Name = "Test",
            AlwaysIgnored = "This will be lost",
            Value = 42
        };

        // Act
        var yaml = YamlSerializer.Serialize(original, YamlIgnoreConditionContext.Default.ClassWithIgnoreAlways);
        var result = YamlSerializer.Deserialize<ClassWithIgnoreAlways>(yaml, YamlIgnoreConditionContext.Default.ClassWithIgnoreAlways);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Null(result.AlwaysIgnored); // Lost during round trip
        Assert.Equal(42, result.Value);
    }

    #endregion

    #region YamlIgnoreCondition.WhenWritingNull Tests

    [Fact]
    public void Serialize_PropertyWithWhenWritingNull_NullValue_PropertyNotWritten()
    {
        // Arrange
        var obj = new ClassWithIgnoreWhenWritingNull
        {
            Name = "Test",
            OptionalValue = null,
            RequiredValue = 42
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.ClassWithIgnoreWhenWritingNull);

        // Assert
        Assert.Contains("name: Test", yaml);
        Assert.Contains("required-value: 42", yaml);
        Assert.DoesNotContain("optional-value:", yaml);
    }

    [Fact]
    public void Serialize_PropertyWithWhenWritingNull_NonNullValue_PropertyWritten()
    {
        // Arrange
        var obj = new ClassWithIgnoreWhenWritingNull
        {
            Name = "Test",
            OptionalValue = "Has a value",
            RequiredValue = 42
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.ClassWithIgnoreWhenWritingNull);

        // Assert
        Assert.Contains("name: Test", yaml);
        Assert.Contains("optional-value: Has a value", yaml);
        Assert.Contains("required-value: 42", yaml);
    }

    [Fact]
    public void Deserialize_PropertyWithWhenWritingNull_PropertyPresent_PropertyRead()
    {
        // Arrange - WhenWritingNull only affects writing, not reading
        const string yaml = """
            name: Test
            optional-value: Value from YAML
            required-value: 42
            """;

        // Act
        var result = YamlSerializer.Deserialize<ClassWithIgnoreWhenWritingNull>(yaml, YamlIgnoreConditionContext.Default.ClassWithIgnoreWhenWritingNull);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal("Value from YAML", result.OptionalValue);
        Assert.Equal(42, result.RequiredValue);
    }

    [Fact]
    public void RoundTrip_PropertyWithWhenWritingNull_NonNullValue_Preserved()
    {
        // Arrange
        var original = new ClassWithIgnoreWhenWritingNull
        {
            Name = "Test",
            OptionalValue = "Keep this",
            RequiredValue = 42
        };

        // Act
        var yaml = YamlSerializer.Serialize(original, YamlIgnoreConditionContext.Default.ClassWithIgnoreWhenWritingNull);
        var result = YamlSerializer.Deserialize<ClassWithIgnoreWhenWritingNull>(yaml, YamlIgnoreConditionContext.Default.ClassWithIgnoreWhenWritingNull);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal("Keep this", result.OptionalValue);
        Assert.Equal(42, result.RequiredValue);
    }

    [Fact]
    public void Serialize_NullableBoolWithWhenWritingNull_NullValue_PropertyNotWritten()
    {
        // Arrange
        var obj = new ClassWithNullableBoolIgnoreWhenNull
        {
            Name = "Test",
            OptionalFlag = null
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.ClassWithNullableBoolIgnoreWhenNull);

        // Assert
        Assert.Contains("name: Test", yaml);
        Assert.DoesNotContain("optional-flag:", yaml);
    }

    [Fact]
    public void Serialize_NullableBoolWithWhenWritingNull_FalseValue_PropertyWritten()
    {
        // Arrange - false is not null, so it should be written
        var obj = new ClassWithNullableBoolIgnoreWhenNull
        {
            Name = "Test",
            OptionalFlag = false
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.ClassWithNullableBoolIgnoreWhenNull);

        // Assert
        Assert.Contains("name: Test", yaml);
        Assert.Contains("optional-flag: false", yaml);
    }

    [Fact]
    public void Serialize_NullableBoolWithWhenWritingNull_TrueValue_PropertyWritten()
    {
        // Arrange
        var obj = new ClassWithNullableBoolIgnoreWhenNull
        {
            Name = "Test",
            OptionalFlag = true
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.ClassWithNullableBoolIgnoreWhenNull);

        // Assert
        Assert.Contains("name: Test", yaml);
        Assert.Contains("optional-flag: true", yaml);
    }

    #endregion

    #region YamlIgnoreCondition.WhenWritingDefault Tests

    [Fact]
    public void Serialize_IntWithWhenWritingDefault_ZeroValue_PropertyNotWritten()
    {
        // Arrange
        var obj = new ClassWithIgnoreWhenWritingDefault
        {
            Name = "Test",
            Counter = 0 // default for int
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.ClassWithIgnoreWhenWritingDefault);

        // Assert
        Assert.Contains("name: Test", yaml);
        Assert.DoesNotContain("counter:", yaml);
    }

    [Fact]
    public void Serialize_IntWithWhenWritingDefault_NonZeroValue_PropertyWritten()
    {
        // Arrange
        var obj = new ClassWithIgnoreWhenWritingDefault
        {
            Name = "Test",
            Counter = 5
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.ClassWithIgnoreWhenWritingDefault);

        // Assert
        Assert.Contains("name: Test", yaml);
        Assert.Contains("counter: 5", yaml);
    }

    [Fact]
    public void Serialize_BoolWithWhenWritingDefault_FalseValue_PropertyNotWritten()
    {
        // Arrange
        var obj = new ClassWithBoolIgnoreWhenDefault
        {
            Name = "Test",
            IsEnabled = false // default for bool
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.ClassWithBoolIgnoreWhenDefault);

        // Assert
        Assert.Contains("name: Test", yaml);
        Assert.DoesNotContain("is-enabled:", yaml);
    }

    [Fact]
    public void Serialize_BoolWithWhenWritingDefault_TrueValue_PropertyWritten()
    {
        // Arrange
        var obj = new ClassWithBoolIgnoreWhenDefault
        {
            Name = "Test",
            IsEnabled = true
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.ClassWithBoolIgnoreWhenDefault);

        // Assert
        Assert.Contains("name: Test", yaml);
        Assert.Contains("is-enabled: true", yaml);
    }

    [Fact]
    public void Serialize_StringWithWhenWritingDefault_NullValue_PropertyNotWritten()
    {
        // Arrange - default for string? is null
        var obj = new ClassWithStringIgnoreWhenDefault
        {
            Name = "Test",
            Description = null
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.ClassWithStringIgnoreWhenDefault);

        // Assert
        Assert.Contains("name: Test", yaml);
        Assert.DoesNotContain("description:", yaml);
    }

    [Fact]
    public void Serialize_StringWithWhenWritingDefault_EmptyString_PropertyWritten()
    {
        // Arrange - empty string is not null (the default), so it should be written
        var obj = new ClassWithStringIgnoreWhenDefault
        {
            Name = "Test",
            Description = ""
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.ClassWithStringIgnoreWhenDefault);

        // Assert
        Assert.Contains("name: Test", yaml);
        Assert.Contains("description:", yaml);
    }

    [Fact]
    public void Deserialize_PropertyWithWhenWritingDefault_PropertyPresent_PropertyRead()
    {
        // Arrange - WhenWritingDefault only affects writing, not reading
        const string yaml = """
            name: Test
            counter: 0
            """;

        // Act
        var result = YamlSerializer.Deserialize<ClassWithIgnoreWhenWritingDefault>(yaml, YamlIgnoreConditionContext.Default.ClassWithIgnoreWhenWritingDefault);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(0, result.Counter);
    }

    #endregion

    #region YamlIgnoreCondition.Never Tests

    [Fact]
    public void Serialize_PropertyWithIgnoreNever_NullValue_PropertyWritten()
    {
        // Arrange - Never means always write, even if null
        var obj = new ClassWithIgnoreNever
        {
            Name = "Test",
            AlwaysWritten = null
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.ClassWithIgnoreNever);

        // Assert
        Assert.Contains("name: Test", yaml);
        Assert.Contains("always-written:", yaml);
    }

    [Fact]
    public void Serialize_PropertyWithIgnoreNever_NonNullValue_PropertyWritten()
    {
        // Arrange
        var obj = new ClassWithIgnoreNever
        {
            Name = "Test",
            AlwaysWritten = "Has a value"
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.ClassWithIgnoreNever);

        // Assert
        Assert.Contains("name: Test", yaml);
        Assert.Contains("always-written: Has a value", yaml);
    }

    [Fact]
    public void Deserialize_PropertyWithIgnoreNever_PropertyPresent_PropertyRead()
    {
        // Arrange
        const string yaml = """
            name: Test
            always-written: Value from YAML
            """;

        // Act
        var result = YamlSerializer.Deserialize<ClassWithIgnoreNever>(yaml, YamlIgnoreConditionContext.Default.ClassWithIgnoreNever);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal("Value from YAML", result.AlwaysWritten);
    }

    #endregion

    #region Legacy Migration Scenario Tests

    [Fact]
    public void Deserialize_LegacyProperty_WithWhenWritingNull_PropertyRead()
    {
        // Arrange - Simulates reading a legacy YAML format
        const string yaml = """
            name: my-feature
            legacy-enabled: true
            """;

        // Act
        var result = YamlSerializer.Deserialize<FeatureConfig>(yaml, YamlIgnoreConditionContext.Default.FeatureConfig);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("my-feature", result.Name);
        Assert.True(result.LegacyEnabled); // Should be read from YAML
    }

    [Fact]
    public void Serialize_LegacyProperty_WithWhenWritingNull_WhenNull_PropertyNotWritten()
    {
        // Arrange - Legacy property is null (not migrated or already migrated)
        var obj = new FeatureConfig
        {
            Name = "my-feature",
            LegacyEnabled = null,
            Settings = new FeatureConfigSettings { Enabled = true }
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.FeatureConfig);

        // Assert
        Assert.Contains("name: my-feature", yaml);
        Assert.Contains("settings:", yaml);
        Assert.Contains("enabled: true", yaml);
        Assert.DoesNotContain("legacy-enabled:", yaml); // Should not be written
    }

    [Fact]
    public void RoundTrip_LegacyPropertyRead_ThenNotWritten()
    {
        // Arrange - Simulates reading legacy YAML
        const string legacyYaml = """
            name: my-feature
            legacy-enabled: false
            """;

        // Act - Read the legacy format
        var config = YamlSerializer.Deserialize<FeatureConfig>(legacyYaml, YamlIgnoreConditionContext.Default.FeatureConfig);
        
        // Simulate migration: use legacy value to populate new format, then clear legacy
        if (config != null && config.LegacyEnabled.HasValue)
        {
            config.Settings = new FeatureConfigSettings { Enabled = config.LegacyEnabled.Value };
            config.LegacyEnabled = null;
        }

        // Serialize back
        var newYaml = YamlSerializer.Serialize(config!, YamlIgnoreConditionContext.Default.FeatureConfig);

        // Assert - Legacy property should not appear in output
        Assert.Contains("name: my-feature", newYaml);
        Assert.Contains("settings:", newYaml);
        Assert.Contains("enabled: false", newYaml);
        Assert.DoesNotContain("legacy-enabled:", newYaml);
    }

    #endregion

    #region Multiple Conditions on Different Properties

    [Fact]
    public void Serialize_MixedConditions_CorrectPropertiesWritten()
    {
        // Arrange
        var obj = new ClassWithMixedConditions
        {
            Name = "Test",
            AlwaysIgnored = "should not appear",
            NullIgnored = null,
            DefaultIgnored = 0,
            NeverIgnored = null
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.ClassWithMixedConditions);

        // Assert
        Assert.Contains("name: Test", yaml);
        Assert.DoesNotContain("always-ignored:", yaml); // Always ignored
        Assert.DoesNotContain("null-ignored:", yaml); // WhenWritingNull + null value
        Assert.DoesNotContain("default-ignored:", yaml); // WhenWritingDefault + 0
        Assert.Contains("never-ignored:", yaml); // Never - always written even when null
    }

    [Fact]
    public void Serialize_MixedConditions_AllNonDefault_AllWritten()
    {
        // Arrange
        var obj = new ClassWithMixedConditions
        {
            Name = "Test",
            AlwaysIgnored = "still ignored",
            NullIgnored = "has value",
            DefaultIgnored = 5,
            NeverIgnored = "has value"
        };

        // Act
        var yaml = YamlSerializer.Serialize(obj, YamlIgnoreConditionContext.Default.ClassWithMixedConditions);

        // Assert
        Assert.Contains("name: Test", yaml);
        Assert.DoesNotContain("always-ignored:", yaml); // Still ignored
        Assert.Contains("null-ignored: has value", yaml); // Written because not null
        Assert.Contains("default-ignored: 5", yaml); // Written because not default
        Assert.Contains("never-ignored: has value", yaml); // Always written
    }

    #endregion
}

#region Test Models

/// <summary>
/// Class with YamlIgnore(Condition = Always) - property completely ignored.
/// </summary>
public class ClassWithIgnoreAlways
{
    public string? Name { get; set; }
    
    [YamlPropertyName("always-ignored")]
    [YamlIgnore(Condition = YamlIgnoreCondition.Always)]
    public string? AlwaysIgnored { get; set; }
    
    public int Value { get; set; }
}

/// <summary>
/// Class with YamlIgnore(Condition = WhenWritingNull) - only ignored during serialization when null.
/// </summary>
public class ClassWithIgnoreWhenWritingNull
{
    public string? Name { get; set; }
    
    [YamlPropertyName("optional-value")]
    [YamlIgnore(Condition = YamlIgnoreCondition.WhenWritingNull)]
    public string? OptionalValue { get; set; }
    
    [YamlPropertyName("required-value")]
    public int RequiredValue { get; set; }
}

/// <summary>
/// Class with nullable bool and WhenWritingNull condition.
/// </summary>
public class ClassWithNullableBoolIgnoreWhenNull
{
    public string? Name { get; set; }
    
    [YamlPropertyName("optional-flag")]
    [YamlIgnore(Condition = YamlIgnoreCondition.WhenWritingNull)]
    public bool? OptionalFlag { get; set; }
}

/// <summary>
/// Class with YamlIgnore(Condition = WhenWritingDefault) on an int property.
/// </summary>
public class ClassWithIgnoreWhenWritingDefault
{
    public string? Name { get; set; }
    
    [YamlIgnore(Condition = YamlIgnoreCondition.WhenWritingDefault)]
    public int Counter { get; set; }
}

/// <summary>
/// Class with YamlIgnore(Condition = WhenWritingDefault) on a bool property.
/// </summary>
public class ClassWithBoolIgnoreWhenDefault
{
    public string? Name { get; set; }
    
    [YamlPropertyName("is-enabled")]
    [YamlIgnore(Condition = YamlIgnoreCondition.WhenWritingDefault)]
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Class with YamlIgnore(Condition = WhenWritingDefault) on a string property.
/// </summary>
public class ClassWithStringIgnoreWhenDefault
{
    public string? Name { get; set; }
    
    [YamlIgnore(Condition = YamlIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; set; }
}

/// <summary>
/// Class with YamlIgnore(Condition = Never) - always written even when null.
/// </summary>
public class ClassWithIgnoreNever
{
    public string? Name { get; set; }
    
    [YamlPropertyName("always-written")]
    [YamlIgnore(Condition = YamlIgnoreCondition.Never)]
    public string? AlwaysWritten { get; set; }
}

/// <summary>
/// Simulates a legacy migration scenario where a property is read but not written back.
/// </summary>
public class FeatureConfig
{
    public string? Name { get; set; }
    
    /// <summary>
    /// Legacy property - read from old YAML but not written back.
    /// </summary>
    [YamlPropertyName("legacy-enabled")]
    [YamlIgnore(Condition = YamlIgnoreCondition.WhenWritingNull)]
    public bool? LegacyEnabled { get; set; }
    
    /// <summary>
    /// New format settings object.
    /// </summary>
    public FeatureConfigSettings? Settings { get; set; }
}

/// <summary>
/// Settings object for the new format.
/// </summary>
public class FeatureConfigSettings
{
    public bool Enabled { get; set; }
}

/// <summary>
/// Class with different YamlIgnoreCondition values on different properties.
/// </summary>
public class ClassWithMixedConditions
{
    public string? Name { get; set; }
    
    [YamlPropertyName("always-ignored")]
    [YamlIgnore(Condition = YamlIgnoreCondition.Always)]
    public string? AlwaysIgnored { get; set; }
    
    [YamlPropertyName("null-ignored")]
    [YamlIgnore(Condition = YamlIgnoreCondition.WhenWritingNull)]
    public string? NullIgnored { get; set; }
    
    [YamlPropertyName("default-ignored")]
    [YamlIgnore(Condition = YamlIgnoreCondition.WhenWritingDefault)]
    public int DefaultIgnored { get; set; }
    
    [YamlPropertyName("never-ignored")]
    [YamlIgnore(Condition = YamlIgnoreCondition.Never)]
    public string? NeverIgnored { get; set; }
}

#endregion

#region Serializer Context

/// <summary>
/// Serializer context with IgnoreNullValues disabled to properly test YamlIgnoreCondition.
/// </summary>
[YamlSerializable<ClassWithIgnoreAlways>]
[YamlSerializable<ClassWithIgnoreWhenWritingNull>]
[YamlSerializable<ClassWithNullableBoolIgnoreWhenNull>]
[YamlSerializable<ClassWithIgnoreWhenWritingDefault>]
[YamlSerializable<ClassWithBoolIgnoreWhenDefault>]
[YamlSerializable<ClassWithStringIgnoreWhenDefault>]
[YamlSerializable<ClassWithIgnoreNever>]
[YamlSerializable<FeatureConfig>]
[YamlSerializable<FeatureConfigSettings>]
[YamlSerializable<ClassWithMixedConditions>]
public partial class YamlIgnoreConditionContext : YamlSerializerContext
{
}

#endregion
