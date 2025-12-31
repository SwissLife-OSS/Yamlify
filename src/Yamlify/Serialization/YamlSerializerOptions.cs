using Yamlify.Schema;

namespace Yamlify.Serialization;

/// <summary>
/// Provides options to be used with <see cref="YamlSerializer"/>.
/// </summary>
public sealed class YamlSerializerOptions
{
    private static YamlSerializerOptions? _default;
    private static readonly object _defaultLock = new();

    /// <summary>
    /// Gets a singleton instance of <see cref="YamlSerializerOptions"/> with default configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike other instances, the <see cref="Default"/> singleton allows setting <see cref="TypeInfoResolver"/>
    /// once before first use. After the first serialization/deserialization operation, it becomes read-only.
    /// </para>
    /// <para>
    /// This enables a simplified API pattern where you set the resolver once at startup:
    /// <code>
    /// YamlSerializerOptions.Default.TypeInfoResolver = MyContext.Default;
    /// var yaml = YamlSerializer.Serialize(myObject); // No context needed!
    /// </code>
    /// </para>
    /// </remarks>
    public static YamlSerializerOptions Default
    {
        get
        {
            if (_default is null)
            {
                lock (_defaultLock)
                {
                    _default ??= new YamlSerializerOptions { _isDefault = true, IsReadOnly = true };
                }
            }
            return _default;
        }
    }

    private readonly List<YamlConverter> _converters = new();
    private IYamlSchema _schema = CoreSchema.Instance;
    private YamlNamingPolicy? _propertyNamingPolicy;
    private int _maxDepth = YamlSerializerDefaults.DefaultMaxDepth;
    private bool _writeIndented = true;
    private int _indentSize = 2;
    private bool _ignoreNullValues;
    private bool _ignoreEmptyObjects;
    private bool _ignoreReadOnlyProperties;
    private bool _includeFields;
    private bool _allowTrailingCommas;
    private bool _readComments;
    private bool _writeComments;
    private bool _preferFlowStyle;
    private bool _indentSequenceItems = true;
    private ScalarStyle _defaultScalarStyle = ScalarStyle.Any;
    private ReferenceHandler? _referenceHandler;
    private IYamlTypeInfoResolver? _typeInfoResolver;
    private EmptyCollectionHandling _emptyCollectionHandling = EmptyCollectionHandling.Default;
    private bool _isDefault;
    private bool _hasBeenUsed;
    
    /// <summary>
    /// The current reference resolver for the ongoing serialization operation.
    /// This is set internally during serialization and should not be modified externally.
    /// </summary>
    [ThreadStatic]
    private static ReferenceResolver? _currentResolver;

    /// <summary>
    /// Gets or sets whether this instance is read-only.
    /// </summary>
    internal bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets the list of custom converters.
    /// </summary>
    public IList<YamlConverter> Converters => _converters;

    /// <summary>
    /// Gets or sets the YAML schema to use for tag resolution.
    /// </summary>
    public IYamlSchema Schema
    {
        get => _schema;
        set
        {
            ThrowIfReadOnly();
            _schema = value ?? CoreSchema.Instance;
        }
    }

    /// <summary>
    /// Gets or sets the naming policy for converting property names.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="YamlNamingPolicy.KebabCase"/> to match YAML ecosystem conventions.
    /// </remarks>
    public YamlNamingPolicy? PropertyNamingPolicy
    {
        get => _propertyNamingPolicy;
        set
        {
            ThrowIfReadOnly();
            _propertyNamingPolicy = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum depth allowed when reading or writing YAML.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property provides protection against stack overflow or memory exhaustion when 
    /// processing deeply nested or circular YAML structures. When the depth limit is exceeded,
    /// a <see cref="Exceptions.MaxRecursionDepthExceededException"/> is thrown.
    /// </para>
    /// <para>
    /// The default value is 64. The maximum allowed value is 1000.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Allow deeper nesting for complex configurations
    /// var options = new YamlSerializerOptions { MaxDepth = 128 };
    /// var result = YamlSerializer.Deserialize&lt;Config&gt;(yaml, options);
    /// </code>
    /// </example>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is less than or equal to 0, or greater than 1000.
    /// </exception>
    public int MaxDepth
    {
        get => _maxDepth;
        set
        {
            ThrowIfReadOnly();
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, 
                    "MaxDepth must be greater than 0.");
            }
            if (value > YamlSerializerDefaults.MaxAllowedDepth)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    $"MaxDepth cannot exceed {YamlSerializerDefaults.MaxAllowedDepth}.");
            }
            _maxDepth = value;
        }
    }

    /// <summary>
    /// Gets or sets whether the YAML should be written with indentation.
    /// </summary>
    public bool WriteIndented
    {
        get => _writeIndented;
        set
        {
            ThrowIfReadOnly();
            _writeIndented = value;
        }
    }

    /// <summary>
    /// Gets or sets the number of spaces to use for indentation.
    /// </summary>
    public int IndentSize
    {
        get => _indentSize;
        set
        {
            ThrowIfReadOnly();
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _indentSize = value;
        }
    }

    /// <summary>
    /// Gets or sets whether null values should be ignored during serialization.
    /// </summary>
    public bool IgnoreNullValues
    {
        get => _ignoreNullValues;
        set
        {
            ThrowIfReadOnly();
            _ignoreNullValues = value;
        }
    }

    /// <summary>
    /// Gets or sets whether objects that would serialize to empty mappings should be ignored.
    /// </summary>
    /// <remarks>
    /// When true, if an object has no non-null properties (after applying IgnoreNullValues),
    /// the entire property is omitted from the output.
    /// </remarks>
    public bool IgnoreEmptyObjects
    {
        get => _ignoreEmptyObjects;
        set
        {
            ThrowIfReadOnly();
            _ignoreEmptyObjects = value;
        }
    }

    /// <summary>
    /// Gets or sets whether read-only properties should be ignored during serialization.
    /// </summary>
    public bool IgnoreReadOnlyProperties
    {
        get => _ignoreReadOnlyProperties;
        set
        {
            ThrowIfReadOnly();
            _ignoreReadOnlyProperties = value;
        }
    }

    /// <summary>
    /// Gets or sets whether fields should be included during serialization.
    /// </summary>
    public bool IncludeFields
    {
        get => _includeFields;
        set
        {
            ThrowIfReadOnly();
            _includeFields = value;
        }
    }

    /// <summary>
    /// Gets or sets whether trailing commas in flow collections are allowed.
    /// </summary>
    public bool AllowTrailingCommas
    {
        get => _allowTrailingCommas;
        set
        {
            ThrowIfReadOnly();
            _allowTrailingCommas = value;
        }
    }

    /// <summary>
    /// Gets or sets whether comments should be read.
    /// </summary>
    public bool ReadComments
    {
        get => _readComments;
        set
        {
            ThrowIfReadOnly();
            _readComments = value;
        }
    }

    /// <summary>
    /// Gets or sets whether comments should be written.
    /// </summary>
    public bool WriteComments
    {
        get => _writeComments;
        set
        {
            ThrowIfReadOnly();
            _writeComments = value;
        }
    }

    /// <summary>
    /// Gets or sets whether flow style should be preferred over block style.
    /// </summary>
    public bool PreferFlowStyle
    {
        get => _preferFlowStyle;
        set
        {
            ThrowIfReadOnly();
            _preferFlowStyle = value;
        }
    }

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
    public bool IndentSequenceItems
    {
        get => _indentSequenceItems;
        set
        {
            ThrowIfReadOnly();
            _indentSequenceItems = value;
        }
    }

    /// <summary>
    /// Gets or sets the default scalar style.
    /// </summary>
    public ScalarStyle DefaultScalarStyle
    {
        get => _defaultScalarStyle;
        set
        {
            ThrowIfReadOnly();
            _defaultScalarStyle = value;
        }
    }

    /// <summary>
    /// Gets or sets how object references are handled.
    /// </summary>
    public ReferenceHandler? ReferenceHandler
    {
        get => _referenceHandler;
        set
        {
            ThrowIfReadOnly();
            _referenceHandler = value;
        }
    }

    /// <summary>
    /// Gets or sets the type info resolver for the source-generated serialization context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On the <see cref="Default"/> singleton, this property can be set once before any serialization
    /// operation. This enables a simplified API where the context doesn't need to be passed explicitly:
    /// </para>
    /// <code>
    /// // Set once at startup
    /// YamlSerializerOptions.Default.TypeInfoResolver = MyContext.Default;
    /// 
    /// // Then use simple overloads everywhere
    /// var yaml = YamlSerializer.Serialize(myObject);
    /// var obj = YamlSerializer.Deserialize&lt;MyType&gt;(yaml);
    /// </code>
    /// </remarks>
    public IYamlTypeInfoResolver? TypeInfoResolver
    {
        get => _typeInfoResolver;
        set
        {
            // Special case: allow setting TypeInfoResolver on Default before first use
            if (_isDefault && !_hasBeenUsed)
            {
                _typeInfoResolver = value;
                return;
            }
            ThrowIfReadOnly();
            _typeInfoResolver = value;
        }
    }

    /// <summary>
    /// Marks this options instance as having been used for serialization/deserialization.
    /// After this, the Default instance becomes read-only.
    /// </summary>
    internal void MarkAsUsed()
    {
        if (_isDefault)
        {
            _hasBeenUsed = true;
            IsReadOnly = true;
        }
    }

    /// <summary>
    /// Gets or sets how empty collections are handled during serialization and deserialization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set to <see cref="EmptyCollectionHandling.PreferEmptyCollection"/>, null values
    /// for collection types (arrays, lists) will be deserialized as empty collections instead of null.
    /// </para>
    /// <para>
    /// This is particularly useful when working with YAML files where an empty value (key: )
    /// represents null per the YAML spec, but you want arrays to always be initialized.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new YamlSerializerOptions 
    /// { 
    ///     EmptyCollectionHandling = EmptyCollectionHandling.PreferEmptyCollection 
    /// };
    /// // Now "items: " in YAML will deserialize to an empty array instead of null
    /// </code>
    /// </example>
    public EmptyCollectionHandling EmptyCollectionHandling
    {
        get => _emptyCollectionHandling;
        set
        {
            ThrowIfReadOnly();
            _emptyCollectionHandling = value;
        }
    }

    /// <summary>
    /// Gets the current reference resolver for cycle detection during serialization.
    /// Returns null if no ReferenceHandler is configured or serialization is not in progress.
    /// </summary>
    public static ReferenceResolver? CurrentResolver => _currentResolver;

    /// <summary>
    /// Begins a serialization operation with reference tracking if a ReferenceHandler is configured.
    /// </summary>
    /// <returns>A disposable scope that cleans up the resolver when disposed.</returns>
    internal ReferenceResolverScope BeginSerialize()
    {
        var resolver = _referenceHandler?.CreateResolver();
        _currentResolver = resolver;
        return new ReferenceResolverScope(resolver);
    }

    /// <summary>
    /// Clears the current reference resolver. Called when serialization scope ends.
    /// </summary>
    internal static void ClearCurrentResolver()
    {
        _currentResolver = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSerializerOptions"/> class.
    /// </summary>
    public YamlSerializerOptions()
    {
        // Default to kebab-case to match YAML ecosystem conventions
        _propertyNamingPolicy = YamlNamingPolicy.KebabCase;
    }

    /// <summary>
    /// Initializes a new instance by copying from another instance.
    /// </summary>
    /// <param name="options">The options to copy.</param>
    public YamlSerializerOptions(YamlSerializerOptions options)
    {
        _converters = new List<YamlConverter>(options._converters);
        _schema = options._schema;
        _propertyNamingPolicy = options._propertyNamingPolicy;
        _maxDepth = options._maxDepth;
        _writeIndented = options._writeIndented;
        _indentSize = options._indentSize;
        _ignoreNullValues = options._ignoreNullValues;
        _ignoreEmptyObjects = options._ignoreEmptyObjects;
        _ignoreReadOnlyProperties = options._ignoreReadOnlyProperties;
        _includeFields = options._includeFields;
        _allowTrailingCommas = options._allowTrailingCommas;
        _readComments = options._readComments;
        _writeComments = options._writeComments;
        _preferFlowStyle = options._preferFlowStyle;
        _indentSequenceItems = options._indentSequenceItems;
        _defaultScalarStyle = options._defaultScalarStyle;
        _referenceHandler = options._referenceHandler;
        _typeInfoResolver = options._typeInfoResolver;
        _emptyCollectionHandling = options._emptyCollectionHandling;
    }

    /// <summary>
    /// Gets a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The type to get a converter for.</param>
    /// <returns>A converter for the type, or null if none is found.</returns>
    public YamlConverter? GetConverter(Type typeToConvert)
    {
        // Check custom converters first
        foreach (var converter in _converters)
        {
            if (converter.CanConvert(typeToConvert))
            {
                if (converter is YamlConverterFactory factory)
                {
                    return factory.CreateConverter(typeToConvert, this);
                }
                return converter;
            }
        }

        // Check type info resolver
        if (_typeInfoResolver != null)
        {
            var typeInfo = _typeInfoResolver.GetTypeInfo(typeToConvert, this);
            if (typeInfo?.Converter != null)
            {
                return typeInfo.Converter;
            }
        }

        return null;
    }

    private void ThrowIfReadOnly()
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException("YamlSerializerOptions instance is read-only.");
        }
    }
}
