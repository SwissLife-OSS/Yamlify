namespace Yamlify.Serialization;

/// <summary>
/// Ignores a property during serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class YamlIgnoreAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the condition under which the property is ignored.
    /// </summary>
    public YamlIgnoreCondition Condition { get; set; } = YamlIgnoreCondition.Always;
}
