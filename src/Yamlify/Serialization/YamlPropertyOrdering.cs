namespace Yamlify.Serialization;

/// <summary>
/// Specifies the ordering strategy for properties during serialization.
/// </summary>
public enum YamlPropertyOrdering
{
    /// <summary>
    /// Inherit the ordering from the context-level setting.
    /// This is the default when used on [YamlSerializable] attribute.
    /// </summary>
    Inherit = -1,

    /// <summary>
    /// Properties are ordered by their declaration order in the source code.
    /// This is the default behavior and provides stable, predictable output.
    /// </summary>
    DeclarationOrder = 0,

    /// <summary>
    /// Properties are ordered alphabetically by their serialized name (after naming policy is applied).
    /// </summary>
    /// <remarks>
    /// When this option is used, any <see cref="YamlPropertyOrderAttribute"/> on properties
    /// will cause a compile-time error, as explicit ordering conflicts with alphabetical ordering.
    /// </remarks>
    Alphabetical = 1,

    /// <summary>
    /// Properties with <see cref="YamlPropertyOrderAttribute"/> come first (sorted by order value),
    /// followed by remaining properties sorted alphabetically by their serialized name.
    /// </summary>
    /// <remarks>
    /// This allows you to pin specific important properties at the top (e.g., identifiers),
    /// while all other properties are consistently ordered alphabetically.
    /// </remarks>
    OrderedThenAlphabetical = 2
}
