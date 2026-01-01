namespace Yamlify;

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
