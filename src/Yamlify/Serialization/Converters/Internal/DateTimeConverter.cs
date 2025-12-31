using Yamlify.Core;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for DateTime values.
/// </summary>
internal sealed class DateTimeConverter : YamlConverter<DateTime>
{
    /// <inheritdoc/>
    public override DateTime Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return DateTime.TryParse(str, out var value) ? value : default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, DateTime value, YamlSerializerOptions options)
    {
        writer.WriteString(value.ToString("O"));
    }
}
