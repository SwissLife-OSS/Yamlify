namespace Yamlify.Serialization;

/// <summary>
/// Default values for <see cref="YamlSerializerOptions"/>.
/// </summary>
internal static class YamlSerializerDefaults
{
    /// <summary>
    /// Default maximum recursion depth for serialization and deserialization.
    /// </summary>
    /// <remarks>
    /// This value (64) provides protection against stack overflow from deeply nested or circular structures.
    /// </remarks>
    internal const int DefaultMaxDepth = 64;

    /// <summary>
    /// Maximum allowed value for MaxDepth to prevent unreasonable memory allocation.
    /// </summary>
    internal const int MaxAllowedDepth = 1_000;
}
