using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

#region Sibling Discriminator Test Models

/// <summary>
/// Enum representing different value types (used as discriminator).
/// </summary>
public enum ValueType
{
    Integer,
    String,
    Boolean
}

/// <summary>
/// Base class for polymorphic values (abstract).
/// </summary>
public abstract class VariableValueBase
{
}

/// <summary>
/// Integer value implementation.
/// </summary>
public class IntegerVariableValue : VariableValueBase
{
    public int Value { get; set; }
}

/// <summary>
/// String value implementation.
/// </summary>
public class StringVariableValue : VariableValueBase
{
    public string Value { get; set; } = "";
}

/// <summary>
/// Boolean value implementation.
/// </summary>
public class BooleanVariableValue : VariableValueBase
{
    public bool Value { get; set; }
}

/// <summary>
/// Container class that uses sibling discriminator pattern.
/// The Type property determines the concrete type of Value.
/// </summary>
public class VariableContainer
{
    public string Name { get; set; } = "";
    
    /// <summary>
    /// The discriminator property that determines the type of Value.
    /// </summary>
    public ValueType Type { get; set; }
    
    /// <summary>
    /// Polymorphic property - concrete type determined by Type property.
    /// </summary>
    [YamlSiblingDiscriminator(nameof(Type))]
    [YamlDiscriminatorMapping(nameof(ValueType.Integer), typeof(IntegerVariableValue))]
    [YamlDiscriminatorMapping(nameof(ValueType.String), typeof(StringVariableValue))]
    [YamlDiscriminatorMapping(nameof(ValueType.Boolean), typeof(BooleanVariableValue))]
    public VariableValueBase? Value { get; set; }
}

/// <summary>
/// Simple container without optional value (for basic testing).
/// </summary>
public class SimpleVariableContainer
{
    public string Name { get; set; } = "";
    public ValueType Type { get; set; }
    
    [YamlSiblingDiscriminator(nameof(Type))]
    [YamlDiscriminatorMapping(nameof(ValueType.Integer), typeof(IntegerVariableValue))]
    [YamlDiscriminatorMapping(nameof(ValueType.String), typeof(StringVariableValue))]
    [YamlDiscriminatorMapping(nameof(ValueType.Boolean), typeof(BooleanVariableValue))]
    public VariableValueBase? Value { get; set; }
}

/// <summary>
/// Represents an environment like Dev, Prod, etc.
/// </summary>
public enum TestEnvironment
{
    Dev,
    Prod,
    Staging
}

/// <summary>
/// Container with a dictionary of environment overrides where each value is polymorphic.
/// This tests the sibling discriminator pattern applied to dictionary values.
/// </summary>
public class VariableWithEnvironmentOverrides
{
    public string Name { get; set; } = "";
    public ValueType Type { get; set; }
    
    [YamlSiblingDiscriminator(nameof(Type))]
    [YamlDiscriminatorMapping(nameof(ValueType.Integer), typeof(IntegerVariableValue))]
    [YamlDiscriminatorMapping(nameof(ValueType.String), typeof(StringVariableValue))]
    [YamlDiscriminatorMapping(nameof(ValueType.Boolean), typeof(BooleanVariableValue))]
    public VariableValueBase? Value { get; set; }
    
    /// <summary>
    /// Dictionary of environment-specific overrides. Each value's concrete type is determined by the Type property.
    /// </summary>
    [YamlSiblingDiscriminator(nameof(Type))]
    [YamlDiscriminatorMapping(nameof(ValueType.Integer), typeof(IntegerVariableValue))]
    [YamlDiscriminatorMapping(nameof(ValueType.String), typeof(StringVariableValue))]
    [YamlDiscriminatorMapping(nameof(ValueType.Boolean), typeof(BooleanVariableValue))]
    public Dictionary<TestEnvironment, VariableValueBase?>? EnvironmentOverrides { get; set; }
}

#endregion

/// <summary>
/// Tests for sibling discriminator pattern using [YamlSiblingDiscriminator] and [YamlDiscriminatorMapping] attributes.
/// This pattern allows a sibling property (like "Type") to determine the concrete type of another property (like "Value").
/// </summary>
public class SiblingDiscriminatorSerializationTests
{
    #region Deserialization Tests

    [Fact]
    public void Deserialize_IntegerValue_ShouldCreateCorrectType()
    {
        var yaml = """
            name: port
            type: Integer
            value:
              value: 8080
            """;

        var container = YamlSerializer.Deserialize<SimpleVariableContainer>(
            yaml, SiblingDiscriminatorSerializerContext.Default.SimpleVariableContainer);

        Assert.NotNull(container);
        Assert.Equal("port", container.Name);
        Assert.Equal(ValueType.Integer, container.Type);
        Assert.NotNull(container.Value);
        Assert.IsType<IntegerVariableValue>(container.Value);
        var intValue = (IntegerVariableValue)container.Value;
        Assert.Equal(8080, intValue.Value);
    }

    [Fact]
    public void Deserialize_StringValue_ShouldCreateCorrectType()
    {
        var yaml = """
            name: hostname
            type: String
            value:
              value: localhost
            """;

        var container = YamlSerializer.Deserialize<SimpleVariableContainer>(
            yaml, SiblingDiscriminatorSerializerContext.Default.SimpleVariableContainer);

        Assert.NotNull(container);
        Assert.Equal("hostname", container.Name);
        Assert.Equal(ValueType.String, container.Type);
        Assert.NotNull(container.Value);
        Assert.IsType<StringVariableValue>(container.Value);
        var strValue = (StringVariableValue)container.Value;
        Assert.Equal("localhost", strValue.Value);
    }

    [Fact]
    public void Deserialize_BooleanValue_ShouldCreateCorrectType()
    {
        var yaml = """
            name: is-enabled
            type: Boolean
            value:
              value: true
            """;

        var container = YamlSerializer.Deserialize<SimpleVariableContainer>(
            yaml, SiblingDiscriminatorSerializerContext.Default.SimpleVariableContainer);

        Assert.NotNull(container);
        Assert.Equal("is-enabled", container.Name);
        Assert.Equal(ValueType.Boolean, container.Type);
        Assert.NotNull(container.Value);
        Assert.IsType<BooleanVariableValue>(container.Value);
        var boolValue = (BooleanVariableValue)container.Value;
        Assert.True(boolValue.Value);
    }

    [Fact]
    public void Deserialize_TypeBeforeValue_ShouldWork()
    {
        // Type appears before Value in YAML (natural order)
        var yaml = """
            type: Integer
            name: port
            value:
              value: 3000
            """;

        var container = YamlSerializer.Deserialize<SimpleVariableContainer>(
            yaml, SiblingDiscriminatorSerializerContext.Default.SimpleVariableContainer);

        Assert.NotNull(container);
        Assert.IsType<IntegerVariableValue>(container.Value);
        Assert.Equal(3000, ((IntegerVariableValue)container.Value!).Value);
    }

    [Fact]
    public void Deserialize_TypeAfterValue_ShouldWork()
    {
        // Type appears after Value in YAML (requires buffering or two-pass)
        var yaml = """
            name: port
            value:
              value: 5000
            type: Integer
            """;

        var container = YamlSerializer.Deserialize<SimpleVariableContainer>(
            yaml, SiblingDiscriminatorSerializerContext.Default.SimpleVariableContainer);

        Assert.NotNull(container);
        Assert.IsType<IntegerVariableValue>(container.Value);
        Assert.Equal(5000, ((IntegerVariableValue)container.Value!).Value);
    }

    [Fact]
    public void Deserialize_NullValue_ShouldBeNull()
    {
        var yaml = """
            name: optional-field
            type: Integer
            """;

        var container = YamlSerializer.Deserialize<SimpleVariableContainer>(
            yaml, SiblingDiscriminatorSerializerContext.Default.SimpleVariableContainer);

        Assert.NotNull(container);
        Assert.Equal("optional-field", container.Name);
        Assert.Null(container.Value);
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void Roundtrip_IntegerValue_ShouldPreserveType()
    {
        var original = new SimpleVariableContainer
        {
            Name = "port",
            Type = ValueType.Integer,
            Value = new IntegerVariableValue { Value = 8080 }
        };

        var yaml = YamlSerializer.Serialize(original, 
            SiblingDiscriminatorSerializerContext.Default.SimpleVariableContainer);
        var deserialized = YamlSerializer.Deserialize<SimpleVariableContainer>(
            yaml, SiblingDiscriminatorSerializerContext.Default.SimpleVariableContainer);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.Type, deserialized.Type);
        Assert.IsType<IntegerVariableValue>(deserialized.Value);
        Assert.Equal(8080, ((IntegerVariableValue)deserialized.Value!).Value);
    }

    [Fact]
    public void Roundtrip_StringValue_ShouldPreserveType()
    {
        var original = new SimpleVariableContainer
        {
            Name = "hostname",
            Type = ValueType.String,
            Value = new StringVariableValue { Value = "example.com" }
        };

        var yaml = YamlSerializer.Serialize(original,
            SiblingDiscriminatorSerializerContext.Default.SimpleVariableContainer);
        var deserialized = YamlSerializer.Deserialize<SimpleVariableContainer>(
            yaml, SiblingDiscriminatorSerializerContext.Default.SimpleVariableContainer);

        Assert.NotNull(deserialized);
        Assert.IsType<StringVariableValue>(deserialized.Value);
        Assert.Equal("example.com", ((StringVariableValue)deserialized.Value!).Value);
    }

    [Fact]
    public void Roundtrip_BooleanValue_ShouldPreserveType()
    {
        var original = new SimpleVariableContainer
        {
            Name = "debug-mode",
            Type = ValueType.Boolean,
            Value = new BooleanVariableValue { Value = true }
        };

        var yaml = YamlSerializer.Serialize(original,
            SiblingDiscriminatorSerializerContext.Default.SimpleVariableContainer);
        var deserialized = YamlSerializer.Deserialize<SimpleVariableContainer>(
            yaml, SiblingDiscriminatorSerializerContext.Default.SimpleVariableContainer);

        Assert.NotNull(deserialized);
        Assert.IsType<BooleanVariableValue>(deserialized.Value);
        Assert.True(((BooleanVariableValue)deserialized.Value!).Value);
    }

    #endregion

    #region List of Containers Tests

    [Fact]
    public void Deserialize_ListOfContainers_ShouldCreateCorrectTypes()
    {
        var yaml = """
            - name: port
              type: Integer
              value:
                value: 8080
            - name: hostname
              type: String
              value:
                value: localhost
            - name: debug
              type: Boolean
              value:
                value: true
            """;

        var containers = YamlSerializer.Deserialize<List<SimpleVariableContainer>>(
            yaml, SiblingDiscriminatorSerializerContext.Default.ListSimpleVariableContainer);

        Assert.NotNull(containers);
        Assert.Equal(3, containers.Count);
        Assert.IsType<IntegerVariableValue>(containers[0].Value);
        Assert.IsType<StringVariableValue>(containers[1].Value);
        Assert.IsType<BooleanVariableValue>(containers[2].Value);
    }

    #endregion

    #region Dictionary with Polymorphic Values Tests

    [Fact]
    public void Deserialize_DictionaryWithPolymorphicValues_ShouldCreateCorrectTypes()
    {
        var yaml = """
            name: timeout
            type: Integer
            value:
              value: 30
            environment-overrides:
              Dev:
                value: 60
              Prod:
                value: 120
            """;

        var container = YamlSerializer.Deserialize<VariableWithEnvironmentOverrides>(
            yaml, SiblingDiscriminatorSerializerContext.Default.VariableWithEnvironmentOverrides);

        Assert.NotNull(container);
        Assert.Equal("timeout", container.Name);
        Assert.Equal(ValueType.Integer, container.Type);
        
        // Check base value
        Assert.NotNull(container.Value);
        Assert.IsType<IntegerVariableValue>(container.Value);
        Assert.Equal(30, ((IntegerVariableValue)container.Value).Value);
        
        // Check environment overrides
        Assert.NotNull(container.EnvironmentOverrides);
        Assert.Equal(2, container.EnvironmentOverrides.Count);
        
        var devValue = container.EnvironmentOverrides[TestEnvironment.Dev] as IntegerVariableValue;
        Assert.NotNull(devValue);
        Assert.Equal(60, devValue.Value);
        
        var prodValue = container.EnvironmentOverrides[TestEnvironment.Prod] as IntegerVariableValue;
        Assert.NotNull(prodValue);
        Assert.Equal(120, prodValue.Value);
    }

    [Fact]
    public void Roundtrip_DictionaryWithPolymorphicValues_ShouldPreserveTypes()
    {
        var original = new VariableWithEnvironmentOverrides
        {
            Name = "hostname",
            Type = ValueType.String,
            Value = new StringVariableValue { Value = "localhost" },
            EnvironmentOverrides = new Dictionary<TestEnvironment, VariableValueBase?>
            {
                { TestEnvironment.Dev, new StringVariableValue { Value = "dev.example.com" } },
                { TestEnvironment.Prod, new StringVariableValue { Value = "prod.example.com" } }
            }
        };

        var yaml = YamlSerializer.Serialize(original,
            SiblingDiscriminatorSerializerContext.Default.VariableWithEnvironmentOverrides);
        var deserialized = YamlSerializer.Deserialize<VariableWithEnvironmentOverrides>(
            yaml, SiblingDiscriminatorSerializerContext.Default.VariableWithEnvironmentOverrides);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.Type, deserialized.Type);
        
        // Check base value
        Assert.IsType<StringVariableValue>(deserialized.Value);
        Assert.Equal("localhost", ((StringVariableValue)deserialized.Value!).Value);
        
        // Check environment overrides
        Assert.NotNull(deserialized.EnvironmentOverrides);
        Assert.Equal(2, deserialized.EnvironmentOverrides.Count);
        
        var devValue = deserialized.EnvironmentOverrides[TestEnvironment.Dev] as StringVariableValue;
        Assert.NotNull(devValue);
        Assert.Equal("dev.example.com", devValue.Value);
        
        var prodValue = deserialized.EnvironmentOverrides[TestEnvironment.Prod] as StringVariableValue;
        Assert.NotNull(prodValue);
        Assert.Equal("prod.example.com", prodValue.Value);
    }

    #endregion
}

/// <summary>
/// Serializer context for sibling discriminator tests.
/// </summary>
[YamlSerializable(typeof(VariableValueBase))]
[YamlSerializable(typeof(IntegerVariableValue))]
[YamlSerializable(typeof(StringVariableValue))]
[YamlSerializable(typeof(BooleanVariableValue))]
[YamlSerializable(typeof(VariableContainer))]
[YamlSerializable(typeof(SimpleVariableContainer))]
[YamlSerializable(typeof(VariableWithEnvironmentOverrides))]
[YamlSerializable(typeof(List<SimpleVariableContainer>))]
public partial class SiblingDiscriminatorSerializerContext : YamlSerializerContext { }
