namespace Yamlify.Core;

/// <summary>
/// Represents a position within a YAML stream, including line and column information.
/// </summary>
/// <remarks>
/// Line and column numbers are 1-based to match typical editor conventions.
/// </remarks>
public readonly struct Mark : IEquatable<Mark>
{
    /// <summary>
    /// Gets an empty mark representing no position.
    /// </summary>
    public static Mark Empty => default;

    /// <summary>
    /// Gets the byte offset from the start of the input.
    /// </summary>
    public long Offset { get; }

    /// <summary>
    /// Gets the line number (1-based).
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the column number (1-based).
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Mark"/> struct.
    /// </summary>
    /// <param name="offset">The byte offset from the start of the input.</param>
    /// <param name="line">The line number (1-based).</param>
    /// <param name="column">The column number (1-based).</param>
    public Mark(long offset, int line, int column)
    {
        Offset = offset;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Returns a string representation of this mark.
    /// </summary>
    public override string ToString() => $"Line: {Line}, Col: {Column}, Offset: {Offset}";

    /// <inheritdoc/>
    public bool Equals(Mark other) =>
        Offset == other.Offset && Line == other.Line && Column == other.Column;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Mark other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Offset, Line, Column);

    /// <summary>
    /// Determines whether two marks are equal.
    /// </summary>
    public static bool operator ==(Mark left, Mark right) => left.Equals(right);

    /// <summary>
    /// Determines whether two marks are not equal.
    /// </summary>
    public static bool operator !=(Mark left, Mark right) => !left.Equals(right);
}
