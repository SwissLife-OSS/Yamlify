namespace Yamlify.Nodes;

/// <summary>
/// Represents a tag directive in a YAML document.
/// </summary>
/// <param name="Handle">The tag handle (e.g., "!custom!").</param>
/// <param name="Prefix">The tag prefix (e.g., "tag:example.com,2023:").</param>
public readonly record struct TagDirective(string Handle, string Prefix);
