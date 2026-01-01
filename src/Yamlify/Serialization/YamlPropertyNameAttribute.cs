namespace Yamlify.Serialization;

/// <summary>
/// Specifies a property name to use during serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class YamlPropertyNameAttribute : Attribute
{
    /// <summary>
    /// Gets the property name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlPropertyNameAttribute"/> class.
    /// </summary>
    /// <param name="name">The property name to use in YAML.</param>
    public YamlPropertyNameAttribute(string name)
    {
        Name = name;
    }
}
