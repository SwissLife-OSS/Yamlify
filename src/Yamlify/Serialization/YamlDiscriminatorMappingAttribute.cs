namespace Yamlify.Serialization;

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
