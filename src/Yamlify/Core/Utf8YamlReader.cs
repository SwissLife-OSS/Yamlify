using Yamlify.Exceptions;

namespace Yamlify.Core;

/// <summary>
/// A high-performance, low-allocation, forward-only reader for UTF-8 encoded YAML text.
/// </summary>
/// <remarks>
/// <para>
/// This reader is a ref struct and operates directly on a <see cref="ReadOnlySpan{T}"/> of bytes.
/// </para>
/// <para>
/// The reader implements the YAML 1.2 specification for parsing YAML streams.
/// </para>
/// </remarks>
public ref partial struct Utf8YamlReader
{
    private ReadOnlySpan<byte> _buffer;
    private readonly bool _isFinalBlock;
    private readonly YamlReaderOptions _options;
    
    private int _consumed;
    private int _line;
    private int _lineStart;
    private int _currentDepth;
    private long _totalConsumed;
    
    private YamlTokenType _tokenType;
    private ScalarStyle _scalarStyle;
    private CollectionStyle _collectionStyle;
    
    // Block scalar indentation tracking (for stripping leading spaces)
    private int _blockScalarIndent;
    
    // Current token value storage
    private ReadOnlySpan<byte> _valueSpan;
    
    // Token start position tracking
    private Mark _tokenStart;
    
    // Parser state
    private ParserState _state;
    private bool _hasValueSequence;
    private bool _expectDocumentStart; // True after directives, requires explicit ---
    private bool _awaitingFirstFlowEntry; // True after [ or {, before first entry
    private bool _needsFlowComma; // True after parsing flow entry, requires comma before next entry
    private bool _hasYamlDirective; // True if we've seen %YAML in this document
    private bool _hasDocumentContent; // True if we've emitted root content for current document
    private bool _expectingMappingValue; // True after parsing a mapping key, expecting value
    private bool _hadValueOnLine; // True if we parsed a complete key:value on the current line
    private int _lastValueLine; // Line number where _hadValueOnLine was set
    private bool _crossedLineBreakInFlow; // True when SkipFlowWhitespaceAndComments crossed a line break
    private bool _onDocumentStartLine; // True when content follows --- on the same line
    
    // Collection type tracking (true = mapping, false = sequence)
    private long _collectionStack; // Bit flags for up to 64 nested levels
    
    // Flow collection tracking (true = flow, false = block) at each depth
    private long _flowStack; // Bit flags for up to 64 nested levels
    
    // Expecting key tracking (true = expecting key, false = expecting value) at each depth
    private long _expectingKeyStack; // Bit flags for up to 64 nested levels
    
    // Indentation level tracking for each depth (up to 16 levels)
    // Stores the column at which each block collection was started
    private long _indentLevels0; // Stores 4 indent levels (16 bits each) for depths 0-3
    private long _indentLevels1; // Stores 4 indent levels (16 bits each) for depths 4-7
    private long _indentLevels2; // Stores 4 indent levels (16 bits each) for depths 8-11
    private long _indentLevels3; // Stores 4 indent levels (16 bits each) for depths 12-15
    
    // Column of the last anchor/tag token (1-based). Used to determine if anchor/tag
    // was indented relative to parent, which is required for indentless sequences.
    private int _lastAnchorOrTagColumn;
    
    // Token buffer for lookahead (Option C architecture)
    private TokenBuffer _tokenBuffer;
    
    // Simple key tracking stack for implicit key handling
    private SimpleKeyStack _simpleKeyStack;
    
    // Flow level counter (0 = block context, >0 = flow context depth)
    private int _flowLevel;
    
    // Storage for custom tag handles declared via TAG directives.
    // Must be cleared when starting a new document.
    private TagHandleStorage _tagHandles;
    
    /// <summary>
    /// Gets the type of the last processed token.
    /// </summary>
    public readonly YamlTokenType TokenType => _tokenType;

    /// <summary>
    /// Gets the current depth of nesting (sequences and mappings).
    /// </summary>
    public readonly int CurrentDepth => _currentDepth;

    /// <summary>
    /// Gets the total number of bytes consumed so far.
    /// </summary>
    public readonly long BytesConsumed => _totalConsumed + _consumed;

    /// <summary>
    /// Gets the current position within the input.
    /// </summary>
    public readonly Mark Position => new(_totalConsumed + _consumed, _line, _consumed - _lineStart + 1);

    /// <summary>
    /// Gets the position where the current token started.
    /// </summary>
    public readonly Mark TokenStart => _tokenStart;

    /// <summary>
    /// Gets a value indicating whether the current token has a value.
    /// </summary>
    public readonly bool HasValueSequence => _hasValueSequence;

    /// <summary>
    /// Gets the raw UTF-8 bytes of the current token's value.
    /// </summary>
    public readonly ReadOnlySpan<byte> ValueSpan => _valueSpan;

    /// <summary>
    /// Gets the scalar style of the current scalar token.
    /// </summary>
    public readonly ScalarStyle ScalarStyle => _scalarStyle;

    /// <summary>
    /// Gets the collection style of the current collection token.
    /// </summary>
    public readonly CollectionStyle CollectionStyle => _collectionStyle;

    /// <summary>
    /// Gets a value indicating whether this is the final block of input.
    /// </summary>
    public readonly bool IsFinalBlock => _isFinalBlock;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8YamlReader"/> struct.
    /// </summary>
    /// <param name="yamlData">The UTF-8 encoded YAML data to read.</param>
    /// <param name="options">Options for reading.</param>
    public Utf8YamlReader(ReadOnlySpan<byte> yamlData, YamlReaderOptions options = default)
        : this(yamlData, isFinalBlock: true, new YamlReaderState(options))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8YamlReader"/> struct for streaming scenarios.
    /// </summary>
    /// <param name="yamlData">The UTF-8 encoded YAML data to read.</param>
    /// <param name="isFinalBlock">Whether this is the final block of data.</param>
    /// <param name="state">The reader state from a previous read operation.</param>
    public Utf8YamlReader(ReadOnlySpan<byte> yamlData, bool isFinalBlock, YamlReaderState state)
    {
        _buffer = yamlData;
        _isFinalBlock = isFinalBlock;
        _options = state.Options.MaxDepth == 0 ? YamlReaderOptions.Default : state.Options;
        
        _consumed = 0;
        _line = state.Line == 0 ? 1 : state.Line;
        _lineStart = 0;
        _currentDepth = state.CurrentDepth;
        _totalConsumed = state.BytesConsumed;
        
        _tokenType = YamlTokenType.None;
        _scalarStyle = ScalarStyle.Any;
        _collectionStyle = CollectionStyle.Any;
        
        _valueSpan = default;
        
        _state = state.InStreamContext ? ParserState.InStream : ParserState.Initial;
        _hasValueSequence = false;
        
        // Initialize token buffer infrastructure
        _tokenBuffer = default;
        _simpleKeyStack = default;
        _flowLevel = 0;
    }

    /// <summary>
    /// Gets the current state for resumption in streaming scenarios.
    /// </summary>
    public readonly YamlReaderState CurrentState
    {
        get
        {
            return new YamlReaderState(_options)
            {
                CurrentDepth = _currentDepth,
                BytesConsumed = _totalConsumed + _consumed,
                Line = _line,
                Column = _consumed - _lineStart + 1,
                InStreamContext = _state != ParserState.Initial,
                InDocumentContext = _state == ParserState.InDocument || _state == ParserState.InBlockContent
            };
        }
    }

    /// <summary>
    /// Reads the next token from the input.
    /// </summary>
    /// <returns><c>true</c> if a token was read; <c>false</c> if the end of input was reached.</returns>
    /// <exception cref="YamlException">Thrown when the input contains invalid YAML.</exception>
    public bool Read()
    {
        // Check if we have buffered tokens to consume first
        if (!_tokenBuffer.IsEmpty)
        {
            return ConsumeBufferedToken();
        }
        
        // Reset value state
        _valueSpan = default;
        _hasValueSequence = false;
        _scalarStyle = ScalarStyle.Any;
        _collectionStyle = CollectionStyle.Any;

        // Skip whitespace and comments (unless comment handling is enabled)
        // In flow context, ParseFlowContent handles whitespace skipping so we can
        // track line breaks for multi-line implicit key detection
        if (_state != ParserState.InFlowContent)
        {
            SkipWhitespaceAndComments();
        }
        
        // Record the start position of this token
        _tokenStart = Position;

        if (_consumed >= _buffer.Length)
        {
            return HandleEndOfInput();
        }

        return _state switch
        {
            ParserState.Initial => ParseStreamStart(),
            ParserState.InStream => ParseDocument(),
            ParserState.InDocument => ParseDocumentContent(),
            ParserState.InBlockContent => ParseBlockContent(),
            ParserState.InFlowContent => ParseFlowContent(),
            ParserState.Finished => false,
            _ => throw new YamlException($"Invalid parser state: {_state}", Position)
        };
    }

    /// <summary>
    /// Gets the current scalar value as a string.
    /// </summary>
    /// <returns>The scalar value, or null if the current token is not a scalar.</returns>
    public readonly string? GetString()
    {
        if (_tokenType != YamlTokenType.Scalar && 
            _tokenType != YamlTokenType.Alias &&
            _tokenType != YamlTokenType.Anchor &&
            _tokenType != YamlTokenType.Tag)
        {
            return null;
        }

        if (_valueSpan.IsEmpty)
        {
            return string.Empty;
        }

        // For block scalars, we need to strip the content indentation
        if (_scalarStyle is ScalarStyle.Literal or ScalarStyle.Folded && _blockScalarIndent > 0)
        {
            return ProcessBlockScalarContent(_valueSpan, _blockScalarIndent, _scalarStyle);
        }

        // For double-quoted strings, decode escape sequences
        if (_scalarStyle == ScalarStyle.DoubleQuoted)
        {
            return DecodeDoubleQuotedString(_valueSpan);
        }

        return System.Text.Encoding.UTF8.GetString(_valueSpan);
    }

    /// <summary>
    /// Decodes escape sequences in a double-quoted YAML string.
    /// </summary>
    private static string DecodeDoubleQuotedString(ReadOnlySpan<byte> content)
    {
        // Fast path: no escapes
        if (content.IndexOf((byte)'\\') < 0)
        {
            return System.Text.Encoding.UTF8.GetString(content);
        }

        var result = new System.Text.StringBuilder(content.Length);
        int pos = 0;

        while (pos < content.Length)
        {
            byte current = content[pos];

            if (current == (byte)'\\' && pos + 1 < content.Length)
            {
                byte escaped = content[pos + 1];
                pos += 2; // Skip the backslash and escape char

                char decoded = escaped switch
                {
                    (byte)'0' => '\0',    // Null
                    (byte)'a' => '\a',    // Bell
                    (byte)'b' => '\b',    // Backspace
                    (byte)'t' => '\t',    // Tab
                    (byte)'n' => '\n',    // Line feed
                    (byte)'v' => '\v',    // Vertical tab
                    (byte)'f' => '\f',    // Form feed
                    (byte)'r' => '\r',    // Carriage return
                    (byte)'e' => '\x1B',  // Escape
                    (byte)' ' => ' ',     // Space
                    (byte)'"' => '"',     // Double quote
                    (byte)'/' => '/',     // Slash
                    (byte)'\\' => '\\',   // Backslash
                    (byte)'N' => '\x85',  // Next line (NEL)
                    (byte)'_' => '\xA0',  // Non-breaking space
                    (byte)'L' => '\u2028', // Line separator
                    (byte)'P' => '\u2029', // Paragraph separator
                    (byte)'\n' => '\0',   // Escaped newline (line folding) - skip
                    (byte)'x' when pos + 2 <= content.Length => DecodeHexEscape(content, ref pos, 2),
                    (byte)'u' when pos + 4 <= content.Length => DecodeHexEscape(content, ref pos, 4),
                    (byte)'U' when pos + 8 <= content.Length => DecodeHexEscape(content, ref pos, 8),
                    _ => (char)escaped // Fallback: just use the escaped char
                };

                if (decoded != '\0' || escaped == (byte)'0')
                {
                    result.Append(decoded);
                }
            }
            else if (current == (byte)'\n' || current == (byte)'\r')
            {
                // Normalize line breaks in multiline double-quoted strings
                // Skip CR if followed by LF
                if (current == (byte)'\r' && pos + 1 < content.Length && content[pos + 1] == (byte)'\n')
                {
                    pos++;
                }
                result.Append(' '); // Single newlines become spaces in double-quoted strings
                pos++;
            }
            else
            {
                result.Append((char)current);
                pos++;
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Decodes a hex escape sequence (\xNN, \uNNNN, or \UNNNNNNNN).
    /// </summary>
    private static char DecodeHexEscape(ReadOnlySpan<byte> content, ref int pos, int digits)
    {
        if (pos + digits > content.Length)
        {
            return '\0';
        }

        int value = 0;
        for (int i = 0; i < digits; i++)
        {
            byte b = content[pos + i];
            int digit = b switch
            {
                >= (byte)'0' and <= (byte)'9' => b - '0',
                >= (byte)'a' and <= (byte)'f' => b - 'a' + 10,
                >= (byte)'A' and <= (byte)'F' => b - 'A' + 10,
                _ => -1
            };
            if (digit < 0)
            {
                return '\0';
            }

            value = (value << 4) | digit;
        }

        pos += digits;
        return (char)value;
    }

    /// <summary>
    /// Processes block scalar content by stripping indentation and applying folding rules.
    /// </summary>
    private static string ProcessBlockScalarContent(ReadOnlySpan<byte> content, int indent, ScalarStyle style)
    {
        var result = new System.Text.StringBuilder();
        int pos = 0;
        bool firstLine = true;
        bool previousWasEmpty = false;
        
        while (pos < content.Length)
        {
            // Skip the content indentation (strip leading spaces up to indent level)
            int spacesSkipped = 0;
            while (pos < content.Length && content[pos] == (byte)' ' && spacesSkipped < indent)
            {
                pos++;
                spacesSkipped++;
            }
            
            // Find end of line
            int lineStart = pos;
            while (pos < content.Length && content[pos] != (byte)'\n' && content[pos] != (byte)'\r')
            {
                pos++;
            }
            
            int lineEnd = pos;
            bool isEmptyLine = lineStart == lineEnd;
            
            // Handle line content
            if (style == ScalarStyle.Folded)
            {
                // Folded style: single newlines become spaces, empty lines become newlines
                if (isEmptyLine)
                {
                    if (!firstLine)
                    {
                        result.Append('\n');
                    }
                    previousWasEmpty = true;
                }
                else
                {
                    if (!firstLine && !previousWasEmpty)
                    {
                        result.Append(' ');
                    }
                    result.Append(System.Text.Encoding.UTF8.GetString(content.Slice(lineStart, lineEnd - lineStart)));
                    previousWasEmpty = false;
                }
            }
            else
            {
                // Literal style: preserve newlines as-is
                if (!firstLine)
                {
                    result.Append('\n');
                }
                result.Append(System.Text.Encoding.UTF8.GetString(content.Slice(lineStart, lineEnd - lineStart)));
            }
            
            firstLine = false;
            
            // Skip line break
            if (pos < content.Length && content[pos] == (byte)'\r')
            {
                pos++;
            }
            if (pos < content.Length && content[pos] == (byte)'\n')
            {
                pos++;
            }
        }
        
        // Apply chomping: default is Clip (single trailing newline)
        // For now, just add a trailing newline for non-empty content
        if (result.Length > 0 && !result.ToString().EndsWith('\n'))
        {
            result.Append('\n');
        }
        
        return result.ToString();
    }

    /// <summary>
    /// Attempts to parse the current scalar as an Int32.
    /// </summary>
    /// <param name="value">The parsed value.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public readonly bool TryGetInt32(out int value)
    {
        value = 0;
        if (_tokenType != YamlTokenType.Scalar || _valueSpan.IsEmpty)
        {
            return false;
        }

        return System.Buffers.Text.Utf8Parser.TryParse(_valueSpan, out value, out _);
    }

    /// <summary>
    /// Attempts to parse the current scalar as an Int64.
    /// </summary>
    /// <param name="value">The parsed value.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public readonly bool TryGetInt64(out long value)
    {
        value = 0;
        if (_tokenType != YamlTokenType.Scalar || _valueSpan.IsEmpty)
        {
            return false;
        }

        return System.Buffers.Text.Utf8Parser.TryParse(_valueSpan, out value, out _);
    }

    /// <summary>
    /// Attempts to parse the current scalar as a Double.
    /// </summary>
    /// <param name="value">The parsed value.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public readonly bool TryGetDouble(out double value)
    {
        value = 0;
        if (_tokenType != YamlTokenType.Scalar || _valueSpan.IsEmpty)
        {
            return false;
        }

        // Handle special YAML float values
        if (TryMatchSpecialFloat(out value))
        {
            return true;
        }

        return System.Buffers.Text.Utf8Parser.TryParse(_valueSpan, out value, out _);
    }

    /// <summary>
    /// Attempts to parse the current scalar as a Boolean.
    /// </summary>
    /// <param name="value">The parsed value.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public readonly bool TryGetBoolean(out bool value)
    {
        value = false;
        if (_tokenType != YamlTokenType.Scalar || _valueSpan.IsEmpty)
        {
            return false;
        }

        // YAML Core Schema boolean patterns
        return TryMatchBoolean(out value);
    }

    /// <summary>
    /// Gets a value indicating whether the current scalar represents null.
    /// </summary>
    /// <remarks>
    /// This returns true for:
    /// - YAML null values: null, Null, NULL, ~
    /// - Empty unquoted scalars
    /// 
    /// This returns false for:
    /// - Quoted strings like "" or '' (empty quoted strings are intentional empty strings)
    /// - Quoted strings like "null" (the literal string "null", not a null value)
    /// </remarks>
    /// <returns><c>true</c> if the value is null; otherwise, <c>false</c>.</returns>
    public readonly bool IsNull()
    {
        if (_tokenType != YamlTokenType.Scalar)
        {
            return false;
        }

        // Quoted strings are never null - even empty quoted strings are intentional empty strings
        if (_scalarStyle == ScalarStyle.SingleQuoted || _scalarStyle == ScalarStyle.DoubleQuoted)
        {
            return false;
        }

        if (_valueSpan.IsEmpty)
        {
            return true;
        }

        return IsNullValue(_valueSpan);
    }

    /// <summary>
    /// Skips the current token and all its children (for collections).
    /// </summary>
    public void Skip()
    {
        if (_tokenType is YamlTokenType.MappingStart or YamlTokenType.SequenceStart)
        {
            int depth = _currentDepth;
            while (Read() && _currentDepth >= depth)
            {
                // Keep reading until we exit the current depth
            }
        }
    }
}

/// <summary>
/// Internal parser state.
/// </summary>
internal enum ParserState : byte
{
    Initial,
    InStream,
    InDocument,
    InBlockContent,
    InFlowContent,
    Finished
}
