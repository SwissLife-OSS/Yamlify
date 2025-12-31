using System.Diagnostics.CodeAnalysis;

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
    public Core.Mark Start { get; internal set; }

    /// <summary>
    /// Gets the end position of this node in the source.
    /// </summary>
    public Core.Mark End { get; internal set; }

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

/// <summary>
/// Represents a YAML scalar value (string, number, boolean, null).
/// </summary>
public sealed class YamlScalarNode : YamlNode
{
    /// <summary>
    /// Represents a null scalar value.
    /// </summary>
    public static readonly YamlScalarNode Null = new(null) { Tag = "tag:yaml.org,2002:null" };

    /// <summary>
    /// Represents a true boolean value.
    /// </summary>
    public static readonly YamlScalarNode True = new("true") { Tag = "tag:yaml.org,2002:bool" };

    /// <summary>
    /// Represents a false boolean value.
    /// </summary>
    public static readonly YamlScalarNode False = new("false") { Tag = "tag:yaml.org,2002:bool" };

    /// <summary>
    /// Gets or sets the scalar value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the style of this scalar.
    /// </summary>
    public Core.ScalarStyle Style { get; set; }

    /// <inheritdoc/>
    public override YamlNodeType NodeType => YamlNodeType.Scalar;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlScalarNode"/> class.
    /// </summary>
    public YamlScalarNode()
    {
        Value = null;
        Style = Core.ScalarStyle.Any;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlScalarNode"/> class with a value.
    /// </summary>
    /// <param name="value">The scalar value.</param>
    public YamlScalarNode(string? value)
    {
        Value = value;
        Style = Core.ScalarStyle.Any;
    }

    /// <inheritdoc/>
    public override void Accept(IYamlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc/>
    public override YamlNode DeepClone() => new YamlScalarNode(Value)
    {
        Anchor = Anchor,
        Tag = Tag,
        Style = Style,
        Start = Start,
        End = End
    };

    /// <summary>
    /// Gets the value as a boolean.
    /// </summary>
    public bool GetBoolean()
    {
        return Value?.ToLowerInvariant() switch
        {
            "true" or "yes" or "on" => true,
            "false" or "no" or "off" => false,
            _ => throw new InvalidOperationException($"Cannot convert '{Value}' to boolean")
        };
    }

    /// <summary>
    /// Gets the value as an integer.
    /// </summary>
    public int GetInt32()
    {
        if (int.TryParse(Value, out int result))
            return result;
        throw new InvalidOperationException($"Cannot convert '{Value}' to Int32");
    }

    /// <summary>
    /// Gets the value as a long integer.
    /// </summary>
    public long GetInt64()
    {
        if (long.TryParse(Value, out long result))
            return result;
        throw new InvalidOperationException($"Cannot convert '{Value}' to Int64");
    }

    /// <summary>
    /// Gets the value as a double.
    /// </summary>
    public double GetDouble()
    {
        return Value?.ToLowerInvariant() switch
        {
            ".inf" or "+.inf" => double.PositiveInfinity,
            "-.inf" => double.NegativeInfinity,
            ".nan" => double.NaN,
            _ when double.TryParse(Value, out double result) => result,
            _ => throw new InvalidOperationException($"Cannot convert '{Value}' to Double")
        };
    }

    /// <summary>
    /// Returns whether this scalar represents null.
    /// </summary>
    public bool IsNull => Value is null or "" or "~" or "null" or "Null" or "NULL";

    /// <inheritdoc/>
    public override string ToString() => Value ?? "~";

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is YamlScalarNode other)
            return Value == other.Value;
        if (obj is string str)
            return Value == str;
        return false;
    }

    /// <inheritdoc/>
    public override int GetHashCode() => Value?.GetHashCode() ?? 0;
}

/// <summary>
/// Represents a YAML sequence (array/list).
/// </summary>
public sealed class YamlSequenceNode : YamlNode, IList<YamlNode>
{
    private readonly List<YamlNode> _children = new();

    /// <summary>
    /// Gets or sets the style of this sequence.
    /// </summary>
    public Core.CollectionStyle Style { get; set; }

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
        Style = Core.CollectionStyle.Block;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSequenceNode"/> class with items.
    /// </summary>
    /// <param name="children">The initial items.</param>
    public YamlSequenceNode(IEnumerable<YamlNode> children)
    {
        _children.AddRange(children);
        Style = Core.CollectionStyle.Block;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSequenceNode"/> class with items.
    /// </summary>
    /// <param name="children">The initial items.</param>
    public YamlSequenceNode(params YamlNode[] children)
    {
        _children.AddRange(children);
        Style = Core.CollectionStyle.Block;
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

/// <summary>
/// Represents a YAML mapping (object/dictionary).
/// </summary>
public sealed class YamlMappingNode : YamlNode, IDictionary<YamlNode, YamlNode>
{
    private readonly List<KeyValuePair<YamlNode, YamlNode>> _children = new();

    /// <summary>
    /// Gets or sets the style of this mapping.
    /// </summary>
    public Core.CollectionStyle Style { get; set; }

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
        Style = Core.CollectionStyle.Block;
    }

    /// <summary>
    /// Initializes a new instance with key-value pairs.
    /// </summary>
    public YamlMappingNode(IEnumerable<KeyValuePair<YamlNode, YamlNode>> children)
    {
        _children.AddRange(children);
        Style = Core.CollectionStyle.Block;
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

/// <summary>
/// Visitor interface for traversing YAML nodes.
/// </summary>
public interface IYamlVisitor
{
    /// <summary>Visits a scalar node.</summary>
    void Visit(YamlScalarNode scalar);
    
    /// <summary>Visits a sequence node.</summary>
    void Visit(YamlSequenceNode sequence);
    
    /// <summary>Visits a mapping node.</summary>
    void Visit(YamlMappingNode mapping);
    
    /// <summary>Visits an alias node.</summary>
    void Visit(YamlAliasNode alias);
}
