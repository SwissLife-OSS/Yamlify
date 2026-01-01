namespace Yamlify.Serialization;

/// <summary>
/// Specifies the source generation mode for the YAML serializer.
/// </summary>
public enum YamlSourceGenerationMode
{
    /// <summary>
    /// Default mode - generates both metadata and serialization logic.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Generates only metadata (type info, property info).
    /// </summary>
    Metadata = 1,

    /// <summary>
    /// Generates full serialization logic optimized for AOT.
    /// </summary>
    Serialization = 2
}
