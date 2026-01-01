namespace Yamlify;

/// <summary>
/// The exception that is thrown when the maximum recursion depth is exceeded during
/// YAML serialization or deserialization.
/// </summary>
/// <remarks>
/// This exception is thrown to prevent stack overflow or memory exhaustion when processing
/// deeply nested or circular YAML structures. The maximum recursion depth can be configured
/// via <see cref="Serialization.YamlSerializerOptions.MaxDepth"/>.
/// </remarks>
/// <example>
/// <code>
/// // Configure a higher limit if needed for deeply nested structures
/// var options = new YamlSerializerOptions { MaxDepth = 128 };
/// var result = YamlSerializer.Deserialize&lt;MyType&gt;(yaml, options);
/// </code>
/// </example>
public class MaxRecursionDepthExceededException : YamlException
{
    /// <summary>
    /// Gets the maximum recursion depth that was exceeded.
    /// </summary>
    public int MaxDepth { get; }

    /// <summary>
    /// Gets the current recursion depth when the exception was thrown.
    /// </summary>
    public int CurrentDepth { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxRecursionDepthExceededException"/> class
    /// with the specified maximum and current depths.
    /// </summary>
    /// <param name="maxDepth">The maximum recursion depth that was exceeded.</param>
    /// <param name="currentDepth">The current recursion depth when the exception was thrown.</param>
    public MaxRecursionDepthExceededException(int maxDepth, int currentDepth)
        : base(FormatMessage(maxDepth, currentDepth))
    {
        MaxDepth = maxDepth;
        CurrentDepth = currentDepth;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxRecursionDepthExceededException"/> class
    /// with the specified maximum depth, current depth, and position information.
    /// </summary>
    /// <param name="maxDepth">The maximum recursion depth that was exceeded.</param>
    /// <param name="currentDepth">The current recursion depth when the exception was thrown.</param>
    /// <param name="position">The position in the YAML stream where the error occurred.</param>
    public MaxRecursionDepthExceededException(int maxDepth, int currentDepth, Mark position)
        : base(FormatMessage(maxDepth, currentDepth), position)
    {
        MaxDepth = maxDepth;
        CurrentDepth = currentDepth;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxRecursionDepthExceededException"/> class
    /// with the specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public MaxRecursionDepthExceededException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxRecursionDepthExceededException"/> class
    /// with the specified message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MaxRecursionDepthExceededException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    private static string FormatMessage(int maxDepth, int currentDepth) =>
        $"Maximum recursion depth of {maxDepth} exceeded (current depth: {currentDepth}). " +
        "This may indicate a circular reference or deeply nested structure. " +
        "Consider using YamlSerializerOptions.MaxDepth to increase the limit if needed.";
}
