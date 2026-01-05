using Xunit;
using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Tests for the generic attribute syntax [YamlSerializable&lt;T&gt;].
/// </summary>
public class GenericAttributeTests
{
    [Fact]
    public void Serialize_WithGenericAttributeContext_ShouldRoundTrip()
    {
        // Arrange
        var model = new GenericTestModel
        {
            Name = "Test",
            Value = 42
        };

        // Act
        var yaml = YamlSerializer.Serialize(model, GenericTestSerializerContext.Default);
        var result = YamlSerializer.Deserialize<GenericTestModel>(yaml, GenericTestSerializerContext.Default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Serialize_WithGenericAttributeContext_ShouldProduceCorrectYaml()
    {
        // Arrange
        var model = new GenericTestModel
        {
            Name = "Hello",
            Value = 123
        };

        // Act
        var yaml = YamlSerializer.Serialize(model, GenericTestSerializerContext.Default);

        // Assert - Default naming policy uses kebab-case
        Assert.Contains("name: Hello", yaml);
        Assert.Contains("value: 123", yaml);
    }

    [Fact]
    public void Serialize_WithPolymorphicGenericAttribute_ShouldRoundTrip()
    {
        // Arrange
        IGenericShape shape = new GenericCircle { Radius = 5.0 };

        // Act
        var yaml = YamlSerializer.Serialize(shape, GenericTestSerializerContext.Default.IGenericShape);
        var result = YamlSerializer.Deserialize<IGenericShape>(yaml, GenericTestSerializerContext.Default.IGenericShape);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<GenericCircle>(result);
        Assert.Equal(5.0, ((GenericCircle)result).Radius);
    }

    [Fact]
    public void Serialize_WithDerivedTypeMappingAttribute_ShouldRoundTrip()
    {
        // Arrange
        ITransport transport = new Automobile { Manufacturer = "Tesla", Doors = 4 };

        // Act
        var yaml = YamlSerializer.Serialize(transport, DerivedTypeMappingContext.Default.ITransport);
        var result = YamlSerializer.Deserialize<ITransport>(yaml, DerivedTypeMappingContext.Default.ITransport);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Automobile>(result);
        var auto = (Automobile)result;
        Assert.Equal("Tesla", auto.Manufacturer);
        Assert.Equal(4, auto.Doors);
    }

    [Fact]
    public void Serialize_WithDerivedTypeMappingAttribute_ShouldIncludeDiscriminator()
    {
        // Arrange
        ITransport transport = new Bike { Manufacturer = "Harley", HasSidecar = true };

        // Act
        var yaml = YamlSerializer.Serialize(transport, DerivedTypeMappingContext.Default.ITransport);

        // Assert
        Assert.Contains("kind: bike", yaml);
        Assert.Contains("manufacturer: Harley", yaml);
        Assert.Contains("has-sidecar: true", yaml);
    }

    [Fact]
    public void Deserialize_WithDerivedTypeMappingAttribute_ShouldResolveCorrectType()
    {
        // Arrange
        var yaml = """
            kind: automobile
            manufacturer: BMW
            doors: 2
            """;

        // Act
        var result = YamlSerializer.Deserialize<ITransport>(yaml, DerivedTypeMappingContext.Default.ITransport);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Automobile>(result);
        var auto = (Automobile)result;
        Assert.Equal("BMW", auto.Manufacturer);
        Assert.Equal(2, auto.Doors);
    }
}

/// <summary>
/// Test model for generic attribute syntax tests.
/// </summary>
public class GenericTestModel
{
    public string? Name { get; set; }
    public int Value { get; set; }
}

/// <summary>
/// Base interface for polymorphic generic attribute tests.
/// </summary>
public interface IGenericShape
{
    double Area { get; }
}

/// <summary>
/// Circle implementation for polymorphic generic attribute tests.
/// </summary>
public class GenericCircle : IGenericShape
{
    public double Radius { get; set; }
    public double Area => Math.PI * Radius * Radius;
}

/// <summary>
/// Rectangle implementation for polymorphic generic attribute tests.
/// </summary>
public class GenericRectangle : IGenericShape
{
    public double Width { get; set; }
    public double Height { get; set; }
    public double Area => Width * Height;
}

/// <summary>
/// Serializer context using generic attribute syntax [YamlSerializable&lt;T&gt;].
/// </summary>
[YamlSerializable<GenericTestModel>]
[YamlSerializable<GenericCircle>]
[YamlSerializable<GenericRectangle>]
[YamlSerializable<IGenericShape>(
    TypeDiscriminatorPropertyName = "Type",
    DerivedTypes = [typeof(GenericCircle), typeof(GenericRectangle)],
    DerivedTypeDiscriminators = ["circle", "rectangle"])]
public partial class GenericTestSerializerContext : YamlSerializerContext;

/// <summary>
/// Base interface for transport polymorphic tests using YamlDerivedTypeMappingAttribute.
/// </summary>
public interface ITransport
{
    string? Manufacturer { get; set; }
}

/// <summary>
/// Automobile implementation for YamlDerivedTypeMappingAttribute tests.
/// </summary>
public class Automobile : ITransport
{
    public string? Manufacturer { get; set; }
    public int Doors { get; set; }
}

/// <summary>
/// Bike implementation for YamlDerivedTypeMappingAttribute tests.
/// </summary>
public class Bike : ITransport
{
    public string? Manufacturer { get; set; }
    public bool HasSidecar { get; set; }
}

/// <summary>
/// Serializer context using YamlDerivedTypeMappingAttribute for cleaner polymorphic configuration.
/// </summary>
[YamlSerializable<ITransport>(TypeDiscriminatorPropertyName = "kind")]
[YamlDerivedTypeMapping<ITransport, Automobile>("automobile")]
[YamlDerivedTypeMapping<ITransport, Bike>("bike")]
[YamlSerializable<Automobile>]
[YamlSerializable<Bike>]
public partial class DerivedTypeMappingContext : YamlSerializerContext;
