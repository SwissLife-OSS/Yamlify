namespace Yamlify.Exceptions;

// The following types have been moved to individual files:
// - YamlSyntaxException.cs
// - YamlSemanticException.cs

/// <summary>
/// The exception that is thrown when a YAML parsing or serialization error occurs.
/// </summary>
public class YamlException : Exception
{
    /// <summary>
    /// Gets the position in the YAML stream where the error occurred.
    /// </summary>
    public Core.Mark? Position { get; }

    /// <summary>
    /// Gets the path to the element where the error occurred.
    /// </summary>
    public string? Path { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlException"/> class.
    /// </summary>
    public YamlException() : base("A YAML error occurred.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public YamlException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlException"/> class with a specified error message
    /// and position information.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="position">The position in the YAML stream where the error occurred.</param>
    public YamlException(string message, Core.Mark position) 
        : base(FormatMessage(message, position))
    {
        Position = position;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlException"/> class with a specified error message,
    /// position information, and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="position">The position in the YAML stream where the error occurred.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public YamlException(string message, Core.Mark position, Exception innerException) 
        : base(FormatMessage(message, position), innerException)
    {
        Position = position;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlException"/> class with a specified error message
    /// and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public YamlException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlException"/> class with a specified error message
    /// and path information.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="path">The path to the element where the error occurred.</param>
    public YamlException(string message, string path) 
        : base(FormatMessage(message, path))
    {
        Path = path;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlException"/> class with a specified error message,
    /// path information, and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="path">The path to the element where the error occurred.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public YamlException(string message, string path, Exception innerException) 
        : base(FormatMessage(message, path), innerException)
    {
        Path = path;
    }

    private static string FormatMessage(string message, Core.Mark position) =>
        $"{message} Line: {position.Line}, Col: {position.Column}";

    private static string FormatMessage(string message, string path) =>
        $"{message} Path: {path}";
}
