using System.Diagnostics.CodeAnalysis;

namespace Yamlify.Nodes;

/// <summary>
/// Represents a YAML mapping (object/dictionary).
/// </summary>
public sealed class YamlMappingNode : YamlNode, IDictionary<YamlNode, YamlNode>
{
    private readonly List<KeyValuePair<YamlNode, YamlNode>> _children = new();

    /// <summary>
    /// Gets or sets the style of this mapping.
    /// </summary>
    public CollectionStyle Style { get; set; }

    /// <inheritdoc/>
    public override YamlNodeType NodeType => YamlNodeType.Mapping;

    /// <summary>
    /// Gets the number of key-value pairs in this mapping.
    /// </summary>
    public int Count => _children.Count;

    /// <inheritdoc/>
    bool ICollection<KeyValuePair<YamlNode, YamlNode>>.IsReadOnly => false;

    /// <inheritdoc/>
    public ICollection<YamlNode> Keys => _children.Select(c => c.Key).ToList();

    /// <inheritdoc/>
    public ICollection<YamlNode> Values => _children.Select(c => c.Value).ToList();

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlMappingNode"/> class.
    /// </summary>
    public YamlMappingNode()
    {
        Style = CollectionStyle.Block;
    }

    /// <summary>
    /// Initializes a new instance with key-value pairs.
    /// </summary>
    public YamlMappingNode(IEnumerable<KeyValuePair<YamlNode, YamlNode>> children)
    {
        _children.AddRange(children);
        Style = CollectionStyle.Block;
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    public YamlNode this[YamlNode key]
    {
        get
        {
            for (int i = 0; i < _children.Count; i++)
            {
                if (KeyEquals(_children[i].Key, key))
                    return _children[i].Value;
            }
            throw new KeyNotFoundException($"Key '{key}' not found");
        }
        set
        {
            for (int i = 0; i < _children.Count; i++)
            {
                if (KeyEquals(_children[i].Key, key))
                {
                    _children[i] = new KeyValuePair<YamlNode, YamlNode>(key, value);
                    return;
                }
            }
            _children.Add(new KeyValuePair<YamlNode, YamlNode>(key, value));
        }
    }

    /// <summary>
    /// Gets or sets the value associated with the specified string key.
    /// </summary>
    public YamlNode this[string key]
    {
        get => this[new YamlScalarNode(key)];
        set => this[new YamlScalarNode(key)] = value;
    }

    /// <summary>
    /// Adds a key-value pair to the mapping.
    /// </summary>
    public void Add(YamlNode key, YamlNode value)
    {
        _children.Add(new KeyValuePair<YamlNode, YamlNode>(key, value));
    }

    /// <summary>
    /// Adds a key-value pair to the mapping.
    /// </summary>
    public void Add(string key, YamlNode value)
    {
        Add(new YamlScalarNode(key), value);
    }

    /// <summary>
    /// Adds a key-value pair to the mapping.
    /// </summary>
    public void Add(string key, string value)
    {
        Add(new YamlScalarNode(key), new YamlScalarNode(value));
    }

    /// <inheritdoc/>
    void ICollection<KeyValuePair<YamlNode, YamlNode>>.Add(KeyValuePair<YamlNode, YamlNode> item)
    {
        Add(item.Key, item.Value);
    }

    /// <summary>
    /// Removes the value with the specified key.
    /// </summary>
    public bool Remove(YamlNode key)
    {
        for (int i = 0; i < _children.Count; i++)
        {
            if (KeyEquals(_children[i].Key, key))
            {
                _children.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc/>
    bool ICollection<KeyValuePair<YamlNode, YamlNode>>.Remove(KeyValuePair<YamlNode, YamlNode> item)
    {
        return _children.Remove(item);
    }

    /// <summary>
    /// Clears all items from the mapping.
    /// </summary>
    public void Clear() => _children.Clear();

    /// <summary>
    /// Returns whether the mapping contains the specified key.
    /// </summary>
    public bool ContainsKey(YamlNode key)
    {
        for (int i = 0; i < _children.Count; i++)
        {
            if (KeyEquals(_children[i].Key, key))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns whether the mapping contains the specified string key.
    /// </summary>
    public bool ContainsKey(string key) => ContainsKey(new YamlScalarNode(key));

    /// <inheritdoc/>
    bool ICollection<KeyValuePair<YamlNode, YamlNode>>.Contains(KeyValuePair<YamlNode, YamlNode> item)
    {
        return _children.Contains(item);
    }

    /// <summary>
    /// Tries to get the value associated with the specified key.
    /// </summary>
    public bool TryGetValue(YamlNode key, [MaybeNullWhen(false)] out YamlNode value)
    {
        for (int i = 0; i < _children.Count; i++)
        {
            if (KeyEquals(_children[i].Key, key))
            {
                value = _children[i].Value;
                return true;
            }
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Tries to get the value associated with the specified string key.
    /// </summary>
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out YamlNode value)
    {
        return TryGetValue(new YamlScalarNode(key), out value);
    }

    /// <inheritdoc/>
    void ICollection<KeyValuePair<YamlNode, YamlNode>>.CopyTo(KeyValuePair<YamlNode, YamlNode>[] array, int arrayIndex)
    {
        _children.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<YamlNode, YamlNode>> GetEnumerator() => _children.GetEnumerator();

    /// <inheritdoc/>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public override void Accept(IYamlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc/>
    public override YamlNode DeepClone()
    {
        var clone = new YamlMappingNode
        {
            Anchor = Anchor,
            Tag = Tag,
            Style = Style,
            Start = Start,
            End = End
        };
        
        foreach (var kvp in _children)
        {
            clone.Add(kvp.Key.DeepClone(), kvp.Value.DeepClone());
        }
        
        return clone;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var pairs = _children.Select(kvp => $"{kvp.Key}: {kvp.Value}");
        return $"{{{string.Join(", ", pairs)}}}";
    }

    private static bool KeyEquals(YamlNode a, YamlNode b)
    {
        if (a is YamlScalarNode sa && b is YamlScalarNode sb)
            return sa.Value == sb.Value;
        return ReferenceEquals(a, b);
    }
}
