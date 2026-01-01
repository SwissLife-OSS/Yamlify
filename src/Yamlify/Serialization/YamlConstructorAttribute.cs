namespace Yamlify.Serialization;

/// <summary>
/// Specifies constructor parameters for deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor)]
public sealed class YamlConstructorAttribute : Attribute
{
}
