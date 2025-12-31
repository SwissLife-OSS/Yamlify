using Yamlify;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for Uri values.
/// </summary>
internal sealed class UriConverter : YamlConverter<Uri>
{
    /// <inheritdoc/>
    public override Uri? Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return str != null && Uri.TryCreate(str, UriKind.RelativeOrAbsolute, out var value) ? value : null;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, Uri value, YamlSerializerOptions options)
    {
        writer.WriteString(value.OriginalString);
    }
}
