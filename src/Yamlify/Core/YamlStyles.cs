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

/// <summary>
/// Specifies the collection style used in YAML presentation.
/// </summary>
public enum CollectionStyle : byte
{
    /// <summary>
    /// Any style (for reader) or auto-detect best style (for writer).
    /// </summary>
    Any = 0,

    /// <summary>
    /// Block style using indentation.
    /// </summary>
    Block,

    /// <summary>
    /// Flow style using explicit indicators ([], {}).
    /// </summary>
    Flow
}

/// <summary>
/// Specifies the chomping behavior for block scalars.
/// </summary>
public enum ChompingIndicator : byte
{
    /// <summary>
    /// Clip - keep final line break, remove trailing blank lines (default).
    /// </summary>
    Clip = 0,

    /// <summary>
    /// Strip - remove final line break and trailing blank lines.
    /// </summary>
    Strip,

    /// <summary>
    /// Keep - keep final line break and trailing blank lines.
    /// </summary>
    Keep
}
