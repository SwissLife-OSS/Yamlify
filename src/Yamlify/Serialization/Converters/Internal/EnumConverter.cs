using Yamlify;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for enum types.
/// </summary>
/// <typeparam name="T">The enum type.</typeparam>
internal sealed class EnumConverter<T> : YamlConverter<T> where T : struct, Enum
{
    /// <inheritdoc/>
    public override T Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        
        if (Enum.TryParse<T>(str, true, out var result))
        {
            return result;
        }
        
        return default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, T value, YamlSerializerOptions options)
    {
        writer.WriteString(value.ToString());
    }
}
