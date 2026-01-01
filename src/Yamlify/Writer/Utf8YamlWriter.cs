using System.Buffers;
using System.Text;

namespace Yamlify;

/// <summary>
/// A high-performance, forward-only writer for UTF-8 encoded YAML text.
/// </summary>
/// <remarks>
/// <para>
/// This writer writes directly to an <see cref="IBufferWriter{T}"/> for optimal performance.
/// </para>
/// <para>
/// The writer produces YAML 1.2 compliant output.
/// </para>
/// </remarks>
public sealed class Utf8YamlWriter : IDisposable
{
    private readonly IBufferWriter<byte> _output;
    private readonly YamlWriterOptions _options;
    private readonly bool _ownsOutput;
    
    private int _currentDepth;
    private bool _needsNewLine;
    private bool _inFlowContext;
    private bool _afterPropertyName; // True if we just wrote a property name and need a value
    private bool _afterSequenceEntry; // True if we just wrote "- " and the first property should not be indented
    private long _bytesWritten;
    private bool _isDisposed;
    
    // State tracking for proper formatting
    private WriterState _state;
    private readonly Stack<ContainerInfo> _containerStack;

    /// <summary>
    /// Gets the number of bytes written so far.
    /// </summary>
    public long BytesWritten => _bytesWritten;

    /// <summary>
    /// Gets the current depth of nesting.
    /// </summary>
    public int CurrentDepth => _currentDepth;

    /// <summary>
    /// Gets the options used by this writer.
    /// </summary>
    public YamlWriterOptions Options => _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8YamlWriter"/> class.
    /// </summary>
    /// <param name="output">The buffer writer to write to.</param>
    /// <param name="options">The writer options.</param>
    public Utf8YamlWriter(IBufferWriter<byte> output, YamlWriterOptions? options = null)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _options = options ?? YamlWriterOptions.Default;
        _ownsOutput = false;
        _currentDepth = 0;
        _needsNewLine = false;
        _inFlowContext = false;
        _afterPropertyName = false;
        _afterSequenceEntry = false;
        _bytesWritten = 0;
        _isDisposed = false;
        _state = WriterState.Initial;
        _containerStack = new Stack<ContainerInfo>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8YamlWriter"/> class that writes to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="options">The writer options.</param>
    public Utf8YamlWriter(Stream stream, YamlWriterOptions? options = null)
        : this(new StreamBufferWriter(stream), options)
    {
        _ownsOutput = true;
    }

    /// <summary>
    /// Writes the YAML stream start. This is implicitly called on first write.
    /// </summary>
    public void WriteStreamStart()
    {
        if (_options.EmitYamlDirective)
        {
            WriteRaw("%YAML 1.2"u8);
            WriteNewLine();
        }
        _state = WriterState.InStream;
    }

    /// <summary>
    /// Writes the YAML stream end.
    /// </summary>
    public void WriteStreamEnd()
    {
        Flush();
        _state = WriterState.Finished;
    }

    /// <summary>
    /// Writes a document start marker (---).
    /// </summary>
    public void WriteDocumentStart()
    {
        EnsureStreamStarted();
        
        if (_options.EmitDocumentMarkers)
        {
            if (_needsNewLine)
            {
                WriteNewLine();
            }
            WriteRaw("---"u8);
            _needsNewLine = true;
        }
        
        _state = WriterState.InDocument;
    }

    /// <summary>
    /// Writes a document end marker (...).
    /// </summary>
    public void WriteDocumentEnd()
    {
        if (_needsNewLine)
        {
            WriteNewLine();
        }
        
        if (_options.EmitDocumentMarkers)
        {
            WriteRaw("..."u8);
            WriteNewLine();
        }
        
        _state = WriterState.InStream;
    }

    /// <summary>
    /// Writes the start of a mapping (block style by default).
    /// </summary>
    /// <param name="style">The collection style to use.</param>
    public void WriteMappingStart(CollectionStyle style = CollectionStyle.Block)
    {
        EnsureDocumentStarted();
        
        // Clear afterPropertyName flag - nested containers don't need a space
        _afterPropertyName = false;
        
        // Track if we're starting a mapping directly after a sequence entry
        bool afterSequenceEntry = false;
        
        // If we're inside a block sequence, emit the "- " prefix first
        if (!_inFlowContext && _containerStack.Count > 0 && 
            _containerStack.Peek().Type == ContainerType.BlockSequence)
        {
            WriteBlockSequenceEntry();
            afterSequenceEntry = true;
        }
        else
        {
            WriteIndentIfNeeded();
        }

        if (style == CollectionStyle.Flow || _options.PreferFlowStyle)
        {
            WriteRaw((byte)'{');
            _inFlowContext = true;
            _containerStack.Push(new ContainerInfo(ContainerType.FlowMapping, _currentDepth, true));
        }
        else
        {
            _containerStack.Push(new ContainerInfo(ContainerType.BlockMapping, _currentDepth, true));
            // Only set needsNewLine if we're not at the root level AND not after a sequence entry
            // At root level, the first property should start immediately without a leading newline
            // After a sequence entry (- ), the first property should follow on the same line:
            //   - name: value
            if (_currentDepth > 0 && !afterSequenceEntry)
            {
                _needsNewLine = true;
            }
        }
        
        _currentDepth++;
    }

    /// <summary>
    /// Writes the end of a mapping.
    /// </summary>
    public void WriteMappingEnd()
    {
        _currentDepth--;
        
        if (_containerStack.TryPop(out var container))
        {
            if (container.Type == ContainerType.FlowMapping)
            {
                WriteRaw((byte)'}');
                _inFlowContext = _containerStack.Count > 0 && 
                    _containerStack.Peek().Type is ContainerType.FlowMapping or ContainerType.FlowSequence;
                
                // If we're returning to block context, signal that a newline is needed
                if (!_inFlowContext)
                {
                    _needsNewLine = true;
                }
            }
        }
    }

    /// <summary>
    /// Writes the start of a sequence (block style by default).
    /// </summary>
    /// <param name="style">The collection style to use.</param>
    public void WriteSequenceStart(CollectionStyle style = CollectionStyle.Block)
    {
        EnsureDocumentStarted();
        
        // For flow sequences after a property name, add a space first (e.g., "key: []")
        var needsSpaceBeforeFlow = _afterPropertyName && (style == CollectionStyle.Flow || _options.PreferFlowStyle);
        
        // Clear afterPropertyName flag - nested containers don't need a space
        _afterPropertyName = false;
        
        // If we're inside a block sequence, emit the "- " prefix first
        if (!_inFlowContext && _containerStack.Count > 0 && 
            _containerStack.Peek().Type == ContainerType.BlockSequence)
        {
            WriteBlockSequenceEntry();
        }
        else
        {
            WriteIndentIfNeeded();
        }

        if (style == CollectionStyle.Flow || _options.PreferFlowStyle)
        {
            if (needsSpaceBeforeFlow)
            {
                WriteRaw((byte)' ');
            }
            WriteRaw((byte)'[');
            _inFlowContext = true;
            _containerStack.Push(new ContainerInfo(ContainerType.FlowSequence, _currentDepth, true));
        }
        else
        {
            _containerStack.Push(new ContainerInfo(ContainerType.BlockSequence, _currentDepth, true));
            _needsNewLine = true;
        }
        
        // For block sequences with IndentSequenceItems = false (compact style),
        // don't increment depth since items are at the same level as the parent key.
        // The mapping inside will still increment depth for its properties.
        if (_inFlowContext || _options.IndentSequenceItems)
        {
            _currentDepth++;
        }
    }

    /// <summary>
    /// Writes the end of a sequence.
    /// </summary>
    public void WriteSequenceEnd()
    {
        if (_containerStack.TryPop(out var container))
        {
            // Only decrement depth if we incremented it in WriteSequenceStart
            // For compact block sequences, we didn't increment
            var wasFlowSequence = container.Type == ContainerType.FlowSequence;
            if (wasFlowSequence || _options.IndentSequenceItems)
            {
                _currentDepth--;
            }
            
            if (wasFlowSequence)
            {
                WriteRaw((byte)']');
                _inFlowContext = _containerStack.Count > 0 && 
                    _containerStack.Peek().Type is ContainerType.FlowMapping or ContainerType.FlowSequence;
                
                // If we're returning to block context, signal that a newline is needed
                if (!_inFlowContext)
                {
                    _needsNewLine = true;
                }
            }
        }
    }

    /// <summary>
    /// Writes a mapping key property name.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    public void WritePropertyName(string propertyName)
    {
        WritePropertyName(propertyName.AsSpan());
    }

    /// <summary>
    /// Writes a mapping key property name.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    public void WritePropertyName(ReadOnlySpan<char> propertyName)
    {
        if (_inFlowContext)
        {
            if (!IsFirstInContainer())
            {
                WriteRaw(", "u8);
            }
            WriteScalarValue(propertyName);
            WriteRaw(": "u8);
        }
        else
        {
            WriteBlockMappingKey();
            WriteScalarValue(propertyName);
            WriteRaw((byte)':');
            _afterPropertyName = true; // Signal that we're expecting a value next
            // Don't write trailing space here - scalar values will add it if needed
            // This allows block containers to start on the next line without a trailing space
        }
        
        SetNotFirstInContainer();
    }

    /// <summary>
    /// Writes a null value.
    /// </summary>
    public void WriteNull()
    {
        WriteScalarPrelude();
        WriteRaw("null"u8);
        WriteScalarPostlude();
    }

    /// <summary>
    /// Writes a boolean value.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteBoolean(bool value)
    {
        WriteScalarPrelude();
        WriteRaw(value ? "true"u8 : "false"u8);
        WriteScalarPostlude();
    }

    /// <summary>
    /// Writes an integer value.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteNumber(int value)
    {
        WriteScalarPrelude();
        Span<byte> buffer = stackalloc byte[11]; // Max length for int32
        if (Utf8Formatter.TryFormat(value, buffer, out int bytesWritten))
        {
            WriteRaw(buffer[..bytesWritten]);
        }
        WriteScalarPostlude();
    }

    /// <summary>
    /// Writes a long integer value.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteNumber(long value)
    {
        WriteScalarPrelude();
        Span<byte> buffer = stackalloc byte[20]; // Max length for int64
        if (Utf8Formatter.TryFormat(value, buffer, out int bytesWritten))
        {
            WriteRaw(buffer[..bytesWritten]);
        }
        WriteScalarPostlude();
    }

    /// <summary>
    /// Writes a double-precision floating-point value.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteNumber(double value)
    {
        WriteScalarPrelude();
        
        if (double.IsPositiveInfinity(value))
        {
            WriteRaw(".inf"u8);
        }
        else if (double.IsNegativeInfinity(value))
        {
            WriteRaw("-.inf"u8);
        }
        else if (double.IsNaN(value))
        {
            WriteRaw(".nan"u8);
        }
        else
        {
            Span<byte> buffer = stackalloc byte[32];
            if (Utf8Formatter.TryFormat(value, buffer, out int bytesWritten))
            {
                WriteRaw(buffer[..bytesWritten]);
            }
        }
        
        WriteScalarPostlude();
    }

    /// <summary>
    /// Writes a string value with automatic quoting if necessary.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteString(string? value)
    {
        if (value is null)
        {
            WriteNull();
            return;
        }
        
        WriteString(value.AsSpan());
    }

    /// <summary>
    /// Writes a string value with automatic quoting if necessary.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteString(ReadOnlySpan<char> value)
    {
        WriteScalarPrelude();
        WriteScalarValue(value);
        WriteScalarPostlude();
    }

    /// <summary>
    /// Writes an anchor definition.
    /// </summary>
    /// <param name="anchorName">The anchor name.</param>
    public void WriteAnchor(string anchorName)
    {
        WriteRaw((byte)'&');
        WriteRaw(Encoding.UTF8.GetBytes(anchorName));
        WriteRaw((byte)' ');
    }

    /// <summary>
    /// Writes an alias reference.
    /// </summary>
    /// <param name="anchorName">The anchor name to reference.</param>
    public void WriteAlias(string anchorName)
    {
        WriteScalarPrelude();
        WriteRaw((byte)'*');
        WriteRaw(Encoding.UTF8.GetBytes(anchorName));
        WriteScalarPostlude();
    }

    /// <summary>
    /// Writes a tag.
    /// </summary>
    /// <param name="tag">The tag to write.</param>
    public void WriteTag(string tag)
    {
        WriteRaw((byte)'!');
        WriteRaw(Encoding.UTF8.GetBytes(tag));
        WriteRaw((byte)' ');
    }

    /// <summary>
    /// Writes a comment.
    /// </summary>
    /// <param name="comment">The comment text.</param>
    public void WriteComment(string comment)
    {
        WriteRaw(" # "u8);
        WriteRaw(Encoding.UTF8.GetBytes(comment));
    }

    /// <summary>
    /// Writes a literal block scalar.
    /// </summary>
    /// <param name="value">The multi-line string value.</param>
    public void WriteLiteralScalar(string value)
    {
        WriteScalarPrelude();
        WriteRaw((byte)'|');
        WriteNewLine();
        
        var lines = value.Split('\n');
        foreach (var line in lines)
        {
            WriteIndent(_currentDepth);
            WriteRaw(Encoding.UTF8.GetBytes(line.TrimEnd('\r')));
            WriteNewLine();
        }
    }

    /// <summary>
    /// Writes a folded block scalar.
    /// </summary>
    /// <param name="value">The multi-line string value.</param>
    public void WriteFoldedScalar(string value)
    {
        WriteScalarPrelude();
        WriteRaw((byte)'>');
        WriteNewLine();
        
        var lines = value.Split('\n');
        foreach (var line in lines)
        {
            WriteIndent(_currentDepth);
            WriteRaw(Encoding.UTF8.GetBytes(line.TrimEnd('\r')));
            WriteNewLine();
        }
    }

    /// <summary>
    /// Flushes any buffered data to the underlying output.
    /// </summary>
    public void Flush()
    {
        if (_output is StreamBufferWriter sbw)
        {
            sbw.Flush();
        }
    }

    /// <summary>
    /// Resets the writer to be reused with new output.
    /// </summary>
    public void Reset()
    {
        _currentDepth = 0;
        _needsNewLine = false;
        _inFlowContext = false;
        _afterPropertyName = false;
        _afterSequenceEntry = false;
        _bytesWritten = 0;
        _state = WriterState.Initial;
        _containerStack.Clear();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed) return;
        
        Flush();
        
        if (_ownsOutput && _output is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        _isDisposed = true;
    }

    // Helper methods

    private void EnsureStreamStarted()
    {
        if (_state == WriterState.Initial)
        {
            WriteStreamStart();
        }
    }

    private void EnsureDocumentStarted()
    {
        EnsureStreamStarted();
        
        if (_state == WriterState.InStream)
        {
            WriteDocumentStart();
        }
    }

    private void WriteIndentIfNeeded()
    {
        if (_needsNewLine && !_inFlowContext)
        {
            WriteNewLine();
            WriteIndent(_currentDepth);
            _needsNewLine = false;
        }
    }

    private void WriteBlockMappingKey()
    {
        if (_needsNewLine)
        {
            WriteNewLine();
        }
        
        // If we just wrote "- " for a sequence entry, skip indentation for the first property
        // This gives us "- name: value" instead of "-     name: value"
        if (_afterSequenceEntry)
        {
            _afterSequenceEntry = false;
            _needsNewLine = false;
            return;
        }
        
        // Indent based on depth-1 since depth was incremented when entering the container
        // At root level (depth 1), this results in 0 indentation which is correct
        if (_currentDepth > 1)
        {
            WriteIndent(_currentDepth - 1);
        }
        _needsNewLine = false;
    }

    private void WriteBlockSequenceEntry()
    {
        if (_needsNewLine)
        {
            WriteNewLine();
        }
        // For indented style, depth was incremented so use depth-1.
        // For compact style, depth was NOT incremented so use depth-1 as well.
        // In both cases, depth-1 gives us the correct indent level:
        // - Indented: sequence at depth 2 → dash at depth 1 (indented from parent)
        // - Compact: sequence at depth 1 → dash at depth 0 (same level as parent)
        var indentDepth = _currentDepth - 1;
        if (indentDepth > 0)
        {
            WriteIndent(indentDepth);
        }
        WriteRaw("- "u8);
        _needsNewLine = false;
        _afterSequenceEntry = true; // Signal that the next property should not be indented
    }

    private void WriteScalarPrelude()
    {
        if (_inFlowContext)
        {
            if (!IsFirstInContainer())
            {
                WriteRaw(", "u8);
            }
        }
        else if (_containerStack.Count > 0 && 
                 _containerStack.Peek().Type == ContainerType.BlockSequence)
        {
            WriteBlockSequenceEntry();
            // For scalars, reset the flag since we're not entering a mapping
            _afterSequenceEntry = false;
        }
        else if (_afterPropertyName)
        {
            // Add space between colon and scalar value in block mapping context
            WriteRaw((byte)' ');
            _afterPropertyName = false;
        }
    }

    private void WriteScalarPostlude()
    {
        SetNotFirstInContainer();
        if (!_inFlowContext)
        {
            _needsNewLine = true;
        }
    }

    private void WriteScalarValue(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            WriteRaw("''"u8);
            return;
        }
        
        // Determine if quoting is needed
        bool needsQuoting = NeedsQuoting(value);
        
        if (needsQuoting)
        {
            WriteRaw((byte)'\'');
            Span<byte> charBuffer = stackalloc byte[4];
            foreach (char c in value)
            {
                if (c == '\'')
                {
                    WriteRaw("''"u8); // Escape single quote
                }
                else
                {
                    var chars = new ReadOnlySpan<char>(in c);
                    int byteCount = Encoding.UTF8.GetBytes(chars, charBuffer);
                    WriteRaw(charBuffer[..byteCount]);
                }
            }
            WriteRaw((byte)'\'');
        }
        else
        {
            WriteRaw(Encoding.UTF8.GetBytes(value.ToArray()));
        }
    }

    private static bool NeedsQuoting(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty) return true;
        
        char first = value[0];
        
        // Check for indicators that require quoting
        if (first is '-' or '?' or ':' or ',' or '[' or ']' or '{' or '}' 
            or '#' or '&' or '*' or '!' or '|' or '>' or '\'' or '"' 
            or '%' or '@' or '`')
        {
            return true;
        }
        
        // Check for special values
        if (value.SequenceEqual("null") || value.SequenceEqual("true") || 
            value.SequenceEqual("false") || value.SequenceEqual("~"))
        {
            return true;
        }
        
        // Check for problematic characters within the string
        foreach (char c in value)
        {
            if (c is ':' or '#' or '\n' or '\r')
            {
                return true;
            }
        }
        
        return false;
    }

    private void WriteIndent(int depth)
    {
        int spaces = depth * _options.IndentSize;
        Span<byte> indent = stackalloc byte[spaces];
        indent.Fill((byte)' ');
        WriteRaw(indent);
    }

    private void WriteNewLine()
    {
        WriteRaw((byte)'\n');
    }

    private void WriteRaw(byte b)
    {
        var span = _output.GetSpan(1);
        span[0] = b;
        _output.Advance(1);
        _bytesWritten++;
    }

    private void WriteRaw(ReadOnlySpan<byte> bytes)
    {
        var span = _output.GetSpan(bytes.Length);
        bytes.CopyTo(span);
        _output.Advance(bytes.Length);
        _bytesWritten += bytes.Length;
    }

    private bool IsFirstInContainer()
    {
        return _containerStack.Count > 0 && _containerStack.Peek().IsFirst;
    }

    private void SetNotFirstInContainer()
    {
        if (_containerStack.Count > 0)
        {
            var container = _containerStack.Pop();
            _containerStack.Push(container with { IsFirst = false });
        }
    }
}
