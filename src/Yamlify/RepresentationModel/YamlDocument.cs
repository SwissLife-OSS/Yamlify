namespace Yamlify.RepresentationModel;

// The following types have been moved to individual files:
// - TagDirective.cs
// - YamlStream.cs
// - YamlDocumentParser.cs
// - YamlDocumentEmitter.cs

/// <summary>
/// Represents a YAML document containing a single root node.
/// </summary>
public sealed class YamlDocument
{
    /// <summary>
    /// Gets or sets the root node of this document.
    /// </summary>
    public YamlNode? RootNode { get; set; }

    /// <summary>
    /// Gets or sets the YAML version directive (e.g., "1.2").
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets the tag directives declared in this document.
    /// </summary>
    public IList<TagDirective> TagDirectives { get; } = new List<TagDirective>();

    /// <summary>
    /// Gets whether the document has an explicit start marker (---).
    /// </summary>
    public bool HasExplicitStart { get; set; }

    /// <summary>
    /// Gets whether the document has an explicit end marker (...).
    /// </summary>
    public bool HasExplicitEnd { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlDocument"/> class.
    /// </summary>
    public YamlDocument()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlDocument"/> class with a root node.
    /// </summary>
    /// <param name="rootNode">The root node.</param>
    public YamlDocument(YamlNode rootNode)
    {
        RootNode = rootNode;
    }

    /// <summary>
    /// Creates a deep clone of this document.
    /// </summary>
    public YamlDocument DeepClone()
    {
        var clone = new YamlDocument
        {
            RootNode = RootNode?.DeepClone(),
            Version = Version,
            HasExplicitStart = HasExplicitStart,
            HasExplicitEnd = HasExplicitEnd
        };
        
        foreach (var tag in TagDirectives)
        {
            clone.TagDirectives.Add(tag);
        }
        
        return clone;
    }

    /// <summary>
    /// Accepts a visitor for traversing the document tree.
    /// </summary>
    public void Accept(IYamlVisitor visitor)
    {
        RootNode?.Accept(visitor);
    }
}
