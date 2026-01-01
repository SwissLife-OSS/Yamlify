namespace Yamlify.Serialization;

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
