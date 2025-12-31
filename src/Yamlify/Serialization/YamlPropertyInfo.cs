namespace Yamlify.Serialization;

/// <summary>
/// Provides metadata about a property for YAML serialization.
/// </summary>
public abstract class YamlPropertyInfo
{
    /// <summary>
    /// Gets the name of the property in the CLR type.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the name of the property as it appears in YAML.
    /// </summary>
    public abstract string YamlPropertyName { get; }

    /// <summary>
    /// Gets the type of the property.
    /// </summary>
    public abstract Type PropertyType { get; }

    /// <summary>
    /// Gets whether the property is required.
    /// </summary>
    public abstract bool IsRequired { get; }

    /// <summary>
    /// Gets the order of the property.
    /// </summary>
    public abstract int Order { get; }

    /// <summary>
    /// Gets the ignore condition for this property.
    /// </summary>
    public abstract YamlIgnoreCondition? IgnoreCondition { get; }

    /// <summary>
    /// Gets the getter function.
    /// </summary>
    public abstract Func<object, object?>? Get { get; }

    /// <summary>
    /// Gets the setter action.
    /// </summary>
    public abstract Action<object, object?>? Set { get; }
}
