using Yamlify.Core;
using Yamlify.Schema;

namespace Yamlify.Serialization;

/// <summary>
/// Base class for converting objects to and from YAML.
/// </summary>
public abstract class YamlConverter
{
    /// <summary>
    /// Gets the type this converter handles.
    /// </summary>
    public abstract Type Type { get; }

    /// <summary>
    /// Determines whether this converter can convert the specified type.
    /// </summary>
    public abstract bool CanConvert(Type typeToConvert);
}

/// <summary>
/// Converts a type to and from YAML.
/// </summary>
/// <typeparam name="T">The type to convert.</typeparam>
public abstract class YamlConverter<T> : YamlConverter
{
    /// <inheritdoc/>
    public sealed override Type Type => typeof(T);

    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => typeof(T).IsAssignableFrom(typeToConvert);

    /// <summary>
    /// Reads and converts the YAML to type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The converted value.</returns>
    public abstract T? Read(ref Utf8YamlReader reader, YamlSerializerOptions options);

    /// <summary>
    /// Writes a value as YAML.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to convert.</param>
    /// <param name="options">The serializer options.</param>
    public abstract void Write(Utf8YamlWriter writer, T value, YamlSerializerOptions options);
}

/// <summary>
/// Factory for creating converters dynamically.
/// </summary>
public abstract class YamlConverterFactory : YamlConverter
{
    /// <inheritdoc/>
    public sealed override Type Type => typeof(object);

    /// <summary>
    /// Creates a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The type to create a converter for.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>A converter for the specified type.</returns>
    public abstract YamlConverter? CreateConverter(Type typeToConvert, YamlSerializerOptions options);
}
