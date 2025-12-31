namespace Yamlify.Nodes;

/// <summary>
/// Represents an alias reference to another node.
/// </summary>
public sealed class YamlAliasNode : YamlNode
{
    /// <summary>
    /// Gets or sets the name of the anchor this alias references.
    /// </summary>
    public string AnchorName { get; set; }

    /// <inheritdoc/>
    public override YamlNodeType NodeType => YamlNodeType.Alias;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlAliasNode"/> class.
    /// </summary>
    /// <param name="anchorName">The anchor name to reference.</param>
    public YamlAliasNode(string anchorName)
    {
        AnchorName = anchorName ?? throw new ArgumentNullException(nameof(anchorName));
    }

    /// <inheritdoc/>
    public override void Accept(IYamlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc/>
    public override YamlNode DeepClone() => new YamlAliasNode(AnchorName)
    {
        Anchor = Anchor,
        Tag = Tag,
        Start = Start,
        End = End
    };

    /// <inheritdoc/>
    public override string ToString() => $"*{AnchorName}";
}
