using Yamlify;
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

    /// <summary>
    /// Gets or sets a delegate to the source-generated deserialization logic.
    /// This allows custom converters to delegate to the generated code for standard deserialization
    /// while handling special cases (e.g., legacy format migration) themselves.
    /// </summary>
    /// <remarks>
    /// This property is automatically set by the source generator when a custom converter is registered
    /// via <see cref="YamlConverterAttribute"/>. It enables patterns like:
    /// <code>
    /// public override T? Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    /// {
    ///     // Handle legacy format
    ///     if (reader.TokenType == YamlTokenType.Boolean)
    ///         return new MyType(reader.GetBoolean());
    ///     
    ///     // Delegate to generated code for standard format
    ///     return GeneratedRead!(ref reader, options);
    /// }
    /// </code>
    /// </remarks>
    public YamlDeserializeFunc<T>? GeneratedRead { get; init; }

    /// <summary>
    /// Gets or sets a delegate to the source-generated serialization logic.
    /// This allows custom converters to delegate to the generated code for standard serialization
    /// while performing custom pre/post processing.
    /// </summary>
    /// <remarks>
    /// This property is automatically set by the source generator when a custom converter is registered
    /// via <see cref="YamlConverterAttribute"/>. It enables patterns like:
    /// <code>
    /// public override void Write(Utf8YamlWriter writer, T value, YamlSerializerOptions options)
    /// {
    ///     // Pre-processing
    ///     LogWrite(value);
    ///     
    ///     // Delegate to generated code
    ///     GeneratedWrite!(writer, value, options);
    /// }
    /// </code>
    /// </remarks>
    public YamlSerializeAction<T>? GeneratedWrite { get; init; }
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
