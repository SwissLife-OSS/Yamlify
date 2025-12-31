namespace Yamlify.Serialization;

/// <summary>
/// Marks a property as required during deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class YamlRequiredAttribute : Attribute
{
}
