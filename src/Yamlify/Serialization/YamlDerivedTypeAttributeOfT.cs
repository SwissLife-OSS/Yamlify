namespace Yamlify.Serialization;

/// <summary>
/// Generic version of <see cref="YamlDerivedTypeAttribute"/> for a more type-safe API.
/// </summary>
/// <typeparam name="T">The derived type.</typeparam>
/// <example>
/// <code>
/// [YamlPolymorphic(TypeDiscriminatorPropertyName = "type")]
/// [YamlDerivedType&lt;Dog&gt;("dog")]
/// [YamlDerivedType&lt;Cat&gt;("cat")]
/// public abstract class Animal { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public sealed class YamlDerivedTypeAttribute<T> : YamlDerivedTypeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YamlDerivedTypeAttribute{T}"/> class.
    /// </summary>
    /// <param name="typeDiscriminator">The type discriminator value. If null, the type name is used.</param>
    public YamlDerivedTypeAttribute(string? typeDiscriminator = null) : base(typeof(T), typeDiscriminator)
    {
    }
}
