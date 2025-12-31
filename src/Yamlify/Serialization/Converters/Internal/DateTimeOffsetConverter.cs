using Yamlify.Core;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for DateTimeOffset values.
/// </summary>
internal sealed class DateTimeOffsetConverter : YamlConverter<DateTimeOffset>
{
    /// <inheritdoc/>
    public override DateTimeOffset Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return DateTimeOffset.TryParse(str, out var value) ? value : default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, DateTimeOffset value, YamlSerializerOptions options)
    {
        writer.WriteString(value.ToString("O"));
    }
}
