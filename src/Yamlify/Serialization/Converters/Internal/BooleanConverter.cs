using Yamlify;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for boolean values.
/// </summary>
internal sealed class BooleanConverter : YamlConverter<bool>
{
    /// <inheritdoc/>
    public override bool Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var result = reader.TryGetBoolean(out var value) ? value : false;
        reader.Read();
        return result;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, bool value, YamlSerializerOptions options)
    {
        writer.WriteBoolean(value);
    }
}
