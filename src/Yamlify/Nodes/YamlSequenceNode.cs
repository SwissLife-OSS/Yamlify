namespace Yamlify.Nodes;

/// <summary>
/// Represents a YAML sequence (array/list).
/// </summary>
public sealed class YamlSequenceNode : YamlNode, IList<YamlNode>
{
    private readonly List<YamlNode> _children = new();

    /// <summary>
    /// Gets or sets the style of this sequence.
    /// </summary>
    public CollectionStyle Style { get; set; }

    /// <inheritdoc/>
    public override YamlNodeType NodeType => YamlNodeType.Sequence;

    /// <summary>
    /// Gets the number of items in this sequence.
    /// </summary>
    public int Count => _children.Count;

    /// <inheritdoc/>
    bool ICollection<YamlNode>.IsReadOnly => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSequenceNode"/> class.
    /// </summary>
    public YamlSequenceNode()
    {
        Style = CollectionStyle.Block;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSequenceNode"/> class with items.
    /// </summary>
    /// <param name="children">The initial items.</param>
    public YamlSequenceNode(IEnumerable<YamlNode> children)
    {
        _children.AddRange(children);
        Style = CollectionStyle.Block;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSequenceNode"/> class with items.
    /// </summary>
    /// <param name="children">The initial items.</param>
    public YamlSequenceNode(params YamlNode[] children)
    {
        _children.AddRange(children);
        Style = CollectionStyle.Block;
    }

    /// <summary>
    /// Gets or sets the item at the specified index.
    /// </summary>
    public YamlNode this[int index]
    {
        get => _children[index];
        set => _children[index] = value;
    }

    /// <summary>
    /// Adds an item to the sequence.
    /// </summary>
    public void Add(YamlNode item) => _children.Add(item);

    /// <summary>
    /// Inserts an item at the specified index.
    /// </summary>
    public void Insert(int index, YamlNode item) => _children.Insert(index, item);

    /// <summary>
    /// Removes the item at the specified index.
    /// </summary>
    public void RemoveAt(int index) => _children.RemoveAt(index);

    /// <summary>
    /// Removes an item from the sequence.
    /// </summary>
    public bool Remove(YamlNode item) => _children.Remove(item);

    /// <summary>
    /// Clears all items from the sequence.
    /// </summary>
    public void Clear() => _children.Clear();

    /// <summary>
    /// Returns whether the sequence contains the specified item.
    /// </summary>
    public bool Contains(YamlNode item) => _children.Contains(item);

    /// <summary>
    /// Returns the index of the specified item.
    /// </summary>
    public int IndexOf(YamlNode item) => _children.IndexOf(item);

    /// <inheritdoc/>
    void ICollection<YamlNode>.CopyTo(YamlNode[] array, int arrayIndex) => _children.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public IEnumerator<YamlNode> GetEnumerator() => _children.GetEnumerator();

    /// <inheritdoc/>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public override void Accept(IYamlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc/>
    public override YamlNode DeepClone()
    {
        var clone = new YamlSequenceNode
        {
            Anchor = Anchor,
            Tag = Tag,
            Style = Style,
            Start = Start,
            End = End
        };
        
        foreach (var child in _children)
        {
            clone.Add(child.DeepClone());
        }
        
        return clone;
    }

    /// <inheritdoc/>
    public override string ToString() => $"[{string.Join(", ", _children)}]";
}
