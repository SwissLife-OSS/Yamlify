namespace Yamlify.Serialization;

/// <summary>
/// Generic version of <see cref="YamlSerializableAttribute"/> for a more type-safe API.
/// </summary>
/// <typeparam name="T">The type for which to generate serialization metadata.</typeparam>
/// <example>
/// <code>
/// [YamlSerializable&lt;Person&gt;]
/// [YamlSerializable&lt;Address&gt;]
/// [YamlSerializable&lt;IAnimal&gt;(
///     TypeDiscriminatorPropertyName = "type",
///     DerivedTypes = new[] { typeof(Dog), typeof(Cat) },
///     DerivedTypeDiscriminators = new[] { "dog", "cat" })]
/// public partial class MyContext : YamlSerializerContext { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class YamlSerializableAttribute<T> : YamlSerializableAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSerializableAttribute{T}"/> class.
    /// </summary>
    public YamlSerializableAttribute() : base(typeof(T))
    {
    }
}
