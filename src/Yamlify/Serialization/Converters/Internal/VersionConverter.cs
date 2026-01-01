using Yamlify;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for Version values.
/// </summary>
internal sealed class VersionConverter : YamlConverter<Version>
{
    /// <inheritdoc/>
    public override Version? Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return str != null && Version.TryParse(str, out var value) ? value : null;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, Version value, YamlSerializerOptions options)
    {
        writer.WriteString(value.ToString());
    }
}
