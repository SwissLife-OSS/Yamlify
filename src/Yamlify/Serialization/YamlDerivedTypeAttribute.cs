namespace Yamlify.Serialization;

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
