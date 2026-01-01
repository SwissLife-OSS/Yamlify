using Yamlify;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for string values.
/// </summary>
internal sealed class StringConverter : YamlConverter<string>
{
    /// <inheritdoc/>
    public override string? Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        if (reader.IsNull())
        {
            reader.Read();
            return null;
        }
        
        var value = reader.GetString();
        reader.Read();
        return value;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, string value, YamlSerializerOptions options)
    {
        writer.WriteString(value);
    }
}
