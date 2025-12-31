using Yamlify.Core;
using Yamlify.Schema;

namespace Yamlify.Serialization;

/// <summary>
/// Base class for converting objects to and from YAML.
/// </summary>
public abstract class YamlConverter
{
    /// <summary>
    /// Gets the type this converter handles.
    /// </summary>
    public abstract Type Type { get; }

    /// <summary>
    /// Determines whether this converter can convert the specified type.
    /// </summary>
    public abstract bool CanConvert(Type typeToConvert);
}

/// <summary>
/// Converts a type to and from YAML.
/// </summary>
/// <typeparam name="T">The type to convert.</typeparam>
public abstract class YamlConverter<T> : YamlConverter
{
    /// <inheritdoc/>
    public sealed override Type Type => typeof(T);

    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => typeof(T).IsAssignableFrom(typeToConvert);

    /// <summary>
    /// Reads and converts the YAML to type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The converted value.</returns>
    public abstract T? Read(ref Utf8YamlReader reader, YamlSerializerOptions options);

    /// <summary>
    /// Writes a value as YAML.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to convert.</param>
    /// <param name="options">The serializer options.</param>
    public abstract void Write(Utf8YamlWriter writer, T value, YamlSerializerOptions options);
}

/// <summary>
/// Factory for creating converters dynamically.
/// </summary>
public abstract class YamlConverterFactory : YamlConverter
{
    /// <inheritdoc/>
    public sealed override Type Type => typeof(object);

    /// <summary>
    /// Creates a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The type to create a converter for.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>A converter for the specified type.</returns>
    public abstract YamlConverter? CreateConverter(Type typeToConvert, YamlSerializerOptions options);
}

/// <summary>
/// Marks a class as using a specific converter.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class YamlConverterAttribute : Attribute
{
    /// <summary>
    /// Gets the converter type.
    /// </summary>
    public Type ConverterType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlConverterAttribute"/> class.
    /// </summary>
    /// <param name="converterType">The type of the converter.</param>
    public YamlConverterAttribute(Type converterType)
    {
        ConverterType = converterType;
    }
}

/// <summary>
/// Specifies a property name to use during serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class YamlPropertyNameAttribute : Attribute
{
    /// <summary>
    /// Gets the property name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlPropertyNameAttribute"/> class.
    /// </summary>
    /// <param name="name">The property name to use in YAML.</param>
    public YamlPropertyNameAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Ignores a property during serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class YamlIgnoreAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the condition under which the property is ignored.
    /// </summary>
    public YamlIgnoreCondition Condition { get; set; } = YamlIgnoreCondition.Always;
}

/// <summary>
/// Specifies when a property should be ignored.
/// </summary>
public enum YamlIgnoreCondition
{
    /// <summary>Always ignore the property.</summary>
    Always,
    /// <summary>Ignore the property only if it's null.</summary>
    WhenWritingNull,
    /// <summary>Ignore the property only if it's the default value.</summary>
    WhenWritingDefault,
    /// <summary>Never ignore the property.</summary>
    Never
}

/// <summary>
/// Marks a property as required during deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class YamlRequiredAttribute : Attribute
{
}

/// <summary>
/// Specifies the order of properties during serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class YamlPropertyOrderAttribute : Attribute
{
    /// <summary>
    /// Gets the order.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlPropertyOrderAttribute"/> class.
    /// </summary>
    /// <param name="order">The property order.</param>
    public YamlPropertyOrderAttribute(int order)
    {
        Order = order;
    }
}

/// <summary>
/// Specifies constructor parameters for deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor)]
public sealed class YamlConstructorAttribute : Attribute
{
}

/// <summary>
/// Specifies polymorphic type information.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public sealed class YamlDerivedTypeAttribute : Attribute
{
    /// <summary>
    /// Gets the derived type.
    /// </summary>
    public Type DerivedType { get; }

    /// <summary>
    /// Gets the type discriminator.
    /// </summary>
    public string? TypeDiscriminator { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlDerivedTypeAttribute"/> class.
    /// </summary>
    /// <param name="derivedType">The derived type.</param>
    /// <param name="typeDiscriminator">The type discriminator value.</param>
    public YamlDerivedTypeAttribute(Type derivedType, string? typeDiscriminator = null)
    {
        DerivedType = derivedType;
        TypeDiscriminator = typeDiscriminator;
    }
}

/// <summary>
/// Specifies the property name used as a type discriminator for polymorphic serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class YamlPolymorphicAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the type discriminator property name.
    /// </summary>
    public string TypeDiscriminatorPropertyName { get; set; } = "$type";
}

/// <summary>
/// Specifies that the property type should be determined by a sibling property's value.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is used when a property has an abstract or interface type and
/// the concrete type should be determined by another property's value at the same level.
/// </para>
/// <para>
/// For example, given a class with a "Type" enum property and a "Value" abstract property,
/// this attribute can be applied to "Value" to indicate that "Type" determines the concrete type.
/// </para>
/// <example>
/// <code>
/// public class Variable
/// {
///     public VariableValueType Type { get; set; }
///     
///     [YamlSiblingDiscriminator(nameof(Type))]
///     public VariableValue? Value { get; set; }
/// }
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class YamlSiblingDiscriminatorAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the sibling property that contains the type discriminator value.
    /// </summary>
    public string DiscriminatorPropertyName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSiblingDiscriminatorAttribute"/> class.
    /// </summary>
    /// <param name="discriminatorPropertyName">The name of the sibling property whose value determines the concrete type.</param>
    public YamlSiblingDiscriminatorAttribute(string discriminatorPropertyName)
    {
        DiscriminatorPropertyName = discriminatorPropertyName;
    }
}

/// <summary>
/// Maps a discriminator value to a concrete type for polymorphic deserialization.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is used in conjunction with <see cref="YamlSiblingDiscriminatorAttribute"/>
/// to specify which concrete type should be used for each discriminator value.
/// </para>
/// <para>
/// The attribute should be applied to the property with <see cref="YamlSiblingDiscriminatorAttribute"/>,
/// with one <see cref="YamlDiscriminatorMappingAttribute"/> for each possible concrete type.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public sealed class YamlDiscriminatorMappingAttribute : Attribute
{
    /// <summary>
    /// Gets the discriminator value.
    /// </summary>
    public string DiscriminatorValue { get; }

    /// <summary>
    /// Gets the concrete type to use for this discriminator value.
    /// </summary>
    public Type ConcreteType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlDiscriminatorMappingAttribute"/> class.
    /// </summary>
    /// <param name="discriminatorValue">The discriminator value (e.g., enum name).</param>
    /// <param name="concreteType">The concrete type to use for this discriminator value.</param>
    public YamlDiscriminatorMappingAttribute(string discriminatorValue, Type concreteType)
    {
        DiscriminatorValue = discriminatorValue;
        ConcreteType = concreteType;
    }
}
