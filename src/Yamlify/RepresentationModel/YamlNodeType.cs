namespace Yamlify.RepresentationModel;

/// <summary>
/// Represents the type of a YAML node.
/// </summary>
public enum YamlNodeType
{
    /// <summary>A scalar value (string, number, boolean, null).</summary>
    Scalar,
    /// <summary>A sequence (array/list).</summary>
    Sequence,
    /// <summary>A mapping (object/dictionary).</summary>
    Mapping,
    /// <summary>An alias reference to another node.</summary>
    Alias
}
