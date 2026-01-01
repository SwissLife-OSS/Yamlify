namespace Yamlify.Serialization;

/// <summary>
/// Marks a class as using a specific converter.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class YamlConverterAttribute : Attribute
{
    /// <summary>
    /// Gets the converter type.
    /// </summary>
    public Type ConverterType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlConverterAttribute"/> class.
    /// </summary>
    /// <param name="converterType">The type of the converter.</param>
    public YamlConverterAttribute(Type converterType)
    {
        ConverterType = converterType;
    }
}
