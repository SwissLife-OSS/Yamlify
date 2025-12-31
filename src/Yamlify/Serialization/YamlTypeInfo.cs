using Yamlify.Core;

namespace Yamlify.Serialization;

// The following types have been moved to individual files:
// - YamlSerializeAction.cs
// - YamlDeserializeFunc.cs
// - YamlPropertyInfo.cs
// - YamlPropertyInfoOfT.cs
// - IYamlTypeInfoResolver.cs
// - YamlSerializerContext.cs

/// <summary>
/// Provides metadata about a type for YAML serialization.
/// </summary>
public abstract class YamlTypeInfo
{
    /// <summary>
    /// Gets the type this info represents.
    /// </summary>
    public abstract Type Type { get; }

    /// <summary>
    /// Gets the converter for this type.
    /// </summary>
    public abstract YamlConverter? Converter { get; }

    /// <summary>
    /// Gets the property metadata for this type.
    /// </summary>
    public abstract IReadOnlyList<YamlPropertyInfo> Properties { get; }

    /// <summary>
    /// Gets the serializer options associated with this type info.
    /// </summary>
    public abstract YamlSerializerOptions? Options { get; }
}

/// <summary>
/// Provides strongly-typed metadata about a type for YAML serialization.
/// </summary>
/// <typeparam name="T">The type.</typeparam>
public sealed class YamlTypeInfo<T> : YamlTypeInfo
{
    /// <inheritdoc/>
    public override Type Type => typeof(T);

    /// <inheritdoc/>
    public override YamlConverter? Converter { get; }

    /// <inheritdoc/>
    public override IReadOnlyList<YamlPropertyInfo> Properties { get; }

    /// <inheritdoc/>
    public override YamlSerializerOptions? Options { get; }

    /// <summary>
    /// Gets the strongly-typed factory for creating instances.
    /// </summary>
    public Func<T>? CreateInstance { get; init; }

    /// <summary>
    /// Gets the action to serialize a value of this type.
    /// </summary>
    public YamlSerializeAction<T>? SerializeAction { get; init; }

    /// <summary>
    /// Gets the function to deserialize a value of this type.
    /// </summary>
    public YamlDeserializeFunc<T>? DeserializeFunc { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlTypeInfo{T}"/> class.
    /// </summary>
    /// <param name="options">The serializer options.</param>
    public YamlTypeInfo(YamlSerializerOptions? options = null)
    {
        Options = options;
        Properties = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlTypeInfo{T}"/> class.
    /// </summary>
    /// <param name="converter">The converter for this type.</param>
    /// <param name="options">The serializer options.</param>
    public YamlTypeInfo(YamlConverter<T>? converter, YamlSerializerOptions? options = null)
    {
        Converter = converter;
        Options = options;
        Properties = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlTypeInfo{T}"/> class.
    /// </summary>
    /// <param name="converter">The converter for this type.</param>
    /// <param name="properties">The property metadata.</param>
    /// <param name="options">The serializer options.</param>
    public YamlTypeInfo(
        YamlConverter<T>? converter, 
        IReadOnlyList<YamlPropertyInfo>? properties,
        YamlSerializerOptions? options = null)
    {
        Converter = converter;
        Properties = properties ?? [];
        Options = options;
    }
}
