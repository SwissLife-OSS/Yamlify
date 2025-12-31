namespace Yamlify;

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
