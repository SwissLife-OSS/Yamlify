namespace Yamlify.Serialization;

/// <summary>
/// Determines the policy for converting property names.
/// </summary>
public abstract class YamlNamingPolicy
{
    /// <summary>
    /// Gets a naming policy that converts names to camelCase.
    /// </summary>
    public static YamlNamingPolicy CamelCase { get; } = new CamelCaseNamingPolicy();

    /// <summary>
    /// Gets a naming policy that converts names to snake_case.
    /// </summary>
    public static YamlNamingPolicy SnakeCase { get; } = new SnakeCaseNamingPolicy();

    /// <summary>
    /// Gets a naming policy that converts names to kebab-case.
    /// </summary>
    public static YamlNamingPolicy KebabCase { get; } = new KebabCaseNamingPolicy();

    /// <summary>
    /// Converts a name.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The converted name.</returns>
    public abstract string ConvertName(string name);
}
