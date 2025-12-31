using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Yamlify.Core;

namespace Yamlify.Serialization;

/// <summary>
/// Provides AOT-compatible functionality to serialize objects to YAML and deserialize YAML to objects.
/// </summary>
/// <remarks>
/// <para>
/// This serializer is fully AOT-compatible and requires a source-generated <see cref="YamlSerializerContext"/>
/// to provide type metadata. No reflection is used at runtime.
/// </para>
/// <para>
/// For AOT compilation (Native AOT, iOS, WASM), you must use the overloads that accept a 
/// <see cref="YamlSerializerContext"/> or <see cref="YamlTypeInfo{T}"/>, or set the 
/// <see cref="YamlSerializerOptions.Default"/> TypeInfoResolver once at startup.
/// </para>
/// <example>
/// <code>
/// // Option 1: Pass context explicitly
/// var yaml = YamlSerializer.Serialize(person, MyContext.Default.Person);
/// 
/// // Option 2: Set default resolver once, then use simple overloads
/// YamlSerializerOptions.Default.TypeInfoResolver = MyContext.Default;
/// var yaml = YamlSerializer.Serialize(person); // No context needed!
/// var person = YamlSerializer.Deserialize&lt;Person&gt;(yaml);
/// </code>
/// </example>
/// </remarks>
public static class YamlSerializer
{
    #region Serialize - Simple API (uses Default.TypeInfoResolver)

    /// <summary>
    /// Converts the value to a YAML string using the default type info resolver.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>A YAML string representation of the value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no TypeInfoResolver is configured on <see cref="YamlSerializerOptions.Default"/>.
    /// </exception>
    /// <remarks>
    /// Requires <see cref="YamlSerializerOptions.Default"/>.TypeInfoResolver to be set before use.
    /// </remarks>
    public static string Serialize<TValue>(TValue value)
    {
        var options = YamlSerializerOptions.Default;
        var typeInfo = GetTypeInfoFromDefault<TValue>(options);
        options.MarkAsUsed();
        return Serialize(value, typeInfo);
    }

    /// <summary>
    /// Converts the value to a YAML string using custom options with a type info resolver.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Serializer options with TypeInfoResolver configured.</param>
    /// <returns>A YAML string representation of the value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no TypeInfoResolver is configured on the options.
    /// </exception>
    public static string Serialize<TValue>(TValue value, YamlSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var typeInfo = GetTypeInfoFromOptions<TValue>(options);
        options.MarkAsUsed();
        return Serialize(value, typeInfo, options);
    }

    #endregion

    #region Serialize - Context-based (AOT-compatible)

    /// <summary>
    /// Converts the value to a YAML string using source-generated type information.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="typeInfo">The source-generated type info for the value.</param>
    /// <returns>A YAML string representation of the value.</returns>
    public static string Serialize<TValue>(TValue value, YamlTypeInfo<TValue> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        
        var bufferWriter = new ArrayBufferWriter<byte>();
        using var writer = new Utf8YamlWriter(bufferWriter, CreateWriterOptions(typeInfo.Options));
        
        SerializeCore(writer, value, typeInfo);
        writer.Flush();
        
        return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
    }

    /// <summary>
    /// Converts the value to a YAML string using a serialization context.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="context">The source-generated serialization context.</param>
    /// <returns>A YAML string representation of the value.</returns>
    public static string Serialize<TValue>(TValue value, YamlSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var typeInfo = context.GetTypeInfo<TValue>();
        if (typeInfo is null)
        {
            throw new InvalidOperationException(
                $"No type info found for type '{typeof(TValue).FullName}' in the provided context. " +
                $"Ensure the type is registered with [YamlSerializable(typeof({typeof(TValue).Name}))].");
        }
        
        return Serialize(value, typeInfo);
    }

    /// <summary>
    /// Converts the value to a YAML string using source-generated type information and custom options.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="typeInfo">The source-generated type info for the value.</param>
    /// <param name="options">Custom serializer options to override the default.</param>
    /// <returns>A YAML string representation of the value.</returns>
    public static string Serialize<TValue>(TValue value, YamlTypeInfo<TValue> typeInfo, YamlSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        ArgumentNullException.ThrowIfNull(options);
        
        var bufferWriter = new ArrayBufferWriter<byte>();
        using var writer = new Utf8YamlWriter(bufferWriter, CreateWriterOptions(options));
        
        SerializeCore(writer, value, typeInfo, options);
        writer.Flush();
        
        return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
    }

    /// <summary>
    /// Converts the value to UTF-8 encoded YAML bytes using source-generated type information.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="typeInfo">The source-generated type info for the value.</param>
    /// <returns>A UTF-8 encoded YAML representation of the value.</returns>
    public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, YamlTypeInfo<TValue> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        
        var bufferWriter = new ArrayBufferWriter<byte>();
        using var writer = new Utf8YamlWriter(bufferWriter, CreateWriterOptions(typeInfo.Options));
        
        SerializeCore(writer, value, typeInfo);
        writer.Flush();
        
        return bufferWriter.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Writes the value as YAML to the specified writer.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="typeInfo">The source-generated type info for the value.</param>
    public static void Serialize<TValue>(Utf8YamlWriter writer, TValue value, YamlTypeInfo<TValue> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(typeInfo);
        
        SerializeCore(writer, value, typeInfo);
    }

    #endregion

    #region Serialize - Stream (AOT-compatible)

    /// <summary>
    /// Writes the value as YAML to the specified stream using the default type info resolver.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no TypeInfoResolver is configured on <see cref="YamlSerializerOptions.Default"/>.
    /// </exception>
    public static void Serialize<TValue>(Stream stream, TValue value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var options = YamlSerializerOptions.Default;
        var typeInfo = GetTypeInfoFromDefault<TValue>(options);
        options.MarkAsUsed();
        Serialize(stream, value, typeInfo);
    }

    /// <summary>
    /// Writes the value as YAML to the specified stream.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="typeInfo">The source-generated type info for the value.</param>
    public static void Serialize<TValue>(Stream stream, TValue value, YamlTypeInfo<TValue> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(typeInfo);
        
        using var writer = new Utf8YamlWriter(stream, CreateWriterOptions(typeInfo.Options));
        SerializeCore(writer, value, typeInfo);
    }

    /// <summary>
    /// Writes the value as YAML to the specified stream using custom options.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Serializer options with TypeInfoResolver configured.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no TypeInfoResolver is configured on the options.
    /// </exception>
    public static void Serialize<TValue>(Stream stream, TValue value, YamlSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(options);
        var typeInfo = GetTypeInfoFromOptions<TValue>(options);
        options.MarkAsUsed();
        
        using var writer = new Utf8YamlWriter(stream, CreateWriterOptions(options));
        SerializeCore(writer, value, typeInfo, options);
    }

    /// <summary>
    /// Asynchronously writes the value as YAML to the specified stream using the default type info resolver.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no TypeInfoResolver is configured on <see cref="YamlSerializerOptions.Default"/>.
    /// </exception>
    public static async Task SerializeAsync<TValue>(
        Stream stream, 
        TValue value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var options = YamlSerializerOptions.Default;
        var typeInfo = GetTypeInfoFromDefault<TValue>(options);
        options.MarkAsUsed();
        await SerializeAsync(stream, value, typeInfo, cancellationToken);
    }

    /// <summary>
    /// Asynchronously writes the value as YAML to the specified stream.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="typeInfo">The source-generated type info for the value.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static async Task SerializeAsync<TValue>(
        Stream stream, 
        TValue value, 
        YamlTypeInfo<TValue> typeInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(typeInfo);
        
        var bytes = SerializeToUtf8Bytes(value, typeInfo);
        await stream.WriteAsync(bytes, cancellationToken);
    }

    #endregion

    #region Deserialize - Simple API (uses Default.TypeInfoResolver)

    /// <summary>
    /// Parses the YAML string into a <typeparamref name="TValue"/> using the default type info resolver.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize to.</typeparam>
    /// <param name="yaml">The YAML string to parse.</param>
    /// <returns>A <typeparamref name="TValue"/> representation of the YAML value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no TypeInfoResolver is configured on <see cref="YamlSerializerOptions.Default"/>.
    /// </exception>
    /// <remarks>
    /// Requires <see cref="YamlSerializerOptions.Default"/>.TypeInfoResolver to be set before use.
    /// </remarks>
    public static TValue? Deserialize<TValue>(string yaml)
    {
        var options = YamlSerializerOptions.Default;
        var typeInfo = GetTypeInfoFromDefault<TValue>(options);
        options.MarkAsUsed();
        return Deserialize(yaml, typeInfo);
    }

    /// <summary>
    /// Parses the UTF-8 encoded YAML into a <typeparamref name="TValue"/> using the default type info resolver.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize to.</typeparam>
    /// <param name="utf8Yaml">The UTF-8 encoded YAML to parse.</param>
    /// <returns>A <typeparamref name="TValue"/> representation of the YAML value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no TypeInfoResolver is configured on <see cref="YamlSerializerOptions.Default"/>.
    /// </exception>
    public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> utf8Yaml)
    {
        var options = YamlSerializerOptions.Default;
        var typeInfo = GetTypeInfoFromDefault<TValue>(options);
        options.MarkAsUsed();
        return Deserialize(utf8Yaml, typeInfo);
    }

    /// <summary>
    /// Parses the YAML string into a <typeparamref name="TValue"/> using custom options with a type info resolver.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize to.</typeparam>
    /// <param name="yaml">The YAML string to parse.</param>
    /// <param name="options">Serializer options with TypeInfoResolver configured.</param>
    /// <returns>A <typeparamref name="TValue"/> representation of the YAML value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no TypeInfoResolver is configured on the options.
    /// </exception>
    public static TValue? Deserialize<TValue>(string yaml, YamlSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var typeInfo = GetTypeInfoFromOptions<TValue>(options);
        options.MarkAsUsed();
        return Deserialize(yaml, typeInfo);
    }

    #endregion

    #region Deserialize - Context-based (AOT-compatible)

    /// <summary>
    /// Parses the YAML string representing a single value into a <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize to.</typeparam>
    /// <param name="yaml">The YAML string to parse.</param>
    /// <param name="typeInfo">The source-generated type info for the target type.</param>
    /// <returns>A <typeparamref name="TValue"/> representation of the YAML value.</returns>
    public static TValue? Deserialize<TValue>(string yaml, YamlTypeInfo<TValue> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        
        if (string.IsNullOrEmpty(yaml))
        {
            return default;
        }
        
        var utf8Bytes = Encoding.UTF8.GetBytes(yaml);
        return Deserialize(utf8Bytes.AsSpan(), typeInfo);
    }

    /// <summary>
    /// Parses the UTF-8 encoded YAML representing a single value into a <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize to.</typeparam>
    /// <param name="utf8Yaml">The UTF-8 encoded YAML to parse.</param>
    /// <param name="typeInfo">The source-generated type info for the target type.</param>
    /// <returns>A <typeparamref name="TValue"/> representation of the YAML value.</returns>
    public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> utf8Yaml, YamlTypeInfo<TValue> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        
        if (utf8Yaml.IsEmpty)
        {
            return default;
        }
        
        var reader = new Utf8YamlReader(utf8Yaml);
        
        // Skip to first meaningful token
        while (reader.Read())
        {
            if (reader.TokenType != YamlTokenType.StreamStart &&
                reader.TokenType != YamlTokenType.DocumentStart)
            {
                break;
            }
        }
        
        return DeserializeCore(ref reader, typeInfo);
    }

    /// <summary>
    /// Parses the YAML string using a serialization context.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize to.</typeparam>
    /// <param name="yaml">The YAML string to parse.</param>
    /// <param name="context">The source-generated serialization context.</param>
    /// <returns>A <typeparamref name="TValue"/> representation of the YAML value.</returns>
    public static TValue? Deserialize<TValue>(string yaml, YamlSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var typeInfo = context.GetTypeInfo<TValue>();
        if (typeInfo is null)
        {
            throw new InvalidOperationException(
                $"No type info found for type '{typeof(TValue).FullName}' in the provided context. " +
                $"Ensure the type is registered with [YamlSerializable(typeof({typeof(TValue).Name}))].");
        }
        
        return Deserialize(yaml, typeInfo);
    }

    /// <summary>
    /// Reads a value from the specified reader.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize to.</typeparam>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="typeInfo">The source-generated type info for the target type.</param>
    /// <returns>A <typeparamref name="TValue"/> representation of the YAML value.</returns>
    [return: MaybeNull]
    public static TValue Deserialize<TValue>(ref Utf8YamlReader reader, YamlTypeInfo<TValue> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        return DeserializeCore(ref reader, typeInfo);
    }

    #endregion

    #region Deserialize - Stream (AOT-compatible)

    /// <summary>
    /// Reads the stream and parses it as YAML using the default type info resolver.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize to.</typeparam>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>A <typeparamref name="TValue"/> representation of the YAML value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no TypeInfoResolver is configured on <see cref="YamlSerializerOptions.Default"/>.
    /// </exception>
    [return: MaybeNull]
    public static TValue Deserialize<TValue>(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var options = YamlSerializerOptions.Default;
        var typeInfo = GetTypeInfoFromDefault<TValue>(options);
        options.MarkAsUsed();
        return Deserialize(stream, typeInfo);
    }

    /// <summary>
    /// Reads the stream and parses it as YAML into a <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize to.</typeparam>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="typeInfo">The source-generated type info for the target type.</param>
    /// <returns>A <typeparamref name="TValue"/> representation of the YAML value.</returns>
    [return: MaybeNull]
    public static TValue Deserialize<TValue>(Stream stream, YamlTypeInfo<TValue> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(typeInfo);
        
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return Deserialize(memoryStream.ToArray().AsSpan(), typeInfo);
    }

    /// <summary>
    /// Reads the stream and parses it as YAML using custom options.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize to.</typeparam>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="options">Serializer options with TypeInfoResolver configured.</param>
    /// <returns>A <typeparamref name="TValue"/> representation of the YAML value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no TypeInfoResolver is configured on the options.
    /// </exception>
    [return: MaybeNull]
    public static TValue Deserialize<TValue>(Stream stream, YamlSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(options);
        var typeInfo = GetTypeInfoFromOptions<TValue>(options);
        options.MarkAsUsed();
        return Deserialize(stream, typeInfo);
    }

    /// <summary>
    /// Asynchronously reads the stream and parses it as YAML using the default type info resolver.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize to.</typeparam>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <typeparamref name="TValue"/> representation of the YAML value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no TypeInfoResolver is configured on <see cref="YamlSerializerOptions.Default"/>.
    /// </exception>
    [return: MaybeNull]
    public static async ValueTask<TValue> DeserializeAsync<TValue>(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var options = YamlSerializerOptions.Default;
        var typeInfo = GetTypeInfoFromDefault<TValue>(options);
        options.MarkAsUsed();
        return await DeserializeAsync(stream, typeInfo, cancellationToken);
    }

    /// <summary>
    /// Asynchronously reads the stream and parses it as YAML into a <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize to.</typeparam>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="typeInfo">The source-generated type info for the target type.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <typeparamref name="TValue"/> representation of the YAML value.</returns>
    [return: MaybeNull]
    public static async ValueTask<TValue> DeserializeAsync<TValue>(
        Stream stream, 
        YamlTypeInfo<TValue> typeInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(typeInfo);
        
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return Deserialize(memoryStream.ToArray().AsSpan(), typeInfo);
    }

    #endregion

    #region Core Implementation

    private static void SerializeCore<TValue>(Utf8YamlWriter writer, TValue value, YamlTypeInfo<TValue> typeInfo)
    {
        SerializeCore(writer, value, typeInfo, typeInfo.Options ?? YamlSerializerOptions.Default);
    }

    private static void SerializeCore<TValue>(Utf8YamlWriter writer, TValue value, YamlTypeInfo<TValue> typeInfo, YamlSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        // Create reference resolver scope for circular reference detection
        using var scope = options.BeginSerialize();

        if (typeInfo.Converter is YamlConverter<TValue> converter)
        {
            converter.Write(writer, value, options);
            return;
        }

        // Use serialize action if available (from source generator)
        if (typeInfo.SerializeAction is not null)
        {
            typeInfo.SerializeAction(writer, value, options);
            return;
        }

        throw new InvalidOperationException(
            $"No serialization logic found for type '{typeof(TValue).FullName}'. " +
            "Ensure the type has a converter or serialize action configured.");
    }

    private static TValue? DeserializeCore<TValue>(ref Utf8YamlReader reader, YamlTypeInfo<TValue> typeInfo)
    {
        if (reader.TokenType == YamlTokenType.None ||
            reader.TokenType == YamlTokenType.DocumentEnd ||
            reader.TokenType == YamlTokenType.StreamEnd)
        {
            return default;
        }

        if (reader.IsNull())
        {
            reader.Read();
            return default;
        }

        if (typeInfo.Converter is YamlConverter<TValue> converter)
        {
            return converter.Read(ref reader, typeInfo.Options ?? YamlSerializerOptions.Default);
        }

        // Use deserialize func if available (from source generator)
        if (typeInfo.DeserializeFunc is not null)
        {
            return typeInfo.DeserializeFunc(ref reader, typeInfo.Options ?? YamlSerializerOptions.Default);
        }

        throw new InvalidOperationException(
            $"No deserialization logic found for type '{typeof(TValue).FullName}'. " +
            "Ensure the type has a converter or deserialize function configured.");
    }

    private static YamlWriterOptions CreateWriterOptions(YamlSerializerOptions? options)
    {
        options ??= YamlSerializerOptions.Default;
        
        return new YamlWriterOptions
        {
            IndentSize = options.IndentSize,
            PreferFlowStyle = options.PreferFlowStyle,
            DefaultScalarStyle = options.DefaultScalarStyle,
            SkipNullValues = options.IgnoreNullValues,
            IndentSequenceItems = options.IndentSequenceItems
        };
    }

    private static YamlTypeInfo<TValue> GetTypeInfoFromDefault<TValue>(YamlSerializerOptions options)
    {
        var resolver = options.TypeInfoResolver 
            ?? throw new InvalidOperationException(
                $"No TypeInfoResolver is configured on YamlSerializerOptions.Default. " +
                $"Set YamlSerializerOptions.Default.TypeInfoResolver to your generated context before using this overload, " +
                $"or use the overload that accepts a YamlTypeInfo<{typeof(TValue).Name}> or YamlSerializerContext.");
        
        var typeInfo = resolver.GetTypeInfo(typeof(TValue), options) as YamlTypeInfo<TValue>
            ?? throw new InvalidOperationException(
                $"No type info found for type '{typeof(TValue).FullName}' in the configured TypeInfoResolver. " +
                $"Ensure the type is registered with [YamlSerializable(typeof({typeof(TValue).Name}))].");
        
        return typeInfo;
    }

    private static YamlTypeInfo<TValue> GetTypeInfoFromOptions<TValue>(YamlSerializerOptions options)
    {
        var resolver = options.TypeInfoResolver 
            ?? throw new InvalidOperationException(
                $"No TypeInfoResolver is configured on the provided options. " +
                $"Set options.TypeInfoResolver or use the overload that accepts a YamlTypeInfo<{typeof(TValue).Name}>.");
        
        var typeInfo = resolver.GetTypeInfo(typeof(TValue), options) as YamlTypeInfo<TValue>
            ?? throw new InvalidOperationException(
                $"No type info found for type '{typeof(TValue).FullName}' in the configured TypeInfoResolver. " +
                $"Ensure the type is registered with [YamlSerializable(typeof({typeof(TValue).Name}))].");
        
        return typeInfo;
    }

    #endregion
}
