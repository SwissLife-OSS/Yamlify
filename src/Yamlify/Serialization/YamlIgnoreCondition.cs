namespace Yamlify.Serialization;

/// <summary>
/// Specifies when a property should be ignored.
/// </summary>
public enum YamlIgnoreCondition
{
    /// <summary>Always ignore the property.</summary>
    Always,
    /// <summary>Ignore the property only if it's null.</summary>
    WhenWritingNull,
    /// <summary>Ignore the property only if it's the default value.</summary>
    WhenWritingDefault,
    /// <summary>Never ignore the property.</summary>
    Never
}
