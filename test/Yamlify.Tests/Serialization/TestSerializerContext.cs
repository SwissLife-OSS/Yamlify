using Yamlify;
using Yamlify.Serialization;
using Yamlify.Serialization.Converters;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Source-generated serialization context for all test types.
/// </summary>
/// <remarks>
/// Types are registered via [YamlSerializable] attributes.
/// The source generator produces converters for these types.
/// Note: Records with init-only properties and complex types are not yet supported.
/// </remarks>
// Basic types
[YamlSerializable(typeof(SimpleClass))]
[YamlSerializable(typeof(ParentClass))]
[YamlSerializable(typeof(Level1))]
[YamlSerializable(typeof(Level2))]
[YamlSerializable(typeof(Level3))]
// Primitives
[YamlSerializable(typeof(AllPrimitivesClass))]
[YamlSerializable(typeof(SpecialNumbersClass))]
// Enums
[YamlSerializable(typeof(EnumClass))]
// DateTime and related types
[YamlSerializable(typeof(DateTimeClass))]
[YamlSerializable(typeof(GuidClass))]
[YamlSerializable(typeof(TimeSpanClass))]
[YamlSerializable(typeof(UriClass))]
// Nullable types
[YamlSerializable(typeof(NullableTypesClass))]
// Primary constructors
[YamlSerializable(typeof(PrimaryConstructorClass))]
[YamlSerializable(typeof(PrimaryConstructorWithExtraProperty))]
[YamlSerializable(typeof(PrimaryConstructorWithDefaults))]
[YamlSerializable(typeof(OuterPrimaryClass))]
[YamlSerializable(typeof(InnerPrimaryClass))]
// Collections
[YamlSerializable(typeof(CollectionsClass))]
// Records
[YamlSerializable(typeof(PersonRecord))]
[YamlSerializable(typeof(AddressRecord))]
[YamlSerializable(typeof(PointRecord))]
// Structs
[YamlSerializable(typeof(SimpleStruct))]
[YamlSerializable(typeof(ImmutablePoint))]
// Positional records
[YamlSerializable(typeof(PositionalRecord))]
[YamlSerializable(typeof(RecordWithDefaults))]
// Inheritance types
[YamlSerializable(typeof(Dog))]
[YamlSerializable(typeof(Cat))]
[YamlSerializable(typeof(Car))]
[YamlSerializable(typeof(Motorcycle))]
// Nested collections
[YamlSerializable(typeof(NestedCollectionsClass))]
// Attribute tests
[YamlSerializable(typeof(ClassWithAttributedProperties))]
[YamlSerializable(typeof(ClassWithPropertyOrder))]
// Additional numeric types
[YamlSerializable(typeof(AllNumericTypesClass))]
// DateOnly and TimeOnly types
[YamlSerializable(typeof(DateOnlyClass))]
[YamlSerializable(typeof(TimeOnlyClass))]
// ReadOnly collections
[YamlSerializable(typeof(ReadOnlyCollectionsClass))]
// Edge cases
[YamlSerializable(typeof(MixedTypesClass))]
// RecursionDepth test types
[YamlSerializable(typeof(RecursionDepthTests.RecursiveNode))]
[YamlSerializable(typeof(RecursionDepthTests.NestedContainer))]
[YamlSerializable(typeof(RecursionDepthTests.SimpleModel))]
// EmptyCollectionHandling test types
[YamlSerializable(typeof(EmptyCollectionHandlingTests.ModelWithArrays))]
[YamlSerializable(typeof(EmptyCollectionHandlingTests.ModelWithLists))]
[YamlSerializable(typeof(EmptyCollectionHandlingTests.ModelWithNestedArrays))]
[YamlSerializable(typeof(EmptyCollectionHandlingTests.NestedModel))]
[YamlSerializable(typeof(EmptyCollectionHandlingTests.RecordWithArrays))]
[YamlSerializable(typeof(EmptyCollectionHandlingTests.CollectionSimpleModel))]
// Circular reference test types
[YamlSerializable(typeof(CircularReferenceClass))]
// Enum types registered directly (scalar serialization)
[YamlSerializable(typeof(Status))]
[YamlSerializable(typeof(Priority))]
// Dictionary with enum keys
[YamlSerializable(typeof(DictionaryWithEnumKeyClass))]
// Dictionary with array values
[YamlSerializable(typeof(DictionaryWithArrayValueClass))]
[YamlSerializable(typeof(ItemInfo))]
// Enum collections
[YamlSerializable(typeof(EnumCollectionsClass))]
// Type name collision test types
[YamlSerializable(typeof(TypeCollision.NamespaceA.Config))]
[YamlSerializable(typeof(TypeCollision.NamespaceB.Config))]
// Root-level collection types
[YamlSerializable(typeof(List<SimpleClass>))]
[YamlSerializable(typeof(Dictionary<string, SimpleClass>))]
// Default value preservation test types
[YamlSerializable(typeof(ClassWithPropertyDefaults))]
[YamlSerializable(typeof(ClassWithBooleanDefaults))]
[YamlSerializable(typeof(ConfigWithDefaults))]
[YamlSerializable(typeof(ContainerWithNestedDefaults))]
// Writer formatting test types
[YamlSerializable(typeof(DeclarationOrderClass))]
[YamlSerializable(typeof(DeeplyNestedClass))]
// Property ordering test types
[YamlSerializable(typeof(UnorderedClass))]
[YamlSerializable(typeof(InheritedOrderingDerived))]
// IgnoreEmptyObjects comparison types
[YamlSerializable(typeof(ParentWithMetadata))]
[YamlSerializable(typeof(MetadataClass))]
public partial class TestSerializerContext : YamlSerializerContext
{
    // Source generator will generate type info properties
}

/// <summary>
/// Test class with YamlPropertyName and YamlIgnore attributes.
/// </summary>
public class ClassWithAttributedProperties
{
    [YamlPropertyName("custom-name")]
    public string? CustomProperty { get; set; }
    
    [YamlIgnore]
    public string? IgnoredProperty { get; set; }
    
    public string? RegularProperty { get; set; }
}

/// <summary>
/// Test class with YamlPropertyOrder attribute to control serialization order.
/// </summary>
public class ClassWithPropertyOrder
{
    [YamlPropertyOrder(3)]
    public string? Third { get; set; }
    
    [YamlPropertyOrder(1)]
    public string? First { get; set; }
    
    [YamlPropertyOrder(2)]
    public string? Second { get; set; }
    
    // No order attribute - should come last
    public string? Unordered { get; set; }
}

/// <summary>
/// Test class with all additional numeric types.
/// </summary>
public class AllNumericTypesClass
{
    public byte ByteValue { get; set; }
    public sbyte SByteValue { get; set; }
    public short ShortValue { get; set; }
    public ushort UShortValue { get; set; }
    public uint UIntValue { get; set; }
    public ulong ULongValue { get; set; }
    public char CharValue { get; set; }
}

/// <summary>
/// Test class for DateOnly serialization.
/// </summary>
public class DateOnlyClass
{
    public DateOnly Date { get; set; }
    public DateOnly? NullableDate { get; set; }
}

/// <summary>
/// Test class for TimeOnly serialization.
/// </summary>
public class TimeOnlyClass
{
    public TimeOnly Time { get; set; }
    public TimeOnly? NullableTime { get; set; }
}

/// <summary>
/// Test class for IReadOnlyDictionary and IReadOnlyList.
/// </summary>
public class ReadOnlyCollectionsClass
{
    public IReadOnlyDictionary<string, int>? ReadOnlyDict { get; set; }
    public IReadOnlyList<string>? ReadOnlyList { get; set; }
    public IReadOnlyDictionary<string, SimpleClass>? ObjectDict { get; set; }
}

/// <summary>
/// Test class combining many different types in one object.
/// </summary>
public class MixedTypesClass
{
    public string? Name { get; set; }
    public int Count { get; set; }
    public double? Ratio { get; set; }
    public DateTime Created { get; set; }
    public Guid Id { get; set; }
    public List<string>? Tags { get; set; }
    public Dictionary<string, int>? Scores { get; set; }
    public SimpleClass? Nested { get; set; }
}

/// <summary>
/// Priority enum for testing standalone enum registration.
/// </summary>
public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Test class with dictionary using enum keys.
/// </summary>
public class DictionaryWithEnumKeyClass
{
    public Dictionary<Priority, string>? PriorityLabels { get; set; }
    public Dictionary<Status, int>? StatusCounts { get; set; }
}

/// <summary>
/// Simple item info for dictionary value tests.
/// </summary>
public class ItemInfo
{
    public string? Name { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Test class with dictionary using array values.
/// </summary>
public class DictionaryWithArrayValueClass
{
    public Dictionary<string, string[]>? TagsByCategory { get; set; }
    public Dictionary<Priority, ItemInfo[]>? ItemsByPriority { get; set; }
}

/// <summary>
/// Test class with enum collections (arrays and lists).
/// </summary>
public class EnumCollectionsClass
{
    public Status[]? StatusArray { get; set; }
    public List<Priority>? PriorityList { get; set; }
}

// Type name collision test types are in TypeCollisionTestTypes.cs
