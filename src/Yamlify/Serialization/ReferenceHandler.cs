namespace Yamlify.Serialization;

/// <summary>
/// Defines how object references are handled during serialization.
/// </summary>
public abstract class ReferenceHandler
{
    /// <summary>
    /// Gets a reference handler that ignores circular references.
    /// </summary>
    public static ReferenceHandler IgnoreCycles { get; } = new IgnoreCyclesReferenceHandler();

    /// <summary>
    /// Gets a reference handler that preserves references using YAML anchors.
    /// </summary>
    public static ReferenceHandler Preserve { get; } = new PreserveReferenceHandler();

    /// <summary>
    /// Creates a resolver for tracking references.
    /// </summary>
    public abstract ReferenceResolver CreateResolver();
}
