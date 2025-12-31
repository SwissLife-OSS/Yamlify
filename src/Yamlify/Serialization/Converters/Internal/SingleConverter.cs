using Yamlify.Core;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for single-precision floating-point values.
/// </summary>
internal sealed class SingleConverter : YamlConverter<float>
{
    /// <inheritdoc/>
    public override float Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var result = reader.TryGetDouble(out var value) ? (float)value : 0f;
        reader.Read();
        return result;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, float value, YamlSerializerOptions options)
    {
        writer.WriteNumber(value);
    }
}
