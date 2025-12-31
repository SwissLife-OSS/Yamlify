namespace Yamlify.Serialization;

/// <summary>
/// Configures source generation options for a <see cref="YamlSerializerContext"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class YamlSourceGenerationOptionsAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the property naming policy.
    /// </summary>
    public YamlKnownNamingPolicy PropertyNamingPolicy { get; set; } = YamlKnownNamingPolicy.KebabCase;

    /// <summary>
    /// Gets or sets the source generation mode.
    /// </summary>
    public YamlSourceGenerationMode GenerationMode { get; set; } = YamlSourceGenerationMode.Default;

    /// <summary>
    /// Gets or sets the property ordering strategy.
    /// </summary>
    /// <remarks>
    /// When set to <see cref="YamlPropertyOrdering.Alphabetical"/>, properties will be serialized
    /// in alphabetical order by their serialized name. Using <see cref="YamlPropertyOrderAttribute"/>
    /// on any property when this is set to Alphabetical will cause a compile-time error.
    /// </remarks>
    public YamlPropertyOrdering PropertyOrdering { get; set; } = YamlPropertyOrdering.DeclarationOrder;

    /// <summary>
    /// Gets or sets whether to ignore null values during serialization.
    /// </summary>
    public bool IgnoreNullValues { get; set; }

    /// <summary>
    /// Gets or sets whether to ignore objects that would serialize to an empty mapping.
    /// </summary>
    /// <remarks>
    /// When true, if an object has no non-null properties (after applying IgnoreNullValues),
    /// the entire property is omitted from the output.
    /// </remarks>
    public bool IgnoreEmptyObjects { get; set; }

    /// <summary>
    /// Gets or sets whether to ignore read-only properties.
    /// </summary>
    public bool IgnoreReadOnlyProperties { get; set; }

    /// <summary>
    /// Gets or sets whether to include fields.
    /// </summary>
    public bool IncludeFields { get; set; }

    /// <summary>
    /// Gets or sets whether to write indented YAML.
    /// </summary>
    public bool WriteIndented { get; set; } = true;

    /// <summary>
    /// Gets or sets the default indent size.
    /// </summary>
    public int IndentSize { get; set; } = 2;

    /// <summary>
    /// Gets or sets whether sequence items should be indented relative to their parent key.
    /// When true (default), sequence items are indented:
    /// <code>
    /// resources:
    ///   - name: foo
    /// </code>
    /// When false, sequence items are at the same level as the parent key (compact style):
    /// <code>
    /// resources:
    /// - name: foo
    /// </code>
    /// </summary>
    public bool IndentSequenceItems { get; set; } = true;

    /// <summary>
    /// Gets or sets the default scalar style.
    /// </summary>
    public Core.ScalarStyle DefaultScalarStyle { get; set; } = Core.ScalarStyle.Any;

    /// <summary>
    /// Gets or sets the position of type discriminator properties during serialization.
    /// </summary>
    /// <remarks>
    /// When set to <see cref="DiscriminatorPosition.Ordered"/> (default), discriminator properties
    /// are written according to their <see cref="YamlPropertyOrderAttribute"/> or declaration order.
    /// When set to <see cref="DiscriminatorPosition.First"/>, discriminator properties are always
    /// written first, regardless of their order attribute.
    /// </remarks>
    public DiscriminatorPosition DiscriminatorPosition { get; set; } = DiscriminatorPosition.Ordered;
}
