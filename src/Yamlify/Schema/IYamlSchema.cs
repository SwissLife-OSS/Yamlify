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
