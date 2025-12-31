using Yamlify.Core;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for Guid values.
/// </summary>
internal sealed class GuidConverter : YamlConverter<Guid>
{
    /// <inheritdoc/>
    public override Guid Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return Guid.TryParse(str, out var value) ? value : Guid.Empty;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, Guid value, YamlSerializerOptions options)
    {
        writer.WriteString(value.ToString());
    }
}
