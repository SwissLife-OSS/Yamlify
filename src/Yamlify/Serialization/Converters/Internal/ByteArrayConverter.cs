using Yamlify;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for byte array values (Base64 encoded).
/// </summary>
internal sealed class ByteArrayConverter : YamlConverter<byte[]>
{
    /// <inheritdoc/>
    public override byte[]? Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        if (reader.IsNull())
        {
            reader.Read();
            return null;
        }
        
        var str = reader.GetString();
        reader.Read();
        
        try
        {
            return str != null ? Convert.FromBase64String(str) : null;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, byte[] value, YamlSerializerOptions options)
    {
        writer.WriteString(Convert.ToBase64String(value));
    }
}
