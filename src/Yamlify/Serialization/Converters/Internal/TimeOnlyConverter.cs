using Yamlify.Core;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for TimeOnly values.
/// </summary>
internal sealed class TimeOnlyConverter : YamlConverter<TimeOnly>
{
    /// <inheritdoc/>
    public override TimeOnly Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return TimeOnly.TryParse(str, out var value) ? value : default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, TimeOnly value, YamlSerializerOptions options)
    {
        writer.WriteString(value.ToString("O"));
    }
}
