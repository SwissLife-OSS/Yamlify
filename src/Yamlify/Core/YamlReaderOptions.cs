namespace Yamlify.Core;

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
    public bool CommentHandling { get; init; }

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
        CommentHandling = false;
        AllowTrailingCommas = true;
        StrictDuplicateKeys = false;
    }
}

/// <summary>
/// Encapsulates the state of a <see cref="Utf8YamlReader"/> for resumption across async boundaries.
/// </summary>
public struct YamlReaderState
{
    internal YamlReaderOptions Options { get; }
    internal int CurrentDepth { get; set; }
    internal long BytesConsumed { get; set; }
    internal int Line { get; set; }
    internal int Column { get; set; }
    internal bool InStreamContext { get; set; }
    internal bool InDocumentContext { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlReaderState"/> struct.
    /// </summary>
    /// <param name="options">The reader options to use.</param>
    public YamlReaderState(YamlReaderOptions options = default)
    {
        Options = options.MaxDepth == 0 ? YamlReaderOptions.Default : options;
        CurrentDepth = 0;
        BytesConsumed = 0;
        Line = 1;
        Column = 1;
        InStreamContext = false;
        InDocumentContext = false;
    }
}
