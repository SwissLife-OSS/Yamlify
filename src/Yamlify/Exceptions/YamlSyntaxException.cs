namespace Yamlify.Exceptions;

/// <summary>
/// The exception that is thrown when a YAML syntax error is encountered during parsing.
/// </summary>
public class YamlSyntaxException : YamlException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSyntaxException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="position">The position in the YAML stream where the error occurred.</param>
    public YamlSyntaxException(string message, Core.Mark position) 
        : base(message, position)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSyntaxException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="position">The position in the YAML stream where the error occurred.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public YamlSyntaxException(string message, Core.Mark position, Exception innerException) 
        : base(message, position, innerException)
    {
    }
}
