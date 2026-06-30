namespace Yamlify.Serialization;

/// <summary>
/// When applied to a property, forces null values to be preserved during
/// serialization and deserialization, even if the enclosing context has
/// <see cref="YamlSourceGenerationOptionsAttribute.IgnoreNullValues"/> set to true.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class KeepNullValueAttribute : Attribute
{
}
