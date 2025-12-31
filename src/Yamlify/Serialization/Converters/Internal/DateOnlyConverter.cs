using Yamlify.Core;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for DateOnly values.
/// </summary>
internal sealed class DateOnlyConverter : YamlConverter<DateOnly>
{
    /// <inheritdoc/>
    public override DateOnly Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return DateOnly.TryParse(str, out var value) ? value : default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, DateOnly value, YamlSerializerOptions options)
    {
        writer.WriteString(value.ToString("O"));
    }
}
