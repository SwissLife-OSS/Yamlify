namespace Yamlify.Serialization;

/// <summary>
/// Specifies a polymorphic type mapping between a base type and a derived type for the serializer context.
/// This attribute provides a type-safe way to declare derived type mappings on the context class.
/// </summary>
/// <typeparam name="TBase">The base type or interface for the polymorphic hierarchy.</typeparam>
/// <typeparam name="TDerived">The derived type that implements or extends the base type.</typeparam>
/// <remarks>
/// <para>
/// Use this attribute together with <see cref="YamlSerializableAttribute{T}"/> to configure
/// polymorphic serialization directly on the context class.
/// </para>
/// <para>
/// When using this attribute, you must also register the base type with 
/// <see cref="YamlSerializableAttribute{T}"/> and set its <see cref="YamlSerializableAttribute.TypeDiscriminatorPropertyName"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [YamlSerializable&lt;IAnimal&gt;(TypeDiscriminatorPropertyName = "type")]
/// [YamlDerivedTypeMapping&lt;IAnimal, Dog&gt;("dog")]
/// [YamlDerivedTypeMapping&lt;IAnimal, Cat&gt;("cat")]
/// [YamlSerializable&lt;Dog&gt;]
/// [YamlSerializable&lt;Cat&gt;]
/// public partial class MyContext : YamlSerializerContext { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class YamlDerivedTypeMappingAttribute<TBase, TDerived> : Attribute
    where TDerived : TBase
{
    /// <summary>
    /// Gets the base type for the polymorphic hierarchy.
    /// </summary>
    public Type BaseType => typeof(TBase);

    /// <summary>
    /// Gets the derived type.
    /// </summary>
    public Type DerivedType => typeof(TDerived);

    /// <summary>
    /// Gets the type discriminator value used in YAML to identify this derived type.
    /// </summary>
    public string? TypeDiscriminator { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlDerivedTypeMappingAttribute{TBase, TDerived}"/> class.
    /// </summary>
    /// <param name="typeDiscriminator">
    /// The type discriminator value. If null, the type name of <typeparamref name="TDerived"/> is used.
    /// </param>
    public YamlDerivedTypeMappingAttribute(string? typeDiscriminator = null)
    {
        TypeDiscriminator = typeDiscriminator;
    }
}
