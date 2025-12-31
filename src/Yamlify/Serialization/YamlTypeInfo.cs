using Yamlify.Core;

namespace Yamlify.Serialization;

/// <summary>
/// Delegate for serializing a value to YAML.
/// </summary>
/// <typeparam name="T">The type to serialize.</typeparam>
public delegate void YamlSerializeAction<in T>(Utf8YamlWriter writer, T value, YamlSerializerOptions options);

/// <summary>
/// Delegate for deserializing a value from YAML.
/// </summary>
/// <typeparam name="T">The type to deserialize.</typeparam>
public delegate T? YamlDeserializeFunc<T>(ref Utf8YamlReader reader, YamlSerializerOptions options);

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

/// <summary>
/// Provides metadata about a property for YAML serialization.
/// </summary>
public abstract class YamlPropertyInfo
{
    /// <summary>
    /// Gets the name of the property in the CLR type.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the name of the property as it appears in YAML.
    /// </summary>
    public abstract string YamlPropertyName { get; }

    /// <summary>
    /// Gets the type of the property.
    /// </summary>
    public abstract Type PropertyType { get; }

    /// <summary>
    /// Gets whether the property is required.
    /// </summary>
    public abstract bool IsRequired { get; }

    /// <summary>
    /// Gets the order of the property.
    /// </summary>
    public abstract int Order { get; }

    /// <summary>
    /// Gets the ignore condition for this property.
    /// </summary>
    public abstract YamlIgnoreCondition? IgnoreCondition { get; }

    /// <summary>
    /// Gets the getter function.
    /// </summary>
    public abstract Func<object, object?>? Get { get; }

    /// <summary>
    /// Gets the setter action.
    /// </summary>
    public abstract Action<object, object?>? Set { get; }
}

/// <summary>
/// Provides strongly-typed metadata about a property for YAML serialization.
/// </summary>
/// <typeparam name="TDeclaringType">The type that declares the property.</typeparam>
/// <typeparam name="TProperty">The type of the property.</typeparam>
public sealed class YamlPropertyInfo<TDeclaringType, TProperty> : YamlPropertyInfo
{
    /// <inheritdoc/>
    public override string Name { get; }

    /// <inheritdoc/>
    public override string YamlPropertyName { get; }

    /// <inheritdoc/>
    public override Type PropertyType => typeof(TProperty);

    /// <inheritdoc/>
    public override bool IsRequired { get; }

    /// <inheritdoc/>
    public override int Order { get; }

    /// <inheritdoc/>
    public override YamlIgnoreCondition? IgnoreCondition { get; }

    /// <inheritdoc/>
    public override Func<object, object?>? Get { get; }

    /// <inheritdoc/>
    public override Action<object, object?>? Set { get; }

    /// <summary>
    /// Gets the strongly-typed getter function.
    /// </summary>
    public Func<TDeclaringType, TProperty>? Getter { get; }

    /// <summary>
    /// Gets the strongly-typed setter action.
    /// </summary>
    public Action<TDeclaringType, TProperty>? Setter { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlPropertyInfo{TDeclaringType, TProperty}"/> class.
    /// </summary>
    public YamlPropertyInfo(
        string name,
        string yamlPropertyName,
        Func<TDeclaringType, TProperty>? getter,
        Action<TDeclaringType, TProperty>? setter,
        bool isRequired = false,
        int order = 0,
        YamlIgnoreCondition? ignoreCondition = null)
    {
        Name = name;
        YamlPropertyName = yamlPropertyName;
        Getter = getter;
        Setter = setter;
        IsRequired = isRequired;
        Order = order;
        IgnoreCondition = ignoreCondition;

        // Create boxed versions for non-generic access
        if (getter != null)
        {
            Get = obj => getter((TDeclaringType)obj);
        }
        
        if (setter != null)
        {
            Set = (obj, value) => setter((TDeclaringType)obj, (TProperty)value!);
        }
    }
}

/// <summary>
/// Resolves type information for YAML serialization.
/// </summary>
public interface IYamlTypeInfoResolver
{
    /// <summary>
    /// Gets the type info for the specified type.
    /// </summary>
    /// <param name="type">The type to get info for.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The type info, or null if not found.</returns>
    YamlTypeInfo? GetTypeInfo(Type type, YamlSerializerOptions options);
}

/// <summary>
/// Base class for source-generated serialization contexts.
/// </summary>
/// <remarks>
/// <para>
/// Create a partial class that derives from this class and decorate it with 
/// <see cref="YamlSerializableAttribute"/> for each type you want to serialize.
/// </para>
/// <example>
/// <code>
/// [YamlSerializable(typeof(Person))]
/// [YamlSerializable(typeof(Address))]
/// public partial class MySerializerContext : YamlSerializerContext
/// {
/// }
/// </code>
/// </example>
/// </remarks>
public abstract class YamlSerializerContext : IYamlTypeInfoResolver
{
    private readonly Dictionary<Type, YamlTypeInfo> _typeInfoCache = new();
    
    /// <summary>
    /// Gets the options associated with this context.
    /// </summary>
    public YamlSerializerOptions Options { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSerializerContext"/> class.
    /// </summary>
    /// <param name="options">The serializer options.</param>
    protected YamlSerializerContext(YamlSerializerOptions? options = null)
    {
        Options = options ?? YamlSerializerOptions.Default;
    }

    /// <summary>
    /// Gets the type info for the specified type. Implemented by source generator.
    /// </summary>
    /// <param name="type">The type to get info for.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The type info, or null if not found.</returns>
    public abstract YamlTypeInfo? GetTypeInfo(Type type, YamlSerializerOptions options);

    /// <summary>
    /// Gets the strongly-typed type info for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to get info for.</typeparam>
    /// <returns>The type info, or null if not found.</returns>
    public YamlTypeInfo<T>? GetTypeInfo<T>()
    {
        return GetTypeInfo(typeof(T), Options) as YamlTypeInfo<T>;
    }
    
    /// <summary>
    /// Registers a type info in the cache. Used by derived source-generated contexts.
    /// </summary>
    /// <param name="typeInfo">The type info to register.</param>
    protected void RegisterTypeInfo(YamlTypeInfo typeInfo)
    {
        _typeInfoCache[typeInfo.Type] = typeInfo;
    }
    
    /// <summary>
    /// Gets a cached type info by type.
    /// </summary>
    /// <param name="type">The type to get.</param>
    /// <returns>The cached type info, or null.</returns>
    protected YamlTypeInfo? GetCachedTypeInfo(Type type)
    {
        return _typeInfoCache.GetValueOrDefault(type);
    }
}
