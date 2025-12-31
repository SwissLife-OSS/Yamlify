namespace Yamlify.Core;

/// <summary>
/// Specifies the scalar style used in YAML presentation.
/// </summary>
public enum ScalarStyle : byte
{
    /// <summary>
    /// Any style (for reader) or auto-detect best style (for writer).
    /// </summary>
    Any = 0,

    /// <summary>
    /// Plain (unquoted) scalar style.
    /// </summary>
    Plain,

    /// <summary>
    /// Single-quoted scalar style ('value').
    /// </summary>
    SingleQuoted,

    /// <summary>
    /// Double-quoted scalar style ("value").
    /// </summary>
    DoubleQuoted,

    /// <summary>
    /// Literal block scalar style (|).
    /// </summary>
    Literal,

    /// <summary>
    /// Folded block scalar style (>).
    /// </summary>
    Folded
}
