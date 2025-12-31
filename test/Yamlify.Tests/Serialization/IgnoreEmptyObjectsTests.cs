using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Class with nested object that may be empty.
/// </summary>
public class ParentWithMetadata
{
    public string? Name { get; set; }
    public MetadataClass? Metadata { get; set; }
    public int Value { get; set; }
}

/// <summary>
/// Class that can be empty (all nullable properties null).
/// </summary>
public class MetadataClass
{
    public string? Author { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
}

/// <summary>
/// Class with multiple nested objects.
/// </summary>
public class ContainerWithMultipleNested
{
    public string? Id { get; set; }
    public MetadataClass? Metadata { get; set; }
    public MetadataClass? ExtraMetadata { get; set; }
    public SimpleClass? Child { get; set; }
}

/// <summary>
/// Deeply nested structure to test empty object handling at multiple levels.
/// </summary>
public class OuterLevel
{
    public string? Name { get; set; }
    public MiddleLevel? Middle { get; set; }
}

public class MiddleLevel
{
    public string? Title { get; set; }
    public InnerLevel? Inner { get; set; }
}

public class InnerLevel
{
    public string? Value { get; set; }
    public string? Extra { get; set; }
}

/// <summary>
/// Abstract base class for polymorphic value types.
/// Used to test that IgnoreEmptyObjects doesn't incorrectly skip polymorphic properties.
/// </summary>
[YamlPolymorphic(TypeDiscriminatorPropertyName = "type")]
[YamlDerivedType(typeof(IntValue), "int")]
[YamlDerivedType(typeof(StrValue), "str")]
public abstract class PolymorphicValue
{
    public string? Description { get; set; }
}

/// <summary>
/// Concrete implementation with an integer value.
/// </summary>
public class IntValue : PolymorphicValue
{
    public int Number { get; set; }
}

/// <summary>
/// Concrete implementation with a string value.
/// </summary>
public class StrValue : PolymorphicValue
{
    public string? Text { get; set; }
}

/// <summary>
/// Container with a polymorphic property.
/// </summary>
public class PolymorphicContainer
{
    public string? Name { get; set; }
    public PolymorphicValue? Data { get; set; }
}

/// <summary>
/// Enum for sibling-discriminated polymorphism.
/// </summary>
public enum SiblingValueType
{
    Integer,
    Text,
    Flag
}

/// <summary>
/// Abstract base class for sibling-discriminated polymorphism.
/// The Type property determines which derived type to use.
/// </summary>
public abstract class SiblingValue
{
    public string? Description { get; set; }
}

/// <summary>
/// Concrete implementation with an integer value.
/// </summary>
public class SiblingIntValue : SiblingValue
{
    public int? Number { get; set; }
}

/// <summary>
/// Concrete implementation with a string value.
/// </summary>
public class SiblingStrValue : SiblingValue
{
    public string? Text { get; set; }
}

/// <summary>
/// Concrete implementation with a boolean value.
/// </summary>
public class SiblingBoolValue : SiblingValue
{
    public bool? Flag { get; set; }
}

/// <summary>
/// Container with sibling-discriminated polymorphic property.
/// The Type property determines which derived type Value should be.
/// </summary>
public class SiblingContainer
{
    public string? Name { get; set; }
    public SiblingValueType Type { get; set; }
    
    [YamlSiblingDiscriminator(nameof(Type))]
    [YamlDiscriminatorMapping(nameof(SiblingValueType.Integer), typeof(SiblingIntValue))]
    [YamlDiscriminatorMapping(nameof(SiblingValueType.Text), typeof(SiblingStrValue))]
    [YamlDiscriminatorMapping(nameof(SiblingValueType.Flag), typeof(SiblingBoolValue))]
    public SiblingValue? Value { get; set; }
}

/// <summary>
/// Tests for IgnoreEmptyObjects feature.
/// </summary>
public class IgnoreEmptyObjectsTests
{
    [Fact]
    public void Serialize_WithEmptyNestedObject_OmitsEmptyObject()
    {
        var obj = new ParentWithMetadata
        {
            Name = "Test",
            Metadata = new MetadataClass
            {
                Author = null,
                Description = null,
                Version = null
            },
            Value = 42
        };

        var yaml = YamlSerializer.Serialize(obj, IgnoreEmptyObjectsContext.Default.ParentWithMetadata);

        // The empty metadata object should be omitted
        Assert.Contains("name: Test", yaml);
        Assert.Contains("value: 42", yaml);
        Assert.DoesNotContain("metadata:", yaml);
    }

    [Fact]
    public void Serialize_WithNullNestedObject_OmitsNullObject()
    {
        var obj = new ParentWithMetadata
        {
            Name = "Test",
            Metadata = null,
            Value = 42
        };

        var yaml = YamlSerializer.Serialize(obj, IgnoreEmptyObjectsContext.Default.ParentWithMetadata);

        Assert.Contains("name: Test", yaml);
        Assert.Contains("value: 42", yaml);
        Assert.DoesNotContain("metadata:", yaml);
    }

    [Fact]
    public void Serialize_WithNonEmptyNestedObject_IncludesNestedObject()
    {
        var obj = new ParentWithMetadata
        {
            Name = "Test",
            Metadata = new MetadataClass
            {
                Author = "John Doe",
                Description = null,
                Version = null
            },
            Value = 42
        };

        var yaml = YamlSerializer.Serialize(obj, IgnoreEmptyObjectsContext.Default.ParentWithMetadata);

        // The metadata object should be included because Author is not null
        Assert.Contains("name: Test", yaml);
        Assert.Contains("metadata:", yaml);
        Assert.Contains("author: John Doe", yaml);
        Assert.DoesNotContain("description:", yaml); // Null properties still omitted
        Assert.DoesNotContain("version:", yaml);
    }

    [Fact]
    public void Serialize_MultipleNestedObjects_OmitsOnlyEmptyOnes()
    {
        var obj = new ContainerWithMultipleNested
        {
            Id = "container-1",
            Metadata = new MetadataClass
            {
                Author = "Jane",
                Description = null,
                Version = null
            },
            ExtraMetadata = new MetadataClass
            {
                Author = null,
                Description = null,
                Version = null
            },
            Child = new SimpleClass
            {
                Name = "ChildName", // Non-null to make it non-empty
                Value = 100,
                IsActive = true
            }
        };

        var yaml = YamlSerializer.Serialize(obj, IgnoreEmptyObjectsContext.Default.ContainerWithMultipleNested);

        Assert.Contains("id: container-1", yaml);
        Assert.Contains("metadata:", yaml);
        Assert.Contains("author: Jane", yaml);
        Assert.DoesNotContain("extra-metadata:", yaml); // Empty, should be omitted
        Assert.Contains("child:", yaml);
        Assert.Contains("name: ChildName", yaml);
        Assert.Contains("value: 100", yaml);
    }

    [Fact]
    public void Serialize_ObjectWithOnlyNullablePropsNull_IsConsideredEmpty()
    {
        // SimpleClass has Name (nullable), Value (int), IsActive (bool)
        // IsEmpty only checks nullable properties, so if Name is null, it's considered empty
        var obj = new ContainerWithMultipleNested
        {
            Id = "container-1",
            Metadata = null,
            ExtraMetadata = null,
            Child = new SimpleClass
            {
                Name = null, // Only nullable property is null
                Value = 100,
                IsActive = true
            }
        };

        var yaml = YamlSerializer.Serialize(obj, IgnoreEmptyObjectsContext.Default.ContainerWithMultipleNested);

        Assert.Contains("id: container-1", yaml);
        // Child is considered empty because Name (the only nullable property) is null
        Assert.DoesNotContain("child:", yaml);
    }

    [Fact]
    public void Serialize_DeeplyNestedEmptyObjects_OmitsInnerEmpty()
    {
        // MiddleLevel has Title (null) and Inner (InnerLevel object)
        // Inner is considered empty because Value and Extra are null
        // But MiddleLevel itself is NOT considered empty because Inner is not null
        // (even though Inner is semantically empty)
        var obj = new OuterLevel
        {
            Name = "Outer",
            Middle = new MiddleLevel
            {
                Title = null,
                Inner = new InnerLevel
                {
                    Value = null,
                    Extra = null
                }
            }
        };

        var yaml = YamlSerializer.Serialize(obj, IgnoreEmptyObjectsContext.Default.OuterLevel);

        Assert.Contains("name: Outer", yaml);
        // Middle is present because Inner property is not null (even if Inner is empty)
        Assert.Contains("middle:", yaml);
        // Inner is omitted because it's empty (all nullable props are null)
        Assert.DoesNotContain("inner:", yaml);
        Assert.DoesNotContain("title:", yaml);
    }

    [Fact]
    public void Serialize_DeeplyNestedWithOneNonEmpty_IncludesPath()
    {
        var obj = new OuterLevel
        {
            Name = "Outer",
            Middle = new MiddleLevel
            {
                Title = "Middle Title",
                Inner = new InnerLevel
                {
                    Value = null,
                    Extra = null
                }
            }
        };

        var yaml = YamlSerializer.Serialize(obj, IgnoreEmptyObjectsContext.Default.OuterLevel);

        Assert.Contains("name: Outer", yaml);
        Assert.Contains("middle:", yaml);
        Assert.Contains("title: Middle Title", yaml);
        // Inner is empty, should be omitted
        Assert.DoesNotContain("inner:", yaml);
    }

    [Fact]
    public void Serialize_WithoutIgnoreEmptyObjects_IncludesEmptyObjects()
    {
        var obj = new ParentWithMetadata
        {
            Name = "Test",
            Metadata = new MetadataClass
            {
                Author = null,
                Description = null,
                Version = null
            },
            Value = 42
        };

        // Use the regular context without IgnoreEmptyObjects
        var options = new YamlSerializerOptions { IgnoreNullValues = true };
        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.ParentWithMetadata, options);

        // The empty metadata object should still be present (just with no content)
        Assert.Contains("name: Test", yaml);
        Assert.Contains("metadata:", yaml);
        Assert.Contains("value: 42", yaml);
    }

    [Fact]
    public void RoundTrip_EmptyNestedObjectOmitted_DeserializesToNull()
    {
        var original = new ParentWithMetadata
        {
            Name = "Test",
            Metadata = new MetadataClass
            {
                Author = null,
                Description = null,
                Version = null
            },
            Value = 42
        };

        var yaml = YamlSerializer.Serialize(original, IgnoreEmptyObjectsContext.Default.ParentWithMetadata);
        var result = YamlSerializer.Deserialize(yaml, IgnoreEmptyObjectsContext.Default.ParentWithMetadata);

        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
        // Metadata was omitted during serialization, so it deserializes to null
        Assert.Null(result.Metadata);
    }

    [Fact]
    public void Serialize_PolymorphicProperty_NotSkippedEvenIfBaseClassLooksEmpty()
    {
        // This tests that polymorphic base type properties are NOT incorrectly skipped
        // by IgnoreEmptyObjects. The base class (PolymorphicValue) only has Description,
        // but the derived class (IntValue) has Number. If we only checked the base class's
        // properties for emptiness, we would incorrectly skip this.
        var obj = new PolymorphicContainer
        {
            Name = "Container",
            Data = new IntValue
            {
                Description = null, // Base class property is null
                Number = 42         // Derived class property has a value
            }
        };

        var yaml = YamlSerializer.Serialize(obj, IgnoreEmptyObjectsContext.Default.PolymorphicContainer);

        Assert.Contains("name: Container", yaml);
        // The data property should be included because the derived type has non-null Number
        Assert.Contains("data:", yaml);
        Assert.Contains("number: 42", yaml);
    }

    [Fact]
    public void Serialize_PolymorphicPropertyWithStringValue_NotSkipped()
    {
        var obj = new PolymorphicContainer
        {
            Name = "Container",
            Data = new StrValue
            {
                Description = null,
                Text = "hello"
            }
        };

        var yaml = YamlSerializer.Serialize(obj, IgnoreEmptyObjectsContext.Default.PolymorphicContainer);

        Assert.Contains("name: Container", yaml);
        Assert.Contains("data:", yaml);
        Assert.Contains("text: hello", yaml);
    }

    [Fact]
    public void Serialize_PolymorphicPropertyNull_IsSkipped()
    {
        var obj = new PolymorphicContainer
        {
            Name = "Container",
            Data = null
        };

        var yaml = YamlSerializer.Serialize(obj, IgnoreEmptyObjectsContext.Default.PolymorphicContainer);

        Assert.Contains("name: Container", yaml);
        // Null should still be skipped (this is IgnoreNullValues, not related to IgnoreEmptyObjects)
        Assert.DoesNotContain("data:", yaml);
    }

    [Fact]
    public void Serialize_SiblingDiscriminatedProperty_WithValue_NotSkipped()
    {
        // This tests that sibling-discriminated polymorphic properties are NOT incorrectly skipped
        // when the derived type has a value. The base class (SiblingValue) only has Description,
        // but the derived class (SiblingIntValue) has Number.
        var obj = new SiblingContainer
        {
            Name = "Container",
            Type = SiblingValueType.Integer,
            Value = new SiblingIntValue
            {
                Description = null, // Base class property is null
                Number = 8080       // Derived class property has a value
            }
        };

        var yaml = YamlSerializer.Serialize(obj, IgnoreEmptyObjectsContext.Default.SiblingContainer);

        Assert.Contains("name: Container", yaml);
        Assert.Contains("type: Integer", yaml);
        // The value property should be included because the derived type has Number = 8080
        Assert.Contains("value:", yaml);
        Assert.Contains("number: 8080", yaml);
    }

    [Fact]
    public void Serialize_SiblingDiscriminatedProperty_WithStringValue_NotSkipped()
    {
        var obj = new SiblingContainer
        {
            Name = "Container",
            Type = SiblingValueType.Text,
            Value = new SiblingStrValue
            {
                Description = null,
                Text = "hello world"
            }
        };

        var yaml = YamlSerializer.Serialize(obj, IgnoreEmptyObjectsContext.Default.SiblingContainer);

        Assert.Contains("name: Container", yaml);
        Assert.Contains("type: Text", yaml);
        Assert.Contains("value:", yaml);
        Assert.Contains("text: hello world", yaml);
    }

    [Fact]
    public void Serialize_SiblingDiscriminatedProperty_WithBoolValue_NotSkipped()
    {
        var obj = new SiblingContainer
        {
            Name = "Container",
            Type = SiblingValueType.Flag,
            Value = new SiblingBoolValue
            {
                Description = null,
                Flag = true
            }
        };

        var yaml = YamlSerializer.Serialize(obj, IgnoreEmptyObjectsContext.Default.SiblingContainer);

        Assert.Contains("name: Container", yaml);
        Assert.Contains("type: Flag", yaml);
        Assert.Contains("value:", yaml);
        Assert.Contains("flag: true", yaml);
    }

    [Fact]
    public void Serialize_SiblingDiscriminatedProperty_Empty_IsSkipped()
    {
        // When the derived type has all nullable properties as null, it should be skipped
        var obj = new SiblingContainer
        {
            Name = "Container",
            Type = SiblingValueType.Integer,
            Value = new SiblingIntValue
            {
                Description = null,
                Number = null  // All nullable properties are null - should be skipped
            }
        };

        var yaml = YamlSerializer.Serialize(obj, IgnoreEmptyObjectsContext.Default.SiblingContainer);

        Assert.Contains("name: Container", yaml);
        Assert.Contains("type: Integer", yaml);
        // The value property should be omitted because the derived type is empty
        Assert.DoesNotContain("value:", yaml);
    }

    [Fact]
    public void Serialize_SiblingDiscriminatedProperty_Null_IsSkipped()
    {
        var obj = new SiblingContainer
        {
            Name = "Container",
            Type = SiblingValueType.Integer,
            Value = null
        };

        var yaml = YamlSerializer.Serialize(obj, IgnoreEmptyObjectsContext.Default.SiblingContainer);

        Assert.Contains("name: Container", yaml);
        Assert.Contains("type: Integer", yaml);
        // Null should be skipped
        Assert.DoesNotContain("value:", yaml);
    }

    [Fact]
    public void Serialize_SiblingDiscriminatedProperty_WithDescription_NotSkipped()
    {
        // Even if the derived-specific property is null, if the base class property has a value,
        // the object should not be skipped
        var obj = new SiblingContainer
        {
            Name = "Container",
            Type = SiblingValueType.Integer,
            Value = new SiblingIntValue
            {
                Description = "This is a port number",
                Number = null
            }
        };

        var yaml = YamlSerializer.Serialize(obj, IgnoreEmptyObjectsContext.Default.SiblingContainer);

        Assert.Contains("name: Container", yaml);
        Assert.Contains("type: Integer", yaml);
        Assert.Contains("value:", yaml);
        Assert.Contains("description: This is a port number", yaml);
    }
}

/// <summary>
/// Serializer context with IgnoreNullValues and IgnoreEmptyObjects enabled.
/// </summary>
[YamlSerializable(typeof(ParentWithMetadata))]
[YamlSerializable(typeof(MetadataClass))]
[YamlSerializable(typeof(ContainerWithMultipleNested))]
[YamlSerializable(typeof(SimpleClass))]
[YamlSerializable(typeof(OuterLevel))]
[YamlSerializable(typeof(MiddleLevel))]
[YamlSerializable(typeof(InnerLevel))]
[YamlSerializable(typeof(PolymorphicContainer))]
[YamlSerializable(typeof(PolymorphicValue))]
[YamlSerializable(typeof(IntValue))]
[YamlSerializable(typeof(StrValue))]
[YamlSerializable(typeof(SiblingContainer))]
[YamlSerializable(typeof(SiblingValueType))]
[YamlSerializable(typeof(SiblingValue))]
[YamlSerializable(typeof(SiblingIntValue))]
[YamlSerializable(typeof(SiblingStrValue))]
[YamlSerializable(typeof(SiblingBoolValue))]
[YamlSourceGenerationOptions(
    IgnoreNullValues = true,
    IgnoreEmptyObjects = true)]
public partial class IgnoreEmptyObjectsContext : YamlSerializerContext
{
}
