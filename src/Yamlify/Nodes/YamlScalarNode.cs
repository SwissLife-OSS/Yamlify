namespace Yamlify.Nodes;

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
    public ScalarStyle Style { get; set; }

    /// <inheritdoc/>
    public override YamlNodeType NodeType => YamlNodeType.Scalar;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlScalarNode"/> class.
    /// </summary>
    public YamlScalarNode()
    {
        Value = null;
        Style = ScalarStyle.Any;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlScalarNode"/> class with a value.
    /// </summary>
    /// <param name="value">The scalar value.</param>
    public YamlScalarNode(string? value)
    {
        Value = value;
        Style = ScalarStyle.Any;
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
