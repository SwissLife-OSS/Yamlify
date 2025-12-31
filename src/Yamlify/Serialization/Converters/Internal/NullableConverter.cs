using Yamlify.Core;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for nullable value types.
/// </summary>
/// <typeparam name="T">The underlying value type.</typeparam>
internal sealed class NullableConverter<T> : YamlConverter<T?> where T : struct
{
    private readonly YamlConverter<T> _underlyingConverter;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="underlyingConverter">The converter for the underlying type.</param>
    public NullableConverter(YamlConverter<T> underlyingConverter)
    {
        _underlyingConverter = underlyingConverter;
    }

    /// <inheritdoc/>
    public override T? Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        if (reader.IsNull())
        {
            reader.Read();
            return null;
        }

        return _underlyingConverter.Read(ref reader, options);
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, T? value, YamlSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNull();
            return;
        }

        _underlyingConverter.Write(writer, value.Value, options);
    }
}
