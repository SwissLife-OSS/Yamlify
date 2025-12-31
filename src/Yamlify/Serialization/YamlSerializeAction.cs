using Yamlify.Core;

namespace Yamlify.Serialization;

/// <summary>
/// Delegate for serializing a value to YAML.
/// </summary>
/// <typeparam name="T">The type to serialize.</typeparam>
public delegate void YamlSerializeAction<in T>(Utf8YamlWriter writer, T value, YamlSerializerOptions options);
