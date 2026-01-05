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
public class YamlSerializableAttribute : Attribute
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
