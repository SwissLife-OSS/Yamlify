using System.Diagnostics.CodeAnalysis;

namespace Yamlify.Nodes;

/// <summary>
/// Base class for all YAML nodes in the representation model.
/// </summary>
/// <remarks>
/// The representation model provides a mutable DOM-like API for YAML documents.
/// </remarks>
public abstract class YamlNode
{
    /// <summary>
    /// Gets or sets the anchor name for this node.
    /// </summary>
    public string? Anchor { get; set; }

    /// <summary>
    /// Gets or sets the tag for this node.
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// Gets the type of this node.
    /// </summary>
    public abstract YamlNodeType NodeType { get; }

    /// <summary>
    /// Gets the start position of this node in the source.
    /// </summary>
    public Mark Start { get; internal set; }

    /// <summary>
    /// Gets the end position of this node in the source.
    /// </summary>
    public Mark End { get; internal set; }

    /// <summary>
    /// Accepts a visitor for traversing the node tree.
    /// </summary>
    /// <param name="visitor">The visitor to accept.</param>
    public abstract void Accept(IYamlVisitor visitor);

    /// <summary>
    /// Creates a deep clone of this node.
    /// </summary>
    /// <returns>A deep clone of this node.</returns>
    public abstract YamlNode DeepClone();

    /// <summary>
    /// Implicit conversion from string to YamlScalarNode.
    /// </summary>
    public static implicit operator YamlNode(string value) => new YamlScalarNode(value);

    /// <summary>
    /// Implicit conversion from int to YamlScalarNode.
    /// </summary>
    public static implicit operator YamlNode(int value) => new YamlScalarNode(value.ToString());

    /// <summary>
    /// Implicit conversion from long to YamlScalarNode.
    /// </summary>
    public static implicit operator YamlNode(long value) => new YamlScalarNode(value.ToString());

    /// <summary>
    /// Implicit conversion from double to YamlScalarNode.
    /// </summary>
    public static implicit operator YamlNode(double value) => new YamlScalarNode(value.ToString());

    /// <summary>
    /// Implicit conversion from bool to YamlScalarNode.
    /// </summary>
    public static implicit operator YamlNode(bool value) => new YamlScalarNode(value ? "true" : "false");
}
