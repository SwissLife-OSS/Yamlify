namespace Yamlify.Serialization;

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
