namespace Yamlify.Serialization;

/// <summary>
/// Specifies where the type discriminator property should be written during serialization.
/// </summary>
public enum DiscriminatorPosition
{
    /// <summary>
    /// The discriminator property is written according to its <see cref="YamlPropertyOrderAttribute"/>
    /// or declaration order, just like any other property. This is the default.
    /// </summary>
    Ordered = 0,

    /// <summary>
    /// The discriminator property is always written first, regardless of <see cref="YamlPropertyOrderAttribute"/>.
    /// This can be useful for human readability when viewing YAML files.
    /// </summary>
    First = 1
}
