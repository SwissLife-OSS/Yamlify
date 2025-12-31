namespace Yamlify.Serialization;

/// <summary>
/// Specifies the order of properties during serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class YamlPropertyOrderAttribute : Attribute
{
    /// <summary>
    /// Gets the order.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlPropertyOrderAttribute"/> class.
    /// </summary>
    /// <param name="order">The property order.</param>
    public YamlPropertyOrderAttribute(int order)
    {
        Order = order;
    }
}
