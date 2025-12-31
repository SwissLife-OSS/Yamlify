using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Tests for property ordering options in YAML serialization.
/// </summary>
public class PropertyOrderingTests
{
    #region Declaration Order Tests (Default)

    [Fact]
    public void Serialize_WithDeclarationOrder_InheritedPropertyShouldComeFirst()
    {
        // The Name property is inherited from InheritedOrderingBase
        // With DeclarationOrder, base class properties should come first
        var obj = new InheritedOrderingDerived
        {
            Name = "Test",
            Type = "TestType",
            Value = "TestValue"
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.InheritedOrderingDerived);

        // Base class property (Name) should come before derived properties (Type, Value)
        var nameIndex = yaml.IndexOf("name:");
        var typeIndex = yaml.IndexOf("type:");
        var valueIndex = yaml.IndexOf("value:");

        Assert.True(nameIndex >= 0, "name: not found");
        Assert.True(typeIndex >= 0, "type: not found");
        Assert.True(valueIndex >= 0, "value: not found");

        // Declaration order: Name (from base), then Type, Value (from derived)
        Assert.True(nameIndex < typeIndex, $"Name ({nameIndex}) should come before Type ({typeIndex}) - inherited properties should come first");
        Assert.True(typeIndex < valueIndex, $"Type ({typeIndex}) should come before Value ({valueIndex})");
    }

    [Fact]
    public void Serialize_WithDeclarationOrder_ShouldPreserveDeclarationOrder()
    {
        // The default TestSerializerContext uses declaration order
        var obj = new UnorderedClass
        {
            Zebra = "Z",
            Alpha = "A",
            Middle = "M"
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.UnorderedClass);

        // Properties should appear in declaration order: Zebra, Alpha, Middle
        var zebraIndex = yaml.IndexOf("zebra:");
        var alphaIndex = yaml.IndexOf("alpha:");
        var middleIndex = yaml.IndexOf("middle:");

        Assert.True(zebraIndex >= 0, "zebra: not found");
        Assert.True(alphaIndex >= 0, "alpha: not found");
        Assert.True(middleIndex >= 0, "middle: not found");

        // Declaration order: Zebra, Alpha, Middle
        Assert.True(zebraIndex < alphaIndex, $"Zebra ({zebraIndex}) should come before Alpha ({alphaIndex})");
        Assert.True(alphaIndex < middleIndex, $"Alpha ({alphaIndex}) should come before Middle ({middleIndex})");
    }

    #endregion

    #region Alphabetical Order Tests

    [Fact]
    public void Serialize_WithAlphabeticalOrder_ShouldSortPropertiesAlphabetically()
    {
        var obj = new AlphabeticalOrderClass
        {
            Zebra = "Z",
            Alpha = "A",
            Middle = "M"
        };

        var yaml = YamlSerializer.Serialize(obj, AlphabeticalSerializerContext.Default.AlphabeticalOrderClass);

        // Properties should appear in alphabetical order: alpha, middle, zebra
        var alphaIndex = yaml.IndexOf("alpha:");
        var middleIndex = yaml.IndexOf("middle:");
        var zebraIndex = yaml.IndexOf("zebra:");

        Assert.True(alphaIndex >= 0, "alpha: not found");
        Assert.True(middleIndex >= 0, "middle: not found");
        Assert.True(zebraIndex >= 0, "zebra: not found");

        // Alphabetical order: alpha, middle, zebra
        Assert.True(alphaIndex < middleIndex, $"Alpha ({alphaIndex}) should come before Middle ({middleIndex})");
        Assert.True(middleIndex < zebraIndex, $"Middle ({middleIndex}) should come before Zebra ({zebraIndex})");
    }

    #endregion

    #region OrderedThenAlphabetical Tests

    [Fact]
    public void Serialize_WithOrderedThenAlphabetical_ShouldPutOrderedPropertiesFirstThenAlphabetical()
    {
        var obj = new OrderedThenAlphabeticalClass
        {
            Identifier = "ID-123",
            Zebra = "Z",
            Alpha = "A",
            Middle = "M"
        };

        var yaml = YamlSerializer.Serialize(obj, OrderedThenAlphabeticalSerializerContext.Default.OrderedThenAlphabeticalClass);

        // Identifier has [YamlPropertyOrder(0)] so it should come first
        // Remaining properties should be alphabetical: alpha, middle, zebra
        var identifierIndex = yaml.IndexOf("identifier:");
        var alphaIndex = yaml.IndexOf("alpha:");
        var middleIndex = yaml.IndexOf("middle:");
        var zebraIndex = yaml.IndexOf("zebra:");

        Assert.True(identifierIndex >= 0, "identifier: not found");
        Assert.True(alphaIndex >= 0, "alpha: not found");
        Assert.True(middleIndex >= 0, "middle: not found");
        Assert.True(zebraIndex >= 0, "zebra: not found");

        // Identifier first, then alphabetical: alpha, middle, zebra
        Assert.True(identifierIndex < alphaIndex, $"Identifier ({identifierIndex}) should come before Alpha ({alphaIndex})");
        Assert.True(alphaIndex < middleIndex, $"Alpha ({alphaIndex}) should come before Middle ({middleIndex})");
        Assert.True(middleIndex < zebraIndex, $"Middle ({middleIndex}) should come before Zebra ({zebraIndex})");
    }

    [Fact]
    public void Serialize_WithOrderedThenAlphabetical_MultipleOrderedProperties_ShouldRespectOrder()
    {
        var obj = new MultipleOrderedPropertiesClass
        {
            ThirdOrdered = "Third",
            FirstOrdered = "First",
            SecondOrdered = "Second",
            Zebra = "Z",
            Alpha = "A"
        };

        var yaml = YamlSerializer.Serialize(obj, OrderedThenAlphabeticalSerializerContext.Default.MultipleOrderedPropertiesClass);

        // Ordered properties should appear first in order: FirstOrdered (0), SecondOrdered (1), ThirdOrdered (2)
        // Then unordered alphabetically: alpha, zebra
        var firstIndex = yaml.IndexOf("first-ordered:");
        var secondIndex = yaml.IndexOf("second-ordered:");
        var thirdIndex = yaml.IndexOf("third-ordered:");
        var alphaIndex = yaml.IndexOf("alpha:");
        var zebraIndex = yaml.IndexOf("zebra:");

        Assert.True(firstIndex >= 0, "first-ordered: not found");
        Assert.True(secondIndex >= 0, "second-ordered: not found");
        Assert.True(thirdIndex >= 0, "third-ordered: not found");
        Assert.True(alphaIndex >= 0, "alpha: not found");
        Assert.True(zebraIndex >= 0, "zebra: not found");

        // Ordered first: FirstOrdered < SecondOrdered < ThirdOrdered
        Assert.True(firstIndex < secondIndex, $"First ({firstIndex}) should come before Second ({secondIndex})");
        Assert.True(secondIndex < thirdIndex, $"Second ({secondIndex}) should come before Third ({thirdIndex})");
        
        // Then alphabetical: alpha < zebra
        Assert.True(thirdIndex < alphaIndex, $"Third ({thirdIndex}) should come before Alpha ({alphaIndex})");
        Assert.True(alphaIndex < zebraIndex, $"Alpha ({alphaIndex}) should come before Zebra ({zebraIndex})");
    }

    #endregion

    #region Per-Type Ordering Override Tests

    [Fact]
    public void Serialize_WithPerTypeOverride_ShouldUseTypeSpecificOrdering()
    {
        // PerTypeOverrideClass uses DeclarationOrder override in an Alphabetical context
        var obj = new PerTypeOverrideClass
        {
            Zebra = "Z",
            Alpha = "A",
            Middle = "M"
        };

        var yaml = YamlSerializer.Serialize(obj, MixedOrderingSerializerContext.Default.PerTypeOverrideClass);

        // Should use declaration order (overriding context's alphabetical)
        var zebraIndex = yaml.IndexOf("zebra:");
        var alphaIndex = yaml.IndexOf("alpha:");
        var middleIndex = yaml.IndexOf("middle:");

        Assert.True(zebraIndex >= 0, "zebra: not found");
        Assert.True(alphaIndex >= 0, "alpha: not found");
        Assert.True(middleIndex >= 0, "middle: not found");

        // Declaration order: Zebra, Alpha, Middle
        Assert.True(zebraIndex < alphaIndex, $"Zebra ({zebraIndex}) should come before Alpha ({alphaIndex})");
        Assert.True(alphaIndex < middleIndex, $"Alpha ({alphaIndex}) should come before Middle ({middleIndex})");
    }

    [Fact]
    public void Serialize_WithoutPerTypeOverride_ShouldUseContextOrdering()
    {
        // NoOverrideClass uses context's alphabetical ordering
        var obj = new NoOverrideClass
        {
            Zebra = "Z",
            Alpha = "A",
            Middle = "M"
        };

        var yaml = YamlSerializer.Serialize(obj, MixedOrderingSerializerContext.Default.NoOverrideClass);

        // Should use alphabetical order (from context)
        var alphaIndex = yaml.IndexOf("alpha:");
        var middleIndex = yaml.IndexOf("middle:");
        var zebraIndex = yaml.IndexOf("zebra:");

        Assert.True(alphaIndex >= 0, "alpha: not found");
        Assert.True(middleIndex >= 0, "middle: not found");
        Assert.True(zebraIndex >= 0, "zebra: not found");

        // Alphabetical order: alpha, middle, zebra
        Assert.True(alphaIndex < middleIndex, $"Alpha ({alphaIndex}) should come before Middle ({middleIndex})");
        Assert.True(middleIndex < zebraIndex, $"Middle ({middleIndex}) should come before Zebra ({zebraIndex})");
    }

    #endregion
}

/// <summary>
/// Test class with properties in non-alphabetical declaration order.
/// </summary>
public class UnorderedClass
{
    // Declaration order: Zebra, Alpha, Middle (intentionally not alphabetical)
    public string Zebra { get; set; } = "";
    public string Alpha { get; set; } = "";
    public string Middle { get; set; } = "";
}

/// <summary>
/// Same structure but used with alphabetical ordering context.
/// </summary>
public class AlphabeticalOrderClass
{
    // Declaration order: Zebra, Alpha, Middle (intentionally not alphabetical)
    public string Zebra { get; set; } = "";
    public string Alpha { get; set; } = "";
    public string Middle { get; set; } = "";
}

/// <summary>
/// Test class for OrderedThenAlphabetical mode - one property has order, rest should be alphabetical.
/// </summary>
public class OrderedThenAlphabeticalClass
{
    // Declaration order: Identifier, Zebra, Alpha, Middle
    [YamlPropertyOrder(0)]
    public string Identifier { get; set; } = "";
    public string Zebra { get; set; } = "";
    public string Alpha { get; set; } = "";
    public string Middle { get; set; } = "";
}

/// <summary>
/// Test class for OrderedThenAlphabetical mode with multiple ordered properties.
/// </summary>
public class MultipleOrderedPropertiesClass
{
    // Declaration order doesn't match order values
    [YamlPropertyOrder(2)]
    public string ThirdOrdered { get; set; } = "";
    
    [YamlPropertyOrder(0)]
    public string FirstOrdered { get; set; } = "";
    
    [YamlPropertyOrder(1)]
    public string SecondOrdered { get; set; } = "";
    
    public string Zebra { get; set; } = "";
    public string Alpha { get; set; } = "";
}

/// <summary>
/// Test class that has per-type ordering override.
/// </summary>
public class PerTypeOverrideClass
{
    // Declaration order: Zebra, Alpha, Middle
    public string Zebra { get; set; } = "";
    public string Alpha { get; set; } = "";
    public string Middle { get; set; } = "";
}

/// <summary>
/// Test class that uses context's ordering (no override).
/// </summary>
public class NoOverrideClass
{
    // Declaration order: Zebra, Alpha, Middle
    public string Zebra { get; set; } = "";
    public string Alpha { get; set; } = "";
    public string Middle { get; set; } = "";
}

/// <summary>
/// Base class for inheritance ordering tests.
/// </summary>
public class InheritedOrderingBase
{
    public string Name { get; set; } = "";
}

/// <summary>
/// Derived class for inheritance ordering tests.
/// </summary>
public class InheritedOrderingDerived : InheritedOrderingBase
{
    public string Type { get; set; } = "";
    public string Value { get; set; } = "";
}

/// <summary>
/// Serializer context with alphabetical property ordering.
/// </summary>
[YamlSourceGenerationOptions(PropertyOrdering = YamlPropertyOrdering.Alphabetical)]
[YamlSerializable(typeof(AlphabeticalOrderClass))]
public partial class AlphabeticalSerializerContext : YamlSerializerContext
{
}

/// <summary>
/// Serializer context with OrderedThenAlphabetical property ordering.
/// </summary>
[YamlSourceGenerationOptions(PropertyOrdering = YamlPropertyOrdering.OrderedThenAlphabetical)]
[YamlSerializable(typeof(OrderedThenAlphabeticalClass))]
[YamlSerializable(typeof(MultipleOrderedPropertiesClass))]
public partial class OrderedThenAlphabeticalSerializerContext : YamlSerializerContext
{
}

/// <summary>
/// Serializer context with mixed ordering: alphabetical by default, but one type overrides to declaration order.
/// </summary>
[YamlSourceGenerationOptions(PropertyOrdering = YamlPropertyOrdering.Alphabetical)]
[YamlSerializable(typeof(PerTypeOverrideClass), PropertyOrdering = YamlPropertyOrdering.DeclarationOrder)]
[YamlSerializable(typeof(NoOverrideClass))]
public partial class MixedOrderingSerializerContext : YamlSerializerContext
{
}
