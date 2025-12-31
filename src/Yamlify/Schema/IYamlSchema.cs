namespace Yamlify.Schema;

/// <summary>
/// Defines a YAML schema for resolving tags and validating content.
/// </summary>
/// <remarks>
/// YAML 1.2 defines three schemas:
/// <list type="bullet">
/// <item><description>Failsafe Schema - only maps, sequences, and strings</description></item>
/// <item><description>JSON Schema - adds null, bool, int, and float</description></item>
/// <item><description>Core Schema - extends JSON with user-friendly literals</description></item>
/// </list>
/// </remarks>
public interface IYamlSchema
{
    /// <summary>
    /// Gets the name of this schema.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Resolves the implicit tag for a scalar value.
    /// </summary>
    /// <param name="value">The scalar value.</param>
    /// <returns>The resolved tag, or null if the value should use the default string tag.</returns>
    string? ResolveScalarTag(ReadOnlySpan<char> value);

    /// <summary>
    /// Resolves the implicit tag for a non-plain scalar (quoted or block).
    /// </summary>
    /// <param name="value">The scalar value.</param>
    /// <param name="style">The scalar style.</param>
    /// <returns>The resolved tag.</returns>
    string ResolveNonPlainScalarTag(ReadOnlySpan<char> value, Core.ScalarStyle style);

    /// <summary>
    /// Gets the canonical representation of a value for a given tag.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="tag">The tag.</param>
    /// <returns>The canonical representation.</returns>
    string GetCanonicalValue(string value, string tag);

    /// <summary>
    /// Validates that a value is valid for a given tag.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="tag">The tag.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool ValidateValue(string value, string tag);
}

/// <summary>
/// YAML tag constants.
/// </summary>
public static class YamlTags
{
    /// <summary>The standard tag prefix for YAML types.</summary>
    public const string Prefix = "tag:yaml.org,2002:";

    /// <summary>The null tag.</summary>
    public const string Null = "tag:yaml.org,2002:null";

    /// <summary>The boolean tag.</summary>
    public const string Bool = "tag:yaml.org,2002:bool";

    /// <summary>The integer tag.</summary>
    public const string Int = "tag:yaml.org,2002:int";

    /// <summary>The floating-point tag.</summary>
    public const string Float = "tag:yaml.org,2002:float";

    /// <summary>The string tag.</summary>
    public const string Str = "tag:yaml.org,2002:str";

    /// <summary>The sequence tag.</summary>
    public const string Seq = "tag:yaml.org,2002:seq";

    /// <summary>The mapping tag.</summary>
    public const string Map = "tag:yaml.org,2002:map";

    /// <summary>The binary tag (base64 encoded).</summary>
    public const string Binary = "tag:yaml.org,2002:binary";

    /// <summary>The timestamp tag.</summary>
    public const string Timestamp = "tag:yaml.org,2002:timestamp";
}
