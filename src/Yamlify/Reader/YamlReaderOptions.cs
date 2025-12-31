namespace Yamlify;

/// <summary>
/// Provides options for configuring a <see cref="Utf8YamlReader"/>.
/// </summary>
public readonly struct YamlReaderOptions
{
    /// <summary>
    /// Gets the default reader options.
    /// </summary>
    public static YamlReaderOptions Default => new();

    /// <summary>
    /// Gets the maximum depth of nesting allowed when reading YAML.
    /// Default is 64.
    /// </summary>
    public int MaxDepth { get; init; }

    /// <summary>
    /// Gets a value indicating whether comments should be reported as tokens.
    /// Default is false (comments are skipped).
    /// </summary>
    public bool ReadComments { get; init; }

    /// <summary>
    /// Gets a value indicating whether to allow trailing commas in flow collections.
    /// Default is true (YAML 1.2 allows this).
    /// </summary>
    public bool AllowTrailingCommas { get; init; }

    /// <summary>
    /// Gets a value indicating whether duplicate keys should cause an error.
    /// Default is false (last value wins, per YAML spec recommendation).
    /// </summary>
    public bool StrictDuplicateKeys { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlReaderOptions"/> struct with default values.
    /// </summary>
    public YamlReaderOptions()
    {
        MaxDepth = 64;
        ReadComments = false;
        AllowTrailingCommas = true;
        StrictDuplicateKeys = false;
    }
}
