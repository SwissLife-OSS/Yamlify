namespace Yamlify.Serialization;

/// <summary>
/// Instructs the source generator to generate serialization metadata for the specified type.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to a partial class that derives from <see cref="YamlSerializerContext"/>
/// to generate optimized serialization code for the specified types.
/// </para>
/// <para>
/// For polymorphic types, you can configure polymorphism either on the type itself using
/// <see cref="YamlPolymorphicAttribute"/> and <see cref="YamlDerivedTypeAttribute"/>, or directly
/// on this attribute using <see cref="TypeDiscriminatorPropertyName"/> and <see cref="DerivedTypes"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Option 1: Polymorphism configured on the context
/// [YamlSerializable(typeof(Animal), 
///     TypeDiscriminatorPropertyName = "type",
///     DerivedTypes = new[] { typeof(Dog), typeof(Cat) },
///     DerivedTypeDiscriminators = new[] { "dog", "cat" })]
/// public partial class MyContext : YamlSerializerContext { }
/// 
/// // Option 2: Polymorphism configured on the type (original approach)
/// [YamlPolymorphic(TypeDiscriminatorPropertyName = "type")]
/// [YamlDerivedType(typeof(Dog), "dog")]
/// [YamlDerivedType(typeof(Cat), "cat")]
/// public abstract class Animal { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class YamlSerializableAttribute : Attribute
{
    /// <summary>
    /// Gets the type for which to generate serialization metadata.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets or sets the property name to use for polymorphic type discrimination.
    /// When set along with <see cref="DerivedTypes"/>, enables polymorphic serialization.
    /// </summary>
    /// <remarks>
    /// This is an alternative to using <see cref="YamlPolymorphicAttribute"/> on the type itself.
    /// If both are specified, the attribute on the context takes precedence.
    /// </remarks>
    public string? TypeDiscriminatorPropertyName { get; set; }

    /// <summary>
    /// Gets or sets the derived types for polymorphic serialization.
    /// Must be used together with <see cref="TypeDiscriminatorPropertyName"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The discriminator values for each derived type can be specified in <see cref="DerivedTypeDiscriminators"/>.
    /// If <see cref="DerivedTypeDiscriminators"/> is not specified or has fewer elements,
    /// the type name is used as the discriminator value.
    /// </para>
    /// <para>
    /// This is an alternative to using <see cref="YamlDerivedTypeAttribute"/> on the type itself.
    /// If both are specified, the attribute on the context takes precedence.
    /// </para>
    /// </remarks>
    public Type[]? DerivedTypes { get; set; }

    /// <summary>
    /// Gets or sets the discriminator values corresponding to each type in <see cref="DerivedTypes"/>.
    /// </summary>
    /// <remarks>
    /// This array must have the same length as <see cref="DerivedTypes"/> or be null/empty.
    /// If null or empty, type names are used as discriminator values.
    /// </remarks>
    public string[]? DerivedTypeDiscriminators { get; set; }

    /// <summary>
    /// Gets or sets whether to generate a converter for this type.
    /// </summary>
    public bool GenerateConverter { get; set; } = true;

    /// <summary>
    /// Gets or sets the property ordering strategy for this specific type.
    /// Default is <see cref="YamlPropertyOrdering.Inherit"/> which uses the context-level setting.
    /// </summary>
    public YamlPropertyOrdering PropertyOrdering { get; set; } = YamlPropertyOrdering.Inherit;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSerializableAttribute"/> class.
    /// </summary>
    /// <param name="type">The type for which to generate serialization metadata.</param>
    public YamlSerializableAttribute(Type type)
    {
        Type = type;
    }
}

/// <summary>
/// Specifies the source generation mode for the YAML serializer.
/// </summary>
public enum YamlSourceGenerationMode
{
    /// <summary>
    /// Default mode - generates both metadata and serialization logic.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Generates only metadata (type info, property info).
    /// </summary>
    Metadata = 1,

    /// <summary>
    /// Generates full serialization logic optimized for AOT.
    /// </summary>
    Serialization = 2
}

/// <summary>
/// Configures source generation options for a <see cref="YamlSerializerContext"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class YamlSourceGenerationOptionsAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the property naming policy.
    /// </summary>
    public YamlKnownNamingPolicy PropertyNamingPolicy { get; set; } = YamlKnownNamingPolicy.KebabCase;

    /// <summary>
    /// Gets or sets the source generation mode.
    /// </summary>
    public YamlSourceGenerationMode GenerationMode { get; set; } = YamlSourceGenerationMode.Default;

    /// <summary>
    /// Gets or sets the property ordering strategy.
    /// </summary>
    /// <remarks>
    /// When set to <see cref="YamlPropertyOrdering.Alphabetical"/>, properties will be serialized
    /// in alphabetical order by their serialized name. Using <see cref="YamlPropertyOrderAttribute"/>
    /// on any property when this is set to Alphabetical will cause a compile-time error.
    /// </remarks>
    public YamlPropertyOrdering PropertyOrdering { get; set; } = YamlPropertyOrdering.DeclarationOrder;

    /// <summary>
    /// Gets or sets whether to ignore null values during serialization.
    /// </summary>
    public bool IgnoreNullValues { get; set; }

    /// <summary>
    /// Gets or sets whether to ignore objects that would serialize to an empty mapping.
    /// </summary>
    /// <remarks>
    /// When true, if an object has no non-null properties (after applying IgnoreNullValues),
    /// the entire property is omitted from the output.
    /// </remarks>
    public bool IgnoreEmptyObjects { get; set; }

    /// <summary>
    /// Gets or sets whether to ignore read-only properties.
    /// </summary>
    public bool IgnoreReadOnlyProperties { get; set; }

    /// <summary>
    /// Gets or sets whether to include fields.
    /// </summary>
    public bool IncludeFields { get; set; }

    /// <summary>
    /// Gets or sets whether to write indented YAML.
    /// </summary>
    public bool WriteIndented { get; set; } = true;

    /// <summary>
    /// Gets or sets the default indent size.
    /// </summary>
    public int IndentSize { get; set; } = 2;

    /// <summary>
    /// Gets or sets whether sequence items should be indented relative to their parent key.
    /// When true (default), sequence items are indented:
    /// <code>
    /// resources:
    ///   - name: foo
    /// </code>
    /// When false, sequence items are at the same level as the parent key (compact style):
    /// <code>
    /// resources:
    /// - name: foo
    /// </code>
    /// </summary>
    public bool IndentSequenceItems { get; set; } = true;

    /// <summary>
    /// Gets or sets the default scalar style.
    /// </summary>
    public Core.ScalarStyle DefaultScalarStyle { get; set; } = Core.ScalarStyle.Any;

    /// <summary>
    /// Gets or sets the position of type discriminator properties during serialization.
    /// </summary>
    /// <remarks>
    /// When set to <see cref="DiscriminatorPosition.Ordered"/> (default), discriminator properties
    /// are written according to their <see cref="YamlPropertyOrderAttribute"/> or declaration order.
    /// When set to <see cref="DiscriminatorPosition.First"/>, discriminator properties are always
    /// written first, regardless of their order attribute.
    /// </remarks>
    public DiscriminatorPosition DiscriminatorPosition { get; set; } = DiscriminatorPosition.Ordered;
}

/// <summary>
/// Specifies a known naming policy for source generation.
/// </summary>
public enum YamlKnownNamingPolicy
{
    /// <summary>
    /// No naming policy - use property names as-is.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Convert to camelCase.
    /// </summary>
    CamelCase = 1,

    /// <summary>
    /// Convert to snake_case.
    /// </summary>
    SnakeCase = 2,

    /// <summary>
    /// Convert to kebab-case.
    /// </summary>
    KebabCase = 3
}

/// <summary>
/// Specifies where the type discriminator property should be written during serialization.
/// </summary>
public enum DiscriminatorPosition
{
    /// <summary>
    /// The discriminator property is written according to its <see cref="YamlPropertyOrderAttribute"/>
    /// or declaration order, just like any other property. This is the default.
    /// </summary>
    Ordered = 0,

    /// <summary>
    /// The discriminator property is always written first, regardless of <see cref="YamlPropertyOrderAttribute"/>.
    /// This can be useful for human readability when viewing YAML files.
    /// </summary>
    First = 1
}

/// <summary>
/// Specifies the ordering strategy for properties during serialization.
/// </summary>
public enum YamlPropertyOrdering
{
    /// <summary>
    /// Inherit the ordering from the context-level setting.
    /// This is the default when used on [YamlSerializable] attribute.
    /// </summary>
    Inherit = -1,

    /// <summary>
    /// Properties are ordered by their declaration order in the source code.
    /// This is the default behavior and provides stable, predictable output.
    /// </summary>
    DeclarationOrder = 0,

    /// <summary>
    /// Properties are ordered alphabetically by their serialized name (after naming policy is applied).
    /// </summary>
    /// <remarks>
    /// When this option is used, any <see cref="YamlPropertyOrderAttribute"/> on properties
    /// will cause a compile-time error, as explicit ordering conflicts with alphabetical ordering.
    /// </remarks>
    Alphabetical = 1,

    /// <summary>
    /// Properties with <see cref="YamlPropertyOrderAttribute"/> come first (sorted by order value),
    /// followed by remaining properties sorted alphabetically by their serialized name.
    /// </summary>
    /// <remarks>
    /// This allows you to pin specific important properties at the top (e.g., identifiers),
    /// while all other properties are consistently ordered alphabetically.
    /// </remarks>
    OrderedThenAlphabetical = 2
}
