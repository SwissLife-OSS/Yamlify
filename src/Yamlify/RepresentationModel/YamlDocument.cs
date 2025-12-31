namespace Yamlify.RepresentationModel;

/// <summary>
/// Represents a YAML document containing a single root node.
/// </summary>
public sealed class YamlDocument
{
    /// <summary>
    /// Gets or sets the root node of this document.
    /// </summary>
    public YamlNode? RootNode { get; set; }

    /// <summary>
    /// Gets or sets the YAML version directive (e.g., "1.2").
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets the tag directives declared in this document.
    /// </summary>
    public IList<TagDirective> TagDirectives { get; } = new List<TagDirective>();

    /// <summary>
    /// Gets whether the document has an explicit start marker (---).
    /// </summary>
    public bool HasExplicitStart { get; set; }

    /// <summary>
    /// Gets whether the document has an explicit end marker (...).
    /// </summary>
    public bool HasExplicitEnd { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlDocument"/> class.
    /// </summary>
    public YamlDocument()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlDocument"/> class with a root node.
    /// </summary>
    /// <param name="rootNode">The root node.</param>
    public YamlDocument(YamlNode rootNode)
    {
        RootNode = rootNode;
    }

    /// <summary>
    /// Creates a deep clone of this document.
    /// </summary>
    public YamlDocument DeepClone()
    {
        var clone = new YamlDocument
        {
            RootNode = RootNode?.DeepClone(),
            Version = Version,
            HasExplicitStart = HasExplicitStart,
            HasExplicitEnd = HasExplicitEnd
        };
        
        foreach (var tag in TagDirectives)
        {
            clone.TagDirectives.Add(tag);
        }
        
        return clone;
    }

    /// <summary>
    /// Accepts a visitor for traversing the document tree.
    /// </summary>
    public void Accept(IYamlVisitor visitor)
    {
        RootNode?.Accept(visitor);
    }
}

/// <summary>
/// Represents a tag directive in a YAML document.
/// </summary>
/// <param name="Handle">The tag handle (e.g., "!custom!").</param>
/// <param name="Prefix">The tag prefix (e.g., "tag:example.com,2023:").</param>
public readonly record struct TagDirective(string Handle, string Prefix);

/// <summary>
/// Represents a YAML stream containing multiple documents.
/// </summary>
public sealed class YamlStream : IList<YamlDocument>
{
    private readonly List<YamlDocument> _documents = new();

    /// <summary>
    /// Gets the number of documents in this stream.
    /// </summary>
    public int Count => _documents.Count;

    /// <inheritdoc/>
    bool ICollection<YamlDocument>.IsReadOnly => false;

    /// <summary>
    /// Gets or sets the document at the specified index.
    /// </summary>
    public YamlDocument this[int index]
    {
        get => _documents[index];
        set => _documents[index] = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlStream"/> class.
    /// </summary>
    public YamlStream()
    {
    }

    /// <summary>
    /// Initializes a new instance with documents.
    /// </summary>
    public YamlStream(IEnumerable<YamlDocument> documents)
    {
        _documents.AddRange(documents);
    }

    /// <summary>
    /// Initializes a new instance with documents.
    /// </summary>
    public YamlStream(params YamlDocument[] documents)
    {
        _documents.AddRange(documents);
    }

    /// <summary>
    /// Loads a YAML stream from a string.
    /// </summary>
    /// <param name="yaml">The YAML string to parse.</param>
    /// <returns>A new YamlStream containing the parsed documents.</returns>
    public static YamlStream Load(string yaml)
    {
        return Load(System.Text.Encoding.UTF8.GetBytes(yaml));
    }

    /// <summary>
    /// Loads a YAML stream from UTF-8 bytes.
    /// </summary>
    /// <param name="utf8Yaml">The UTF-8 encoded YAML to parse.</param>
    /// <returns>A new YamlStream containing the parsed documents.</returns>
    public static YamlStream Load(ReadOnlySpan<byte> utf8Yaml)
    {
        var stream = new YamlStream();
        var parser = new YamlDocumentParser();
        
        foreach (var doc in parser.ParseDocuments(utf8Yaml))
        {
            stream.Add(doc);
        }
        
        return stream;
    }

    /// <summary>
    /// Loads a YAML stream from a file.
    /// </summary>
    /// <param name="path">The path to the YAML file.</param>
    /// <returns>A new YamlStream containing the parsed documents.</returns>
    public static YamlStream LoadFromFile(string path)
    {
        var bytes = System.IO.File.ReadAllBytes(path);
        return Load(bytes);
    }

    /// <summary>
    /// Saves this stream to a string.
    /// </summary>
    public string Save()
    {
        using var stream = new System.IO.MemoryStream();
        Save(stream);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Saves this stream to a stream.
    /// </summary>
    public void Save(System.IO.Stream output)
    {
        using var writer = new Core.Utf8YamlWriter(output);
        var emitter = new YamlDocumentEmitter(writer);
        
        foreach (var doc in _documents)
        {
            emitter.Emit(doc);
        }
    }

    /// <summary>
    /// Saves this stream to a file.
    /// </summary>
    public void SaveToFile(string path)
    {
        using var stream = System.IO.File.Create(path);
        Save(stream);
    }

    /// <summary>
    /// Adds a document to the stream.
    /// </summary>
    public void Add(YamlDocument item) => _documents.Add(item);

    /// <summary>
    /// Inserts a document at the specified index.
    /// </summary>
    public void Insert(int index, YamlDocument item) => _documents.Insert(index, item);

    /// <summary>
    /// Removes the document at the specified index.
    /// </summary>
    public void RemoveAt(int index) => _documents.RemoveAt(index);

    /// <summary>
    /// Removes a document from the stream.
    /// </summary>
    public bool Remove(YamlDocument item) => _documents.Remove(item);

    /// <summary>
    /// Clears all documents from the stream.
    /// </summary>
    public void Clear() => _documents.Clear();

    /// <summary>
    /// Returns whether the stream contains the specified document.
    /// </summary>
    public bool Contains(YamlDocument item) => _documents.Contains(item);

    /// <summary>
    /// Returns the index of the specified document.
    /// </summary>
    public int IndexOf(YamlDocument item) => _documents.IndexOf(item);

    /// <inheritdoc/>
    void ICollection<YamlDocument>.CopyTo(YamlDocument[] array, int arrayIndex) => _documents.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public IEnumerator<YamlDocument> GetEnumerator() => _documents.GetEnumerator();

    /// <inheritdoc/>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Parser for building YamlDocument instances from YAML input.
/// </summary>
internal sealed class YamlDocumentParser
{
    private readonly Dictionary<string, YamlNode> _anchors = new();

    public IEnumerable<YamlDocument> ParseDocuments(ReadOnlySpan<byte> utf8Yaml)
    {
        var reader = new Core.Utf8YamlReader(utf8Yaml);
        var documents = new List<YamlDocument>();
        
        while (reader.Read())
        {
            if (reader.TokenType == Core.YamlTokenType.DocumentStart ||
                reader.TokenType == Core.YamlTokenType.Scalar ||
                reader.TokenType == Core.YamlTokenType.MappingStart ||
                reader.TokenType == Core.YamlTokenType.SequenceStart)
            {
                var doc = ParseDocument(ref reader);
                documents.Add(doc);
            }
        }
        
        // If no documents were found but there was content, create one document
        if (documents.Count == 0)
        {
            // Reset and try to parse as implicit document
            reader = new Core.Utf8YamlReader(utf8Yaml);
            if (reader.Read())
            {
                var doc = ParseDocument(ref reader);
                documents.Add(doc);
            }
        }
        
        return documents;
    }

    private YamlDocument ParseDocument(ref Core.Utf8YamlReader reader)
    {
        var document = new YamlDocument();
        _anchors.Clear();
        
        if (reader.TokenType == Core.YamlTokenType.DocumentStart)
        {
            document.HasExplicitStart = true;
            if (!reader.Read())
            {
                return document;
            }
        }
        
        document.RootNode = ParseNode(ref reader);
        
        if (reader.TokenType == Core.YamlTokenType.DocumentEnd)
        {
            document.HasExplicitEnd = true;
        }
        
        return document;
    }

    private YamlNode ParseNode(ref Core.Utf8YamlReader reader)
    {
        string? anchor = null;
        string? tag = null;
        
        // Handle anchor and tag
        while (reader.TokenType == Core.YamlTokenType.Anchor || 
               reader.TokenType == Core.YamlTokenType.Tag)
        {
            if (reader.TokenType == Core.YamlTokenType.Anchor)
            {
                anchor = reader.GetString();
            }
            else if (reader.TokenType == Core.YamlTokenType.Tag)
            {
                tag = reader.GetString();
            }
            
            if (!reader.Read())
            {
                break;
            }
        }
        
        YamlNode node = reader.TokenType switch
        {
            Core.YamlTokenType.Scalar => ParseScalar(ref reader),
            Core.YamlTokenType.MappingStart => ParseMapping(ref reader),
            Core.YamlTokenType.SequenceStart => ParseSequence(ref reader),
            Core.YamlTokenType.Alias => ParseAlias(ref reader),
            _ => new YamlScalarNode(null)
        };
        
        node.Anchor = anchor;
        node.Tag = tag;
        
        if (anchor != null)
        {
            _anchors[anchor] = node;
        }
        
        return node;
    }

    private YamlScalarNode ParseScalar(ref Core.Utf8YamlReader reader)
    {
        var node = new YamlScalarNode(reader.GetString())
        {
            Start = reader.TokenStart,
            End = reader.Position
        };
        
        reader.Read(); // Move past scalar
        return node;
    }

    private YamlMappingNode ParseMapping(ref Core.Utf8YamlReader reader)
    {
        var node = new YamlMappingNode
        {
            Start = reader.TokenStart
        };
        
        reader.Read(); // Move past MappingStart
        
        while (reader.TokenType != Core.YamlTokenType.MappingEnd &&
               reader.TokenType != Core.YamlTokenType.None)
        {
            var key = ParseNode(ref reader);
            var value = ParseNode(ref reader);
            node.Add(key, value);
        }
        
        node.End = reader.Position;
        reader.Read(); // Move past MappingEnd
        
        return node;
    }

    private YamlSequenceNode ParseSequence(ref Core.Utf8YamlReader reader)
    {
        var node = new YamlSequenceNode
        {
            Start = reader.TokenStart
        };
        
        reader.Read(); // Move past SequenceStart
        
        while (reader.TokenType != Core.YamlTokenType.SequenceEnd &&
               reader.TokenType != Core.YamlTokenType.None)
        {
            var item = ParseNode(ref reader);
            node.Add(item);
        }
        
        node.End = reader.Position;
        reader.Read(); // Move past SequenceEnd
        
        return node;
    }

    private YamlNode ParseAlias(ref Core.Utf8YamlReader reader)
    {
        var aliasName = reader.GetString() ?? "";
        
        if (_anchors.TryGetValue(aliasName, out var referencedNode))
        {
            reader.Read();
            return referencedNode.DeepClone();
        }
        
        var aliasNode = new YamlAliasNode(aliasName)
        {
            Start = reader.TokenStart,
            End = reader.Position
        };
        
        reader.Read();
        return aliasNode;
    }
}

/// <summary>
/// Emitter for writing YamlDocument instances to YAML output.
/// </summary>
internal sealed class YamlDocumentEmitter
{
    private readonly Core.Utf8YamlWriter _writer;
    private readonly HashSet<YamlNode> _visitedNodes = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<YamlNode, string> _anchors = new(ReferenceEqualityComparer.Instance);
    private int _anchorCounter;

    public YamlDocumentEmitter(Core.Utf8YamlWriter writer)
    {
        _writer = writer;
    }

    public void Emit(YamlDocument document)
    {
        _visitedNodes.Clear();
        _anchors.Clear();
        _anchorCounter = 0;
        
        // First pass: detect circular references and assign anchors
        if (document.RootNode != null)
        {
            DetectAnchors(document.RootNode);
        }
        
        _writer.WriteDocumentStart();
        
        if (document.RootNode != null)
        {
            EmitNode(document.RootNode);
        }
        
        _writer.WriteDocumentEnd();
    }

    private void DetectAnchors(YamlNode node)
    {
        if (_visitedNodes.Contains(node))
        {
            // Circular reference detected - assign anchor if not already assigned
            if (!_anchors.ContainsKey(node))
            {
                _anchors[node] = $"anchor{++_anchorCounter}";
            }
            return;
        }
        
        _visitedNodes.Add(node);
        
        switch (node)
        {
            case YamlSequenceNode seq:
                foreach (var item in seq)
                {
                    DetectAnchors(item);
                }
                break;
                
            case YamlMappingNode map:
                foreach (var kvp in map)
                {
                    DetectAnchors(kvp.Key);
                    DetectAnchors(kvp.Value);
                }
                break;
        }
    }

    private void EmitNode(YamlNode node)
    {
        // Check if this node needs an alias reference
        if (_anchors.TryGetValue(node, out var anchorName))
        {
            if (_visitedNodes.Contains(node))
            {
                _writer.WriteAlias(anchorName);
                return;
            }
            
            _writer.WriteAnchor(anchorName);
        }
        else if (node.Anchor != null)
        {
            _writer.WriteAnchor(node.Anchor);
        }
        
        if (node.Tag != null)
        {
            _writer.WriteTag(node.Tag);
        }
        
        _visitedNodes.Add(node);
        
        switch (node)
        {
            case YamlScalarNode scalar:
                EmitScalar(scalar);
                break;
                
            case YamlSequenceNode seq:
                EmitSequence(seq);
                break;
                
            case YamlMappingNode map:
                EmitMapping(map);
                break;
                
            case YamlAliasNode alias:
                _writer.WriteAlias(alias.AnchorName);
                break;
        }
    }

    private void EmitScalar(YamlScalarNode scalar)
    {
        if (scalar.IsNull)
        {
            _writer.WriteNull();
        }
        else
        {
            _writer.WriteString(scalar.Value);
        }
    }

    private void EmitSequence(YamlSequenceNode seq)
    {
        _writer.WriteSequenceStart(seq.Style);
        
        foreach (var item in seq)
        {
            EmitNode(item);
        }
        
        _writer.WriteSequenceEnd();
    }

    private void EmitMapping(YamlMappingNode map)
    {
        _writer.WriteMappingStart(map.Style);
        
        foreach (var kvp in map)
        {
            if (kvp.Key is YamlScalarNode key)
            {
                _writer.WritePropertyName(key.Value ?? "");
            }
            else
            {
                // Complex keys - rare but valid in YAML
                _writer.WritePropertyName(kvp.Key.ToString() ?? "");
            }
            
            EmitNode(kvp.Value);
        }
        
        _writer.WriteMappingEnd();
    }
}
