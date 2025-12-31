using Yamlify.Core;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for 32-bit integer values.
/// </summary>
internal sealed class Int32Converter : YamlConverter<int>
{
    /// <inheritdoc/>
    public override int Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var result = reader.TryGetInt32(out var value) ? value : 0;
        reader.Read();
        return result;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, int value, YamlSerializerOptions options)
    {
        writer.WriteNumber(value);
    }
}
