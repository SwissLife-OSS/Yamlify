using Yamlify;

namespace Yamlify.Serialization;

/// <summary>
/// Delegate for deserializing a value from YAML.
/// </summary>
/// <typeparam name="T">The type to deserialize.</typeparam>
public delegate T? YamlDeserializeFunc<T>(ref Utf8YamlReader reader, YamlSerializerOptions options);
