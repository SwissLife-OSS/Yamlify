using Yamlify;
using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Tests for the [KeepNullValue] attribute, which forces a property's null value
/// to be preserved during serialization and deserialization even when
/// <see cref="YamlSerializerOptions.IgnoreNullValues"/> is enabled.
/// </summary>
public class KeepNullValueAttributeTests
{
    #region Serialization Tests

    [Fact]
    public void Serialize_NullReferenceWithoutAttribute_IsOmittedWhenIgnoreNullValuesEnabled()
    {
        var obj = new KeepNullValueModel
        {
            Name = null,
            Description = null,
            Score = null,
            Tag = null
        };

        var yaml = YamlSerializer.Serialize(obj, KeepNullValueContext.Default.KeepNullValueModel);

        Assert.DoesNotContain("name:", yaml);
        Assert.DoesNotContain("score:", yaml);
    }

    [Fact]
    public void Serialize_NullReferenceWithKeepNullValue_IsWrittenAsNull()
    {
        var obj = new KeepNullValueModel
        {
            Name = null,
            Description = null,
            Score = null,
            Tag = null
        };

        var yaml = YamlSerializer.Serialize(obj, KeepNullValueContext.Default.KeepNullValueModel);

        Assert.Contains("description:", yaml);
        Assert.Contains("description: null", yaml);
    }

    [Fact]
    public void Serialize_NullNullableValueTypeWithKeepNullValue_IsWrittenAsNull()
    {
        var obj = new KeepNullValueModel
        {
            Name = null,
            Description = null,
            Score = null,
            Tag = null
        };

        var yaml = YamlSerializer.Serialize(obj, KeepNullValueContext.Default.KeepNullValueModel);

        Assert.Contains("tag:", yaml);
        Assert.Contains("tag: null", yaml);
    }

    [Fact]
    public void Serialize_NonNullValueWithKeepNullValue_WrittenNormally()
    {
        var obj = new KeepNullValueModel
        {
            Name = "alpha",
            Description = "beta",
            Score = 7,
            Tag = 42
        };

        var yaml = YamlSerializer.Serialize(obj, KeepNullValueContext.Default.KeepNullValueModel);

        Assert.Contains("name: alpha", yaml);
        Assert.Contains("description: beta", yaml);
        Assert.Contains("score: 7", yaml);
        Assert.Contains("tag: 42", yaml);
    }

    [Fact]
    public void Serialize_OnlyKeepNullValuePropertiesNull_OthersNotNull_OnlyMarkedAreWritten()
    {
        var obj = new KeepNullValueModel
        {
            Name = "alpha",
            Description = null,
            Score = 5,
            Tag = null
        };

        var yaml = YamlSerializer.Serialize(obj, KeepNullValueContext.Default.KeepNullValueModel);

        Assert.Contains("name: alpha", yaml);
        Assert.Contains("score: 5", yaml);
        Assert.Contains("description: null", yaml);
        Assert.Contains("tag: null", yaml);
    }

    [Fact]
    public void Serialize_RuntimeIgnoreNullValuesEnabled_KeepNullValueStillForcesWrite()
    {
        var obj = new PlainNullableModel
        {
            Name = null,
            Description = null
        };

        var options = new YamlSerializerOptions { IgnoreNullValues = true };
        var yaml = YamlSerializer.Serialize(obj, KeepNullValueContext.Default.PlainNullableModel, options);

        Assert.DoesNotContain("name:", yaml);
        Assert.Contains("description: null", yaml);
    }

    [Fact]
    public void Serialize_RuntimeIgnoreNullValuesDisabled_AllNullsWritten()
    {
        var obj = new PlainNullableModel
        {
            Name = null,
            Description = null
        };

        var options = new YamlSerializerOptions { IgnoreNullValues = false };
        var yaml = YamlSerializer.Serialize(obj, KeepNullValueContext.Default.PlainNullableModel, options);

        Assert.Contains("name: null", yaml);
        Assert.Contains("description: null", yaml);
    }

    #endregion

    #region Deserialization Tests

    [Fact]
    public void Deserialize_ExplicitNullOnKeepNullValueProperty_OverridesClassDefault()
    {
        const string yaml = """
            name: explicit
            description: null
            """;

        var result = YamlSerializer.Deserialize<KeepNullValueWithDefaults>(
            yaml,
            KeepNullValueContext.Default.KeepNullValueWithDefaults);

        Assert.NotNull(result);
        Assert.Equal("explicit", result.Name);
        Assert.Null(result.Description);
    }

    [Fact]
    public void Deserialize_ExplicitNullOnRegularProperty_PreservesClassDefault()
    {
        const string yaml = """
            name: null
            description: explicit
            """;

        var result = YamlSerializer.Deserialize<KeepNullValueWithDefaults>(
            yaml,
            KeepNullValueContext.Default.KeepNullValueWithDefaults);

        Assert.NotNull(result);
        Assert.Equal("default-name", result.Name);
        Assert.Equal("explicit", result.Description);
    }

    [Fact]
    public void Deserialize_MissingProperties_AllDefaultsPreserved()
    {
        const string yaml = "other: 1";

        var result = YamlSerializer.Deserialize<KeepNullValueWithDefaults>(
            yaml,
            KeepNullValueContext.Default.KeepNullValueWithDefaults);

        Assert.NotNull(result);
        Assert.Equal("default-name", result.Name);
        Assert.Equal("default-description", result.Description);
    }

    [Fact]
    public void Deserialize_NonNullValueOnKeepNullValueProperty_ReadsValue()
    {
        const string yaml = """
            name: from-yaml
            description: from-yaml
            """;

        var result = YamlSerializer.Deserialize<KeepNullValueWithDefaults>(
            yaml,
            KeepNullValueContext.Default.KeepNullValueWithDefaults);

        Assert.NotNull(result);
        Assert.Equal("from-yaml", result.Name);
        Assert.Equal("from-yaml", result.Description);
    }

    #endregion

    #region Round Trip Tests

    [Fact]
    public void RoundTrip_NullKeepNullValueProperty_RoundTripsAsNull()
    {
        var original = new KeepNullValueWithDefaults
        {
            Name = "n",
            Description = null
        };

        var yaml = YamlSerializer.Serialize(original, KeepNullValueContext.Default.KeepNullValueWithDefaults);
        var result = YamlSerializer.Deserialize<KeepNullValueWithDefaults>(
            yaml,
            KeepNullValueContext.Default.KeepNullValueWithDefaults);

        Assert.NotNull(result);
        Assert.Equal("n", result.Name);
        Assert.Null(result.Description);
    }

    [Fact]
    public void RoundTrip_NullRegularProperty_LosesValueAndDefaultIsKept()
    {
        var original = new KeepNullValueWithDefaults
        {
            Name = null,
            Description = "d"
        };

        var yaml = YamlSerializer.Serialize(original, KeepNullValueContext.Default.KeepNullValueWithDefaults);
        var result = YamlSerializer.Deserialize<KeepNullValueWithDefaults>(
            yaml,
            KeepNullValueContext.Default.KeepNullValueWithDefaults);

        Assert.NotNull(result);
        // The null Name was omitted on write (because IgnoreNullValues = true and no [KeepNullValue]),
        // so on read the class default is preserved.
        Assert.Equal("default-name", result.Name);
        Assert.Equal("d", result.Description);
    }

    [Fact]
    public void RoundTrip_AnnotatedKeepNullValuesOtherFallbackOnDefault_AllNullsPreserved()
    {
        var original = new KeepNullValueModel
        {
            Description = null,
            Tag = null
        };

        var yaml = YamlSerializer.Serialize(original, KeepNullValueContext.Default.KeepNullValueModel);
        var result = YamlSerializer.Deserialize<KeepNullValueModel>(
            yaml,
            KeepNullValueContext.Default.KeepNullValueModel);

        Assert.NotNull(result);
        Assert.Equal("Tester", result.Name);
        Assert.Null(result.Description);
        Assert.Equal(1, result.Score);
        Assert.Null(result.Tag);
    }

    #endregion
}

#region Test Types

/// <summary>
/// Model used to test [KeepNullValue] across reference and nullable value-type properties.
/// </summary>
public class KeepNullValueModel
{
    public string? Name { get; set; } = "Tester";

    [KeepNullValue]
    public string? Description { get; set; }

    public int? Score { get; set; } = 1;

    [KeepNullValue]
    public int? Tag { get; set; }
}

/// <summary>
/// Model with non-null property defaults used to verify that an explicit YAML null
/// on a <see cref="KeepNullValueAttribute"/>-marked property overrides the default,
/// whereas on a regular property the default is preserved.
/// </summary>
public class KeepNullValueWithDefaults
{
    public string? Name { get; set; } = "default-name";

    [KeepNullValue]
    public string? Description { get; set; } = "default-description";
}

/// <summary>
/// Plain model with one regular and one [KeepNullValue] nullable reference property,
/// used for runtime-options assertions.
/// </summary>
public class PlainNullableModel
{
    public string? Name { get; set; }

    [KeepNullValue]
    public string? Description { get; set; }
}

#endregion

#region Serializer Context

/// <summary>
/// Serializer context with IgnoreNullValues enabled at the source-generator level,
/// matching the configuration that <see cref="KeepNullValueAttribute"/> is designed for.
/// </summary>
[YamlSerializable<KeepNullValueModel>]
[YamlSerializable<KeepNullValueWithDefaults>]
[YamlSerializable<PlainNullableModel>]
[YamlSourceGenerationOptions(IgnoreNullValues = true)]
public partial class KeepNullValueContext : YamlSerializerContext
{
}

#endregion
