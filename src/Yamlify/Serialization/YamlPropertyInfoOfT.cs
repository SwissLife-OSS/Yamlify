namespace Yamlify.Serialization;

/// <summary>
/// Provides strongly-typed metadata about a property for YAML serialization.
/// </summary>
/// <typeparam name="TDeclaringType">The type that declares the property.</typeparam>
/// <typeparam name="TProperty">The type of the property.</typeparam>
public sealed class YamlPropertyInfo<TDeclaringType, TProperty> : YamlPropertyInfo
{
    /// <inheritdoc/>
    public override string Name { get; }

    /// <inheritdoc/>
    public override string YamlPropertyName { get; }

    /// <inheritdoc/>
    public override Type PropertyType => typeof(TProperty);

    /// <inheritdoc/>
    public override bool IsRequired { get; }

    /// <inheritdoc/>
    public override int Order { get; }

    /// <inheritdoc/>
    public override YamlIgnoreCondition? IgnoreCondition { get; }

    /// <inheritdoc/>
    public override Func<object, object?>? Get { get; }

    /// <inheritdoc/>
    public override Action<object, object?>? Set { get; }

    /// <summary>
    /// Gets the strongly-typed getter function.
    /// </summary>
    public Func<TDeclaringType, TProperty>? Getter { get; }

    /// <summary>
    /// Gets the strongly-typed setter action.
    /// </summary>
    public Action<TDeclaringType, TProperty>? Setter { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlPropertyInfo{TDeclaringType, TProperty}"/> class.
    /// </summary>
    public YamlPropertyInfo(
        string name,
        string yamlPropertyName,
        Func<TDeclaringType, TProperty>? getter,
        Action<TDeclaringType, TProperty>? setter,
        bool isRequired = false,
        int order = 0,
        YamlIgnoreCondition? ignoreCondition = null)
    {
        Name = name;
        YamlPropertyName = yamlPropertyName;
        Getter = getter;
        Setter = setter;
        IsRequired = isRequired;
        Order = order;
        IgnoreCondition = ignoreCondition;

        // Create boxed versions for non-generic access
        if (getter != null)
        {
            Get = obj => getter((TDeclaringType)obj);
        }
        
        if (setter != null)
        {
            Set = (obj, value) => setter((TDeclaringType)obj, (TProperty)value!);
        }
    }
}
