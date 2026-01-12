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

    /// <summary>
    /// Maximum number of read operations allowed without progress before throwing.
    /// This prevents infinite loops caused by converters that don't advance the reader.
    /// </summary>
    internal const int MaxReadsWithoutProgress = 1000;
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
    /// Gets a delegate to the source-generated deserialization logic.
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
    ///     return GeneratedRead(ref reader, options);
    /// }
    /// </code>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this property is accessed but the source generator hasn't set it up.
    /// This typically happens if the type doesn't have a <see cref="YamlConverterAttribute"/>
    /// or isn't registered with a <see cref="YamlSerializerContext"/>.
    /// </exception>
    public YamlDeserializeFunc<T> GeneratedRead
    {
        get => _generatedRead ?? throw new InvalidOperationException(
            $"GeneratedRead has not been initialized for {GetType().Name}. " +
            $"Ensure the type has [YamlConverter] attribute and is registered in a YamlSerializerContext.");
        init => _generatedRead = value;
    }
    private YamlDeserializeFunc<T>? _generatedRead;

    /// <summary>
    /// Gets a delegate to the source-generated serialization logic.
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
    ///     GeneratedWrite(writer, value, options);
    /// }
    /// </code>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this property is accessed but the source generator hasn't set it up.
    /// This typically happens if the type doesn't have a <see cref="YamlConverterAttribute"/>
    /// or isn't registered with a <see cref="YamlSerializerContext"/>.
    /// </exception>
    public YamlSerializeAction<T> GeneratedWrite
    {
        get => _generatedWrite ?? throw new InvalidOperationException(
            $"GeneratedWrite has not been initialized for {GetType().Name}. " +
            $"Ensure the type has [YamlConverter] attribute and is registered in a YamlSerializerContext.");
        init => _generatedWrite = value;
    }
    private YamlSerializeAction<T>? _generatedWrite;
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

/// <summary>
/// Provides helper methods for safe converter invocation with infinite loop protection.
/// </summary>
public static class YamlConverterSafeRead
{
    /// <summary>
    /// Calls a custom converter's Read method with protection against infinite loops.
    /// Throws if the reader doesn't advance after the call.
    /// </summary>
    /// <typeparam name="T">The type being read.</typeparam>
    /// <param name="converter">The converter to call.</param>
    /// <param name="reader">The YAML reader.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="YamlException">Thrown when the converter doesn't advance the reader.</exception>
    public static T? Read<T>(YamlConverter<T> converter, ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var positionBefore = reader.BytesConsumed;
        var tokenBefore = reader.TokenType;
        var depthBefore = reader.CurrentDepth;
        
        var result = converter.Read(ref reader, options);
        
        // Check if the reader advanced - either bytes consumed changed, token type changed, or depth changed
        // This handles cases where the converter consumed tokens that might result in same byte position (rare)
        var hasAdvanced = reader.BytesConsumed != positionBefore 
            || reader.TokenType != tokenBefore 
            || reader.CurrentDepth != depthBefore
            || reader.TokenType == YamlTokenType.None; // End of document is fine
        
        if (!hasAdvanced)
        {
            throw new YamlException(
                $"Custom converter '{converter.GetType().Name}' did not advance the reader. " +
                $"This will cause an infinite loop. Ensure you call reader.Read() after processing scalar values.",
                reader.Position);
        }
        
        return result;
    }
}
