namespace Yamlify.Exceptions;

/// <summary>
/// The exception that is thrown when a YAML semantic error is encountered (e.g., unresolved alias).
/// </summary>
public class YamlSemanticException : YamlException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSemanticException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public YamlSemanticException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSemanticException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="position">The position in the YAML stream where the error occurred.</param>
    public YamlSemanticException(string message, Core.Mark position) 
        : base(message, position)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSemanticException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public YamlSemanticException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
