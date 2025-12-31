namespace Yamlify.RepresentationModel;

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
