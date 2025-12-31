namespace Yamlify.Schema;

/// <summary>
/// YAML tag constants.
/// </summary>
public static class YamlTags
{
    /// <summary>The standard tag prefix for YAML types.</summary>
    public const string Prefix = "tag:yaml.org,2002:";

    /// <summary>The null tag.</summary>
    public const string Null = "tag:yaml.org,2002:null";

    /// <summary>The boolean tag.</summary>
    public const string Bool = "tag:yaml.org,2002:bool";

    /// <summary>The integer tag.</summary>
    public const string Int = "tag:yaml.org,2002:int";

    /// <summary>The floating-point tag.</summary>
    public const string Float = "tag:yaml.org,2002:float";

    /// <summary>The string tag.</summary>
    public const string Str = "tag:yaml.org,2002:str";

    /// <summary>The sequence tag.</summary>
    public const string Seq = "tag:yaml.org,2002:seq";

    /// <summary>The mapping tag.</summary>
    public const string Map = "tag:yaml.org,2002:map";

    /// <summary>The binary tag (base64 encoded).</summary>
    public const string Binary = "tag:yaml.org,2002:binary";

    /// <summary>The timestamp tag.</summary>
    public const string Timestamp = "tag:yaml.org,2002:timestamp";
}
