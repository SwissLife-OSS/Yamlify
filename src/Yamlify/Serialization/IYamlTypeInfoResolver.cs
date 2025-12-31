namespace Yamlify.Serialization;

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
