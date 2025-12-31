namespace Yamlify.Core;

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
