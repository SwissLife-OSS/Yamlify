namespace Yamlify.Serialization;

/// <summary>
/// Specifies a known naming policy for source generation.
/// </summary>
public enum YamlKnownNamingPolicy
{
    /// <summary>
    /// No naming policy - use property names as-is.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Convert to camelCase.
    /// </summary>
    CamelCase = 1,

    /// <summary>
    /// Convert to snake_case.
    /// </summary>
    SnakeCase = 2,

    /// <summary>
    /// Convert to kebab-case.
    /// </summary>
    KebabCase = 3
}
