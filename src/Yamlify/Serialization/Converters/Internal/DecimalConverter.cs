using Yamlify;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for decimal values.
/// </summary>
internal sealed class DecimalConverter : YamlConverter<decimal>
{
    /// <inheritdoc/>
    public override decimal Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return decimal.TryParse(str, out var value) ? value : 0m;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, decimal value, YamlSerializerOptions options)
    {
        writer.WriteNumber((double)value);
    }
}
