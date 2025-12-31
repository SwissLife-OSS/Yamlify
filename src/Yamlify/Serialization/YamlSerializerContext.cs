namespace Yamlify.Serialization;

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
