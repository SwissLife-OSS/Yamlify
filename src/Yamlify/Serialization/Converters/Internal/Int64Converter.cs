using Yamlify.Core;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for 64-bit integer values.
/// </summary>
internal sealed class Int64Converter : YamlConverter<long>
{
    /// <inheritdoc/>
    public override long Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var result = reader.TryGetInt64(out var value) ? value : 0L;
        reader.Read();
        return result;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, long value, YamlSerializerOptions options)
    {
        writer.WriteNumber(value);
    }
}
