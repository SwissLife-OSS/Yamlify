using Yamlify.Core;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for double-precision floating-point values.
/// </summary>
internal sealed class DoubleConverter : YamlConverter<double>
{
    /// <inheritdoc/>
    public override double Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var result = reader.TryGetDouble(out var value) ? value : 0.0;
        reader.Read();
        return result;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, double value, YamlSerializerOptions options)
    {
        writer.WriteNumber(value);
    }
}
