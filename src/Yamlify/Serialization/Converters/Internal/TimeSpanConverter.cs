using Yamlify;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for TimeSpan values.
/// </summary>
internal sealed class TimeSpanConverter : YamlConverter<TimeSpan>
{
    /// <inheritdoc/>
    public override TimeSpan Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return TimeSpan.TryParse(str, out var value) ? value : default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, TimeSpan value, YamlSerializerOptions options)
    {
        writer.WriteString(value.ToString("c"));
    }
}
