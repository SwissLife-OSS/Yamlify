namespace Yamlify.Serialization;

/// <summary>
/// Resolves and tracks object references during serialization.
/// </summary>
public abstract class ReferenceResolver
{
    /// <summary>
    /// Adds a reference to the resolver.
    /// </summary>
    /// <param name="referenceId">The reference identifier (anchor name).</param>
    /// <param name="value">The referenced object.</param>
    public abstract void AddReference(string referenceId, object value);

    /// <summary>
    /// Gets the reference identifier for an object if it exists.
    /// </summary>
    /// <param name="value">The object to get the reference for.</param>
    /// <param name="alreadyExists">Whether the reference already exists.</param>
    /// <returns>The reference identifier.</returns>
    public abstract string GetReference(object value, out bool alreadyExists);

    /// <summary>
    /// Resolves a reference by its identifier.
    /// </summary>
    /// <param name="referenceId">The reference identifier.</param>
    /// <returns>The referenced object.</returns>
    public abstract object ResolveReference(string referenceId);

    /// <summary>
    /// Checks if an object has already been serialized (cycle detection).
    /// </summary>
    /// <param name="value">The object to check.</param>
    /// <returns>True if the object was already serialized (is a cycle), false otherwise.</returns>
    public bool IsCycleReference(object value)
    {
        GetReference(value, out var alreadyExists);
        return alreadyExists;
    }
}
