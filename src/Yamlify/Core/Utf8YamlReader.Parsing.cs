using Yamlify.Exceptions;

namespace Yamlify.Core;

/// <summary>
/// Parsing methods for the Utf8YamlReader.
/// </summary>
public ref partial struct Utf8YamlReader
{
    // YAML Character Constants (from spec chapter 5)
    private const byte Tab = 0x09;           // \t
    private const byte LineFeed = 0x0A;      // \n
    private const byte CarriageReturn = 0x0D; // \r
    private const byte Space = 0x20;         // ' '
    
    // Indicators (from spec chapter 5.3)
    private const byte SequenceEntry = 0x2D;  // -
    private const byte MappingKey = 0x3F;     // ?
    private const byte MappingValue = 0x3A;   // :
    private const byte CollectEntry = 0x2C;   // ,
    private const byte SequenceStart = 0x5B;  // [
    private const byte SequenceEnd = 0x5D;    // ]
    private const byte MappingStartChar = 0x7B; // {
    private const byte MappingEndChar = 0x7D;   // }
    private const byte Comment = 0x23;        // #
    private const byte AnchorChar = 0x26;     // &
    private const byte AliasChar = 0x2A;      // *
    private const byte TagChar = 0x21;        // !
    private const byte Literal = 0x7C;        // |
    private const byte Folded = 0x3E;         // >
    private const byte SingleQuote = 0x27;    // '
    private const byte DoubleQuote = 0x22;    // "
    private const byte Directive = 0x25;      // %
    private const byte Percent = 0x25;        // %

    // Document markers
    private static ReadOnlySpan<byte> DocumentStartMarker => "---"u8;
    private static ReadOnlySpan<byte> DocumentEndMarker => "..."u8;
    
    // Directive names
    private static ReadOnlySpan<byte> YamlDirective => "YAML"u8;
    private static ReadOnlySpan<byte> TagDirective => "TAG"u8;

    // Null patterns (Core Schema)
    private static ReadOnlySpan<byte> NullLower => "null"u8;
    private static ReadOnlySpan<byte> NullTitle => "Null"u8;
    private static ReadOnlySpan<byte> NullUpper => "NULL"u8;
    private static ReadOnlySpan<byte> NullTilde => "~"u8;

    // Boolean patterns (Core Schema)
    private static ReadOnlySpan<byte> TrueLower => "true"u8;
    private static ReadOnlySpan<byte> TrueTitle => "True"u8;
    private static ReadOnlySpan<byte> TrueUpper => "TRUE"u8;
    private static ReadOnlySpan<byte> FalseLower => "false"u8;
    private static ReadOnlySpan<byte> FalseTitle => "False"u8;
    private static ReadOnlySpan<byte> FalseUpper => "FALSE"u8;

    // Special float patterns (Core Schema)
    private static ReadOnlySpan<byte> InfLower => ".inf"u8;
    private static ReadOnlySpan<byte> InfTitle => ".Inf"u8;
    private static ReadOnlySpan<byte> InfUpper => ".INF"u8;
    private static ReadOnlySpan<byte> NegInfLower => "-.inf"u8;
    private static ReadOnlySpan<byte> NegInfTitle => "-.Inf"u8;
    private static ReadOnlySpan<byte> NegInfUpper => "-.INF"u8;
    private static ReadOnlySpan<byte> PosInfLower => "+.inf"u8;
    private static ReadOnlySpan<byte> PosInfTitle => "+.Inf"u8;
    private static ReadOnlySpan<byte> PosInfUpper => "+.INF"u8;
    private static ReadOnlySpan<byte> NanLower => ".nan"u8;
    private static ReadOnlySpan<byte> NanTitle => ".NaN"u8;
    private static ReadOnlySpan<byte> NanUpper => ".NAN"u8;

    private bool ParseStreamStart()
    {
        // Handle BOM if present
        SkipByteOrderMark();
        
        _tokenType = YamlTokenType.StreamStart;
        _state = ParserState.InStream;
        return true;
    }

    private bool ParseDocument()
    {
        SkipWhitespaceAndComments();

        if (_consumed >= _buffer.Length)
        {
            // If we were expecting a document start (after directives), that's an error
            if (_expectDocumentStart)
            {
                throw new YamlException("Directive without document - expected '---' after directive", Position);
            }
            return HandleEndOfInput();
        }

        // Check for directives
        if (CurrentByte() == Directive)
        {
            return ParseDirective();
        }

        // Check for explicit document start
        if (IsAtDocumentMarker(DocumentStartMarker))
        {
            _consumed += 3;
            
            // Clear tag handles from previous document ONLY if this --- is not following directives.
            // When _expectDocumentStart is true, it means we just parsed directives that apply to this document.
            // TAG directives are document-scoped: they apply from their declaration until document end.
            if (!_expectDocumentStart)
            {
                _tagHandles.Clear();
            }
            
            // Skip whitespace after --- (content may follow on the same line, which is valid)
            // Content after --- is treated as being at column 0 for indentation purposes
            while (_consumed < _buffer.Length && (CurrentByte() == Space || CurrentByte() == Tab))
            {
                _consumed++;
            }
            
            // If there's content on the same line as ---, set lineStart to current position
            // This makes the content appear at indent 0 for the purposes of block indentation
            _onDocumentStartLine = false;
            if (_consumed < _buffer.Length && !IsLineBreak(CurrentByte()) && CurrentByte() != Comment)
            {
                _lineStart = _consumed;
                _onDocumentStartLine = true; // Content follows --- on the same line
            }
            
            _tokenType = YamlTokenType.DocumentStart;
            _state = ParserState.InDocument;
            _expectDocumentStart = false;
            _hasDocumentContent = false; // Reset for new document
            return true;
        }

        // Check for document end marker (signals empty document or between-document state)
        if (IsAtDocumentMarker(DocumentEndMarker))
        {
            // If we were expecting a document start (after directives), that's an error
            if (_expectDocumentStart)
            {
                throw new YamlException("Directive without document - expected '---' after directive, found '...'", Position);
            }
            _consumed += 3;
            
            // Validate nothing follows except whitespace/comment
            ValidateAfterDocumentMarker();
            
            _tokenType = YamlTokenType.DocumentEnd;
            _hasYamlDirective = false; // Reset for next document
            _hasDocumentContent = false; // Reset for next document (allows bare document after ...)
            _tagHandles.Clear(); // Clear tag handles - TAG directives are document-scoped
            // Stay in stream state to look for more documents
            return true;
        }

        // Implicit document start (content without --- marker)
        // Not allowed if we had directives
        if (_expectDocumentStart)
        {
            throw new YamlException("Directive without document - expected '---' after directive", Position);
        }
        
        // Not allowed if the previous document had content (ambiguous with multiline scalars)
        // This catches cases like:
        //   foo:
        //     bar
        //   invalid    <- should fail, needs explicit ---
        if (_hasDocumentContent)
        {
            throw new YamlException("Content after document end requires explicit document start marker '---'", Position);
        }
        
        if (!IsWhitespaceOrLineBreak(CurrentByte()))
        {
            _tokenType = YamlTokenType.DocumentStart;
            _state = ParserState.InDocument;
            _hasDocumentContent = false; // Reset for new document
            return true;
        }

        return false;
    }

    private bool ParseDocumentContent()
    {
        SkipWhitespaceAndComments();

        if (_consumed >= _buffer.Length)
        {
            // Empty document - emit an empty scalar (null), then document end on next read
            if (_tokenType == YamlTokenType.DocumentStart)
            {
                // First read after document start - emit empty scalar
                _valueSpan = default;
                _tokenType = YamlTokenType.Scalar;
                _scalarStyle = ScalarStyle.Plain;
                return true;
            }
            
            // After the empty scalar, emit document end
            _tokenType = YamlTokenType.DocumentEnd;
            _state = ParserState.InStream;
            return true;
        }

        // Check for document end marker
        if (IsAtStartOfLine() && IsAtDocumentMarker(DocumentEndMarker))
        {
            _consumed += 3;
            
            // Validate nothing follows except whitespace/comment
            ValidateAfterDocumentMarker();
            
            _tokenType = YamlTokenType.DocumentEnd;
            _state = ParserState.InStream;
            return true;
        }

        // Check for new document start
        if (IsAtStartOfLine() && IsAtDocumentMarker(DocumentStartMarker))
        {
            // Implicit document end, then we'll read the new document start next
            _tokenType = YamlTokenType.DocumentEnd;
            _state = ParserState.InStream;
            return true;
        }

        _state = ParserState.InBlockContent;
        return ParseBlockContent();
    }

    private bool ParseBlockContent()
    {
        YamlTokenType previousTokenType = _tokenType;
        SkipWhitespaceAndComments();

        if (_consumed >= _buffer.Length)
        {
            return HandleEndOfBlockContent();
        }

        byte current = CurrentByte();
        
        // Calculate current indentation: how far are we from the line start?
        int currentIndent = _consumed - _lineStart;
        
        // Check if we're at the start of a new line (i.e., first non-whitespace on this line)
        bool isFirstContentOnLine = IsFirstNonWhitespaceOnLine();
        
        // Reset _hadValueOnLine when we move to a new line
        if (isFirstContentOnLine && _hadValueOnLine && _line != _lastValueLine)
        {
            _hadValueOnLine = false;
        }
        
        // Check for invalid comment (# must be preceded by whitespace or start of line)
        // But we DON'T check this here - comments are handled elsewhere
        // The # character as first content on line is valid (comment at start of line)
        // The # character after whitespace is valid (trailing comment)
        // We should only reject # if it's in the middle of content without whitespace
        
        // Check for tabs used as indentation - YAML spec forbids this
        if (_currentDepth > 0 && HasTabsInIndentation())
        {
            throw new YamlException("Tabs are not allowed for indentation in YAML", Position);
        }
        
        // Validate indentation for block collections
        if (_currentDepth > 0 && !IsInsideFlowContext() && isFirstContentOnLine)
        {
            int expectedIndent = GetIndentLevel(_currentDepth - 1);
            
            if (expectedIndent >= 0)
            {
                // Content at less indentation means we're exiting the current collection
                if (currentIndent < expectedIndent)
                {
                    return HandleEndOfBlockContent();
                }
                
                // Special case: When inside a sequence nested in a mapping, and we see ':'
                // at the sequence's indentation level, the sequence is ending because
                // ':' is the explicit value indicator for the parent mapping.
                // This handles patterns like:
                //   ?
                //   - a
                //   - b
                //   :
                //   - c
                if (!IsCurrentCollectionMapping() && current == MappingValue && 
                    IsIndicatorFollowedByWhitespace(1) && currentIndent == expectedIndent)
                {
                    // Check if the parent is a mapping
                    if (_currentDepth >= 2)
                    {
                        // Look up the parent collection type
                        bool parentIsMapping = ((_collectionStack >> (_currentDepth - 2)) & 1) == 1;
                        if (parentIsMapping)
                        {
                            return HandleEndOfBlockContent();
                        }
                    }
                }
                
                // When inside a sequence, if we see content at the same indentation
                // that is NOT a sequence entry '-', the sequence is ending
                // This handles patterns like:
                //   - item1
                //   - item2
                //   invalid: x  <-- this should end the sequence
                if (!IsCurrentCollectionMapping() && currentIndent == expectedIndent &&
                    current != SequenceEntry)
                {
                    // Not a sequence entry at the same indentation - sequence is ending
                    return HandleEndOfBlockContent();
                }
                
                // After a nested collection ends, if we're in a mapping expecting a key,
                // content at greater indentation is invalid (there's nothing to continue)
                if (IsCurrentCollectionMapping() && IsExpectingKey() && currentIndent > expectedIndent &&
                    (previousTokenType == YamlTokenType.SequenceEnd || previousTokenType == YamlTokenType.MappingEnd))
                {
                    throw new YamlException($"Invalid content after nested collection: expected new key at column {expectedIndent + 1}", Position);
                }
                
                // If we just parsed a value (not expecting another value), then the next
                // content at greater indentation is invalid - it should be a sibling key
                // at the same indentation level
                // Exception: After an anchor or tag, we are still expecting the actual value
                if (!_expectingMappingValue && currentIndent > expectedIndent && 
                    _tokenType != YamlTokenType.Anchor && _tokenType != YamlTokenType.Tag)
                {
                    // Check if this looks like a mapping key at wrong indentation
                    if (IsCurrentCollectionMapping() && LooksLikeImplicitMappingKey())
                    {
                        throw new YamlException($"Wrong indentation: expected {expectedIndent} spaces but got {currentIndent}", Position);
                    }
                    // For sequences, check if we see a sequence entry at wrong indentation
                    if (!IsCurrentCollectionMapping() && current == SequenceEntry && IsIndicatorFollowedByWhitespace(1))
                    {
                        throw new YamlException($"Wrong indentation: expected {expectedIndent} spaces but got {currentIndent}", Position);
                    }
                    // In a mapping after a scalar value, any content at wrong indentation is invalid
                    // This catches cases like a comment interrupting a multiline plain scalar:
                    //   key: word1
                    //   #  comment
                    //     word2    <- wrong indentation, not a valid key or continuation
                    if (IsCurrentCollectionMapping() && _tokenType == YamlTokenType.Scalar)
                    {
                        throw new YamlException($"Invalid indentation: expected key at column {expectedIndent + 1} or continuation at greater indent, got column {currentIndent + 1}", Position);
                    }
                }
            }
        }

        // Check for document markers at start of line (column 0)
        if (currentIndent == 0 && isFirstContentOnLine)
        {
            if (IsAtDocumentMarker(DocumentStartMarker) || IsAtDocumentMarker(DocumentEndMarker))
            {
                return HandleEndOfBlockContent();
            }
            
            // NOTE: We intentionally do NOT check for directives here.
            // % at column 0 in document content is valid as part of a multiline plain scalar.
            // The directive check should only happen before any document content.
        }
        
        // Handle zero-indented sequence after explicit mapping key (?)
        // This covers patterns like:
        //   ?
        //   - a
        //   - b
        //   :
        //   - c
        // where the key/value are sequences at the same indentation as ?/:
        if (IsCurrentCollectionMapping() && !IsExpectingKey() && isFirstContentOnLine && 
            _currentDepth > 0 && !IsInsideFlowContext() &&
            current == SequenceEntry && IsIndicatorFollowedByWhitespace(1) &&
            (_tokenType == YamlTokenType.Key || _tokenType == YamlTokenType.Value))
        {
            int parentIndent = GetIndentLevel(_currentDepth - 1);
            // Allow sequence at same or greater indentation after explicit key/value indicator
            if (currentIndent >= parentIndent)
            {
                SetIndentLevel(_currentDepth, currentIndent);
                _tokenType = YamlTokenType.SequenceStart;
                _collectionStyle = CollectionStyle.Block;
                PushCollectionType(false);
                PushFlowContext(false);
                _currentDepth++;
                SetExpectingKey(true); // After this sequence, expect a key
                return true;
            }
        }
        
        // Check if we're starting a nested block collection (mapping or sequence)
        // This handles cases where we have a key (and possibly anchor/tag) and the value
        // is a nested block on the next line(s).
        if (_expectingMappingValue && isFirstContentOnLine && _currentDepth > 0 && !IsInsideFlowContext())
        {
            int parentIndent = GetIndentLevel(_currentDepth - 1);
            if (currentIndent > parentIndent)
            {
                if (current == SequenceEntry && IsIndicatorFollowedByWhitespace(1))
                {
                    SetIndentLevel(_currentDepth, currentIndent);
                    if (IsCurrentCollectionMapping()) SetExpectingKey(true);
                    _tokenType = YamlTokenType.SequenceStart;
                    _collectionStyle = CollectionStyle.Block;
                    PushCollectionType(false);
                    PushFlowContext(false);
                    _currentDepth++;
                    _expectingMappingValue = false;
                    return true;
                }
                if (!IsFlowIndicator(current) && LooksLikeImplicitMappingKey())
                {
                    SetIndentLevel(_currentDepth, currentIndent);
                    if (IsCurrentCollectionMapping()) SetExpectingKey(true);
                    _tokenType = YamlTokenType.MappingStart;
                    _collectionStyle = CollectionStyle.Block;
                    PushCollectionType(true);
                    PushFlowContext(false);
                    _currentDepth++;
                    SetExpectingKey(true);
                    _expectingMappingValue = false;
                    return true;
                }
            }
            // Handle indentless sequence: sequence at same indentation as parent when expecting value
            // This happens in patterns like:
            //   key:
            //    &anchor
            //   - value
            // where the sequence entry is at the same indentation as the key
            // The anchor/tag must be indented from the parent (i.e., anchor column > parent column)
            else if (current == SequenceEntry && IsIndicatorFollowedByWhitespace(1) && currentIndent == parentIndent)
            {
                // This is valid when we just saw an anchor or tag on a prior line,
                // AND that anchor/tag was properly indented from the parent
                // parentIndent is 0-based, _lastAnchorOrTagColumn is 1-based
                if ((_tokenType == YamlTokenType.Anchor || _tokenType == YamlTokenType.Tag) &&
                    _lastAnchorOrTagColumn > parentIndent + 1)
                {
                    SetIndentLevel(_currentDepth, currentIndent);
                    if (IsCurrentCollectionMapping()) SetExpectingKey(true);
                    _tokenType = YamlTokenType.SequenceStart;
                    _collectionStyle = CollectionStyle.Block;
                    PushCollectionType(false);
                    PushFlowContext(false);
                    _currentDepth++;
                    _expectingMappingValue = false;
                    return true;
                }
            }
            // After anchor/tag on previous line, if content is at parent indentation or less,
            // and it's NOT an indentless sequence (handled above), it's an error.
            // This catches cases like:
            //   key: &anchor
            //   !!map  <- at column 0, same as parent - invalid
            //     a: b
            // Exception: If we're expecting a mapping value and the content looks like a new key,
            // this is the empty value case (handled later).
            else if (currentIndent <= parentIndent && 
                     (_tokenType == YamlTokenType.Anchor || _tokenType == YamlTokenType.Tag))
            {
                // Check if this is the "anchor with empty value followed by new key" pattern
                bool isNewMappingKey = _expectingMappingValue && 
                                       (LooksLikeImplicitMappingKey() || 
                                        (current == MappingKey && IsIndicatorFollowedByWhitespace(1)));
                if (!isNewMappingKey)
                {
                    throw new YamlException($"Content after anchor/tag must be indented: expected column > {parentIndent + 1}", Position);
                }
            }
        }
        
        // Handle nested collections after anchor/tag in a sequence entry
        // This covers patterns like:
        //   - !!seq
        //    - nested
        // where the anchor/tag applies to a nested sequence
        if (!IsCurrentCollectionMapping() && isFirstContentOnLine && _currentDepth > 0 && !IsInsideFlowContext() &&
            (_tokenType == YamlTokenType.Anchor || _tokenType == YamlTokenType.Tag))
        {
            int parentIndent = GetIndentLevel(_currentDepth - 1);
            // The nested sequence should be at greater indentation than the parent
            if (currentIndent > parentIndent && current == SequenceEntry && IsIndicatorFollowedByWhitespace(1))
            {
                SetIndentLevel(_currentDepth, currentIndent);
                _tokenType = YamlTokenType.SequenceStart;
                _collectionStyle = CollectionStyle.Block;
                PushCollectionType(false);
                PushFlowContext(false);
                _currentDepth++;
                return true;
            }
            // Also handle nested mapping
            if (currentIndent > parentIndent && LooksLikeImplicitMappingKey())
            {
                SetIndentLevel(_currentDepth, currentIndent);
                _tokenType = YamlTokenType.MappingStart;
                _collectionStyle = CollectionStyle.Block;
                PushCollectionType(true);
                PushFlowContext(false);
                _currentDepth++;
                SetExpectingKey(true);
                return true;
            }
        }
        
        // Handle anchor/tag followed by sibling content (empty value case)
        // This covers patterns like:
        //   a: &anchor
        //   b: *anchor
        // where the anchor attaches to an empty/null value
        if (_expectingMappingValue && isFirstContentOnLine && _currentDepth > 0 && !IsInsideFlowContext() &&
            (_tokenType == YamlTokenType.Anchor || _tokenType == YamlTokenType.Tag))
        {
            int parentIndent = GetIndentLevel(_currentDepth - 1);
            // If content is at same indentation as parent and looks like a new key,
            // the anchor's value is empty
            if (currentIndent <= parentIndent && LooksLikeImplicitMappingKey())
            {
                _valueSpan = default;
                _tokenType = YamlTokenType.Scalar;
                _scalarStyle = ScalarStyle.Plain;
                _expectingMappingValue = false;
                SetExpectingKey(true);
                return true;
            }
            // Also handle sequence entry at same indentation
            if (currentIndent <= parentIndent && current == SequenceEntry && IsIndicatorFollowedByWhitespace(1))
            {
                _valueSpan = default;
                _tokenType = YamlTokenType.Scalar;
                _scalarStyle = ScalarStyle.Plain;
                _expectingMappingValue = false;
                SetExpectingKey(true);
                return true;
            }
        }
        
        // Check if we're starting an implicit block sequence (- item pattern)
        // Only do this at depth 0 (not inside another collection)
        if (_currentDepth == 0 && current == SequenceEntry && IsIndicatorFollowedByWhitespace(1))
        {
            // If we've already emitted root content, we can't have another root node
            if (_hasDocumentContent)
            {
                throw new YamlException("Multiple root nodes not allowed in a single document", Position);
            }
            
            // Block sequence cannot follow anchor/tag on the same line at document level
            // e.g., "&anchor - sequence" is invalid
            if (!isFirstContentOnLine && (_tokenType == YamlTokenType.Anchor || _tokenType == YamlTokenType.Tag))
            {
                throw new YamlException("Block sequence indicator '-' cannot follow anchor or tag on the same line at document level", Position);
            }
            
            // Record the indentation level for this collection
            SetIndentLevel(_currentDepth, currentIndent);
            
            // Emit SequenceStart - position stays where it is to parse the entry next
            _tokenType = YamlTokenType.SequenceStart;
            _collectionStyle = CollectionStyle.Block;
            PushCollectionType(false); // sequence
            PushFlowContext(false); // block context
            _currentDepth++;
            _hasDocumentContent = true; // Mark that we have root content
            return true;
        }
        
        // Check if we're starting an explicit block mapping (? key pattern)
        // Only do this at depth 0 (not inside another collection)
        if (_currentDepth == 0 && current == MappingKey && IsIndicatorFollowedByWhitespace(1))
        {
            // If we've already emitted root content, we can't have another root node
            if (_hasDocumentContent)
            {
                throw new YamlException("Multiple root nodes not allowed in a single document", Position);
            }
            
            // Record the indentation level for this collection
            SetIndentLevel(_currentDepth, currentIndent);
            
            // Emit MappingStart - position stays where it is to parse the explicit key next
            _tokenType = YamlTokenType.MappingStart;
            _collectionStyle = CollectionStyle.Block;
            PushCollectionType(true); // mapping
            PushFlowContext(false); // block context
            _currentDepth++;
            SetExpectingKey(true);
            _hasDocumentContent = true; // Mark that we have root content
            return true;
        }
        
        // Check if we're starting an implicit block mapping (key: value pattern)
        // Only do this at depth 0 (not inside another collection)
        // Flow indicators [ and { can be mapping keys if followed by ]: or }:
        if (_currentDepth == 0 && current != SequenceEntry && 
            (current == SequenceStart || current == MappingStartChar || !IsFlowIndicator(current)))
        {
            if (LooksLikeImplicitMappingKey())
            {
                // If we've already emitted root content, we can't have another root node
                if (_hasDocumentContent)
                {
                    throw new YamlException("Multiple root nodes not allowed in a single document", Position);
                }
                
                // Check for invalid anchor/tag before implicit mapping on document start line
                // e.g., "--- &anchor a: b" is invalid because the anchor placement is ambiguous
                if (_onDocumentStartLine && (current == AnchorChar || current == TagChar))
                {
                    throw new YamlException("Anchor or tag before implicit mapping on same line as document start is ambiguous", Position);
                }
                
                // Record the indentation level for this collection
                SetIndentLevel(_currentDepth, currentIndent);
                
                // Emit MappingStart - position stays where it is to parse the key next
                _tokenType = YamlTokenType.MappingStart;
                _collectionStyle = CollectionStyle.Block;
                PushCollectionType(true); // mapping
                PushFlowContext(false); // block context
                _currentDepth++;
                SetExpectingKey(true);
                _hasDocumentContent = true; // Mark that we have root content
                return true;
            }
        }

        // Parse based on current character
        // But first, if we're at depth 0 and already have document content,
        // reject any new root content that's not a continuation of existing structures
        if (_currentDepth == 0 && _hasDocumentContent)
        {
            throw new YamlException("Multiple root nodes not allowed in a single document", Position);
        }
        
        // Handle null key case: if we are in a mapping and expecting a key, but see ':',
        // it means we have a null key. This applies when:
        // 1. ':' is at the start of a line (first content on line), OR
        // 2. The previous token was MappingStart (we just entered a new mapping)
        // If there was content before ':' on this line that wasn't a structure start,
        // then that content is the key and ':' is the value indicator.
        if (current == MappingValue && IsCurrentCollectionMapping() && IsExpectingKey() && 
            IsIndicatorFollowedByWhitespace(1))
        {
            // Null key: ':' at the very start of the line OR right after MappingStart
            // Value indicator: ':' after some content on the same line
            if (isFirstContentOnLine || previousTokenType == YamlTokenType.MappingStart)
            {
                // Implicit null key - ':' at start of line with no key
                _valueSpan = default;
                _tokenType = YamlTokenType.Scalar;
                _scalarStyle = ScalarStyle.Plain;
                SetExpectingKey(false); // Key is null, now expect value
                return true;
            }
            
            // Mid-line ':' when expecting key - need to validate
            // Invalid: a: b: c: d (we already completed a key:value on this line)
            // Valid: &anchor key : value (no completed key:value yet, anchor+key is the key)
            if (_hadValueOnLine && !IsInsideFlowContext())
            {
                throw new YamlException("Invalid ':' mid-line - cannot start a new mapping key after a value on the same line", Position);
            }
            
            // Otherwise, fall through to parse ':' as value indicator
        }

        bool result = current switch
        {
            SequenceEntry when IsIndicatorFollowedByWhitespace(1) => ParseBlockSequenceEntry(),
            MappingKey when IsIndicatorFollowedByWhitespace(1) => ParseExplicitMappingKey(),
            MappingValue when IsIndicatorFollowedByWhitespace(1) => ParseMappingValue(isFirstContentOnLine),
            SequenceStart => ParseFlowSequence(),
            MappingStartChar => ParseFlowMapping(),
            SingleQuote => ParseSingleQuotedScalar(),
            DoubleQuote => ParseDoubleQuotedScalar(),
            Literal => ParseLiteralBlockScalar(),
            Folded => ParseFoldedBlockScalar(),
            AnchorChar => ParseAnchor(),
            AliasChar => ParseAlias(),
            TagChar => ParseTag(),
            _ => ParsePlainScalar()
        };

        // Validate that implicit keys in block mappings are followed by ':'
        if (result && _tokenType == YamlTokenType.Scalar && 
            IsCurrentCollectionMapping() && IsExpectingKey() && 
            previousTokenType != YamlTokenType.Key)
        {
            // Only validate if it's at the collection indentation level
            // If it's more indented, it's a continuation of the previous value
            int expectedIndent = GetIndentLevel(_currentDepth - 1);
            if (currentIndent <= expectedIndent)
            {
                ValidateImplicitKey();
            }
        }

        // Update state
        if (result && IsCurrentCollectionMapping())
        {
            if (_tokenType == YamlTokenType.MappingStart)
            {
                // New mapping started, expect key
                SetExpectingKey(true);
            }
            else if (_tokenType == YamlTokenType.SequenceStart)
            {
                // New sequence started
                SetExpectingKey(false);
            }
            else if (_tokenType == YamlTokenType.Key)
            {
                // Explicit key, expect value
                SetExpectingKey(false);
            }
            else if (_tokenType == YamlTokenType.Scalar || _tokenType == YamlTokenType.Alias)
            {
                int expectedIndent = GetIndentLevel(_currentDepth - 1);
                bool isContinuation = currentIndent > expectedIndent;

                if (IsExpectingKey() && !isContinuation)
                {
                    // Was expecting key, got scalar at correct indent -> it's a key
                    SetExpectingKey(false);
                }
                else
                {
                    // Was expecting value, or it's a continuation -> it's a value
                    SetExpectingKey(true);
                }
            }
        }

        // Clear expecting value flag if we parsed a value
        if (result && _expectingMappingValue)
        {
            if (_tokenType == YamlTokenType.Scalar ||
                _tokenType == YamlTokenType.SequenceStart ||
                _tokenType == YamlTokenType.MappingStart ||
                _tokenType == YamlTokenType.Alias)
            {
                _expectingMappingValue = false;
                
                // Track that we've completed a key:value on this line
                // But NOT for nested structures (MappingStart/SequenceStart) because
                // their content will be on subsequent lines
                if (_tokenType == YamlTokenType.Scalar || _tokenType == YamlTokenType.Alias)
                {
                    _hadValueOnLine = true;
                    _lastValueLine = _line;
                }
            }
        }

        // Mark document content for root-level content
        // This handles scalar roots, anchored scalars, tagged scalars, etc.
        if (result && _currentDepth == 0 && !_hasDocumentContent)
        {
            if (_tokenType == YamlTokenType.Scalar || 
                _tokenType == YamlTokenType.Alias ||
                _tokenType == YamlTokenType.SequenceStart ||
                _tokenType == YamlTokenType.MappingStart)
            {
                _hasDocumentContent = true;
            }
        }

        return result;
    }

    private void ValidateImplicitKey()
    {
        int savedConsumed = _consumed;
        
        // Skip spaces to find the colon
        while (_consumed < _buffer.Length)
        {
            byte b = CurrentByte();
            if (b != Space && b != Tab)
            {
                break;
            }
            _consumed++;
        }
        
        bool isValid = false;
        if (_consumed < _buffer.Length && CurrentByte() == MappingValue)
        {
            // Must be followed by whitespace to be a value indicator
            if (IsIndicatorFollowedByWhitespace(1))
            {
                isValid = true;
            }
        }
        
        _consumed = savedConsumed;
        
        if (!isValid)
        {
            throw new YamlException("Implicit keys in block mappings must be followed by ':'", Position);
        }
    }

    private bool ParseFlowContent()
    {
        SkipFlowWhitespaceAndComments();

        if (_consumed >= _buffer.Length)
        {
            if (_isFinalBlock)
            {
                throw new YamlException("Unexpected end of input in flow content", Position);
            }
            return false;
        }

        byte current = CurrentByte();
        
        // Check flow indentation: after crossing a line break, content must be MORE
        // indented than the enclosing block context's indentation level (YAML 1.2 spec section 7.1)
        ValidateFlowIndentation(current);
        
        // Check for invalid comment (must have space before # in flow context)
        if (current == Comment)
        {
            throw new YamlException("Comments in flow context must be preceded by whitespace", Position);
        }
        
        // Check for document markers in flow context - this is invalid
        if (IsAtStartOfLine() && (IsAtDocumentMarker(DocumentStartMarker) || IsAtDocumentMarker(DocumentEndMarker)))
        {
            throw new YamlException("Document markers are not allowed inside flow content", Position);
        }
        
        // Check for invalid comma at the beginning of flow collection
        if (current == CollectEntry && _awaitingFirstFlowEntry)
        {
            throw new YamlException("Invalid comma at the beginning of flow collection", Position);
        }
        
        // Check for missing comma between flow entries
        // After parsing an entry, if we see another entry without a comma, it's an error
        // BUT: MappingValue (:) is valid after a key, so don't require comma before it
        if (_needsFlowComma && current != CollectEntry && current != SequenceEnd && 
            current != MappingEndChar && current != MappingValue)
        {
            throw new YamlException("Missing comma between flow sequence entries", Position);
        }
        
        // Clear the awaiting first entry flag if we're parsing content
        if (current != SequenceEnd && current != MappingEndChar)
        {
            _awaitingFirstFlowEntry = false;
        }
        
        // In flow SEQUENCE context, when a scalar is followed by ':' on a new line,
        // it's trying to create an implicit mapping entry (single pair) which is invalid
        // if the key and ':' are on different lines.
        // In flow MAPPING context, the ':' can be on the next line after the key.
        // Check this BEFORE clearing _needsFlowComma
        if (current == MappingValue && _crossedLineBreakInFlow && _needsFlowComma && !IsCurrentCollectionMapping())
        {
            throw new YamlException("Implicit keys in flow context must be on a single line", Position);
        }
        
        // Clear the needs comma flag when we see a comma or end
        // Also clear the crossed line break flag since we've now acted on it
        if (current == CollectEntry || current == SequenceEnd || current == MappingEndChar || current == MappingValue)
        {
            _needsFlowComma = false;
            _crossedLineBreakInFlow = false;
        }

        bool result = current switch
        {
            SequenceEnd => ParseFlowSequenceEnd(),
            MappingEndChar => ParseFlowMappingEnd(),
            CollectEntry => ParseFlowEntry(),
            MappingValue => ParseFlowMappingValue(),
            SequenceStart => ParseFlowSequence(),
            MappingStartChar => ParseFlowMapping(),
            SingleQuote => ParseSingleQuotedScalar(),
            DoubleQuote => ParseDoubleQuotedScalar(),
            AnchorChar => ParseAnchor(),
            AliasChar => ParseAlias(),
            TagChar => ParseTag(),
            // Block sequence indicator '-' is invalid in flow context when followed by whitespace
            SequenceEntry when IsIndicatorFollowedByWhitespace(1) => 
                throw new YamlException("Block sequence indicator '-' is not allowed in flow context", Position),
            // Explicit key indicator '?' is valid in flow context
            MappingKey when IsIndicatorFollowedByWhitespace(1) => ParseFlowExplicitKey(),
            _ => ParsePlainScalar()
        };
        
        // After parsing a scalar or alias in flow context, we need a comma before the next entry
        // (unless it's followed by : for a mapping key)
        if (result && (_tokenType == YamlTokenType.Scalar || _tokenType == YamlTokenType.Alias))
        {
            _needsFlowComma = true;
            // Reset line break tracking - any future line break crossing will be between
            // this content and the next token (which is what we care about)
            _crossedLineBreakInFlow = false;
            
            // Toggle key/value state in flow mappings
            // If we were expecting a key, we got a key, now expect value (but : must follow)
            // If we were expecting a value, we got a value, now expect next key
            if (IsCurrentCollectionMapping())
            {
                if (IsExpectingKey())
                {
                    // We just parsed a key - but we need to wait for ':' before marking value expected
                    // The ':' handler (ParseFlowMappingValue) will be called, which parses the value
                    // So we don't change state here - we stay "expecting key" until we see ':'
                    // But actually we DO toggle because the key was just parsed!
                    SetExpectingKey(false); // Now expecting value
                }
                else
                {
                    // We just parsed a value, now expect next key
                    SetExpectingKey(true);
                }
            }
        }
        
        return result;
    }
    private bool ParseDirective()
    {
        _consumed++; // Skip %

        SkipSpaces();

        if (MatchesBytes(YamlDirective))
        {
            // Check for duplicate YAML directive
            if (_hasYamlDirective)
            {
                throw new YamlException("Duplicate YAML directive", Position);
            }
            _hasYamlDirective = true;
            
            _consumed += 4;
            SkipSpaces();
            
            // Parse version number (must be digits and dots only)
            int versionStart = _consumed;
            while (_consumed < _buffer.Length)
            {
                byte current = CurrentByte();
                if (IsWhitespaceOrLineBreak(current))
                {
                    break;
                }
                // Version can only contain digits and dots
                if (!IsDigit(current) && current != (byte)'.')
                {
                    throw new YamlException("Invalid character in YAML directive version", Position);
                }
                _consumed++;
            }
            
            _valueSpan = _buffer.Slice(versionStart, _consumed - versionStart);
            _tokenType = YamlTokenType.VersionDirective;
            
            // Check for extra content after version
            SkipSpaces();
            if (_consumed < _buffer.Length && !IsLineBreak(CurrentByte()))
            {
                byte current = CurrentByte();
                if (current != Comment)
                {
                    throw new YamlException("Extra content after YAML directive version", Position);
                }
            }
            
            SkipToEndOfLine();
            
            // Mark that we need a document start marker
            _expectDocumentStart = true;
            return true;
        }

        if (MatchesBytes(TagDirective))
        {
            _consumed += 3;
            SkipSpaces();
            
            // Parse tag handle (e.g., "!prefix!")
            int handleStart = _consumed;
            while (_consumed < _buffer.Length && !IsWhitespace(CurrentByte()))
            {
                _consumed++;
            }
            
            // Register the tag handle if it's a named handle (starts and ends with !)
            ReadOnlySpan<byte> handle = _buffer.Slice(handleStart, _consumed - handleStart);
            if (handle.Length >= 2 && handle[0] == TagChar && handle[^1] == TagChar)
            {
                // It's a named tag handle like !prefix! - register it
                _tagHandles.Register(handle);
            }
            
            _valueSpan = handle;
            _tokenType = YamlTokenType.TagDirective;
            
            SkipToEndOfLine();
            
            // Mark that we need a document start marker
            _expectDocumentStart = true;
            return true;
        }

        // Unknown directive - skip with warning
        SkipToEndOfLine();
        return Read(); // Continue to next token
    }

    private bool ParseBlockSequenceEntry()
    {
        int dashColumn = _consumed - _lineStart;
        _consumed++; // Skip -
        int posBeforeSkip = _consumed;
        SkipSpaces();
        bool skippedTabs = HasTabBetween(posBeforeSkip, _consumed);
        
        // Check if we have content on the same line
        if (_consumed >= _buffer.Length || IsLineBreak(CurrentByte()))
        {
            // No content on same line - look ahead to next line for indented content
            if (TryParseMultilineSequenceEntry(dashColumn))
            {
                return true;
            }
            
            // Truly empty entry (no content follows) - emit as empty scalar (null)
            return EmitEmptyScalar();
        }
        
        // Parse the entry content based on what comes after -
        byte current = CurrentByte();
        int entryIndent = _consumed - _lineStart;
        
        // YAML spec: Tabs cannot be used for indentation before block indicators
        if (skippedTabs && IsBlockIndicatorFollowedByWhitespace(current))
        {
            throw new YamlException("Tabs cannot be used for indentation before block indicators", Position);
        }
        
        // Check for nested mapping structures
        if (TryStartNestedMapping(current, entryIndent))
        {
            return true;
        }
        
        return ParseEntryContent(current);
    }
    
    /// <summary>
    /// Attempts to parse content on the next line as the value of a sequence entry.
    /// Called when '-' is followed by a line break (e.g., "-\n  value").
    /// </summary>
    private bool TryParseMultilineSequenceEntry(int dashColumn)
    {
        if (_consumed >= _buffer.Length || !IsLineBreak(CurrentByte()))
        {
            return false;
        }
        
        ConsumeLineBreak();
        SkipWhitespaceAndComments();
        
        if (_consumed >= _buffer.Length || IsLineBreak(CurrentByte()))
        {
            return false;
        }
        
        int nextLineIndent = _consumed - _lineStart;
        
        // Content must be MORE indented than the '-' to be part of this entry
        if (nextLineIndent <= dashColumn)
        {
            return false;
        }
        
        byte current = CurrentByte();
        
        // Check for nested collection structures first
        if (TryStartNestedMapping(current, nextLineIndent) ||
            TryStartNestedSequence(current, nextLineIndent))
        {
            return true;
        }
        
        return ParseEntryContent(current);
    }
    
    /// <summary>
    /// Attempts to start a nested block mapping at the given indentation.
    /// </summary>
    private bool TryStartNestedMapping(byte current, int indent)
    {
        bool isExplicitKey = current == MappingKey && IsIndicatorFollowedByWhitespace(1);
        bool isImplicitKey = LooksLikeImplicitMappingKey();
        bool isEmptyKey = current == MappingValue && IsIndicatorFollowedByWhitespace(1);
        
        if (isExplicitKey || isImplicitKey || isEmptyKey)
        {
            SetIndentLevel(_currentDepth, indent);
            _tokenType = YamlTokenType.MappingStart;
            _collectionStyle = CollectionStyle.Block;
            PushCollectionType(true);
            PushFlowContext(false);
            _currentDepth++;
            SetExpectingKey(true);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Attempts to start a nested block sequence at the given indentation.
    /// </summary>
    private bool TryStartNestedSequence(byte current, int indent)
    {
        if (current == SequenceEntry && IsIndicatorFollowedByWhitespace(1))
        {
            SetIndentLevel(_currentDepth, indent);
            _tokenType = YamlTokenType.SequenceStart;
            _collectionStyle = CollectionStyle.Block;
            PushCollectionType(false);
            PushFlowContext(false);
            _currentDepth++;
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Parses the content of a sequence or mapping entry based on the current byte.
    /// </summary>
    private bool ParseEntryContent(byte current)
    {
        return current switch
        {
            SequenceEntry when IsIndicatorFollowedByWhitespace(1) => ParseNestedSequence(),
            SequenceStart => ParseFlowSequence(),
            MappingStartChar => ParseFlowMapping(),
            SingleQuote => ParseSingleQuotedScalar(),
            DoubleQuote => ParseDoubleQuotedScalar(),
            Literal => ParseLiteralBlockScalar(),
            Folded => ParseFoldedBlockScalar(),
            AnchorChar => ParseAnchor(),
            AliasChar => ParseAlias(),
            TagChar => ParseTag(),
            _ => ParsePlainScalar()
        };
    }
    
    /// <summary>
    /// Emits an empty/null scalar value.
    /// </summary>
    private bool EmitEmptyScalar()
    {
        _valueSpan = default;
        _tokenType = YamlTokenType.Scalar;
        _scalarStyle = ScalarStyle.Plain;
        return true;
    }
    
    /// <summary>
    /// Validates flow content indentation after crossing a line break.
    /// Flow content must be more indented than the enclosing block context (YAML 1.2 spec 7.1).
    /// </summary>
    /// <param name="current">The current byte being parsed.</param>
    private readonly void ValidateFlowIndentation(byte current)
    {
        // Only check after crossing a line break in flow context
        if (!_crossedLineBreakInFlow || _currentDepth <= 0)
        {
            return;
        }
        
        // Closing brackets ] and } are allowed at any indentation
        if (current == SequenceEnd || current == MappingEndChar)
        {
            return;
        }
        
        // Check against the enclosing block context's indentation
        int blockIndent = GetEnclosingBlockIndent();
        if (blockIndent >= 0)
        {
            int currentColumn = _consumed - _lineStart;
            if (currentColumn <= blockIndent)
            {
                throw new YamlException($"Flow content must be indented more than column {blockIndent + 1}", Position);
            }
        }
    }
    
    private bool ParseNestedSequence()
    {
        // Use column position for the nested sequence's indent level
        int seqIndent = _consumed - _lineStart;
        SetIndentLevel(_currentDepth, seqIndent);
        
        // Start a nested sequence
        _tokenType = YamlTokenType.SequenceStart;
        _collectionStyle = CollectionStyle.Block;
        PushCollectionType(false); // sequence
        PushFlowContext(false); // block context
        _currentDepth++;
        return true;
    }

    private bool ParseExplicitMappingKey()
    {
        _consumed++; // Skip ?
        int posBeforeSkip = _consumed;
        SkipSpaces();
        bool skippedTabs = HasTabBetween(posBeforeSkip, _consumed);
        
        // YAML spec 8.2.3: The separation between the indicator and the content 
        // must be at least one space character, or a newline.
        // Tabs are NOT valid as separation after block indicators.
        if (skippedTabs && _consumed < _buffer.Length && !IsLineBreak(CurrentByte()))
        {
            throw new YamlException("Tabs cannot be used as separation after block indicators, use space instead", Position);
        }
        
        _tokenType = YamlTokenType.Key;
        return true;
    }

    private bool ParseFlowExplicitKey()
    {
        // In flow context, '?' starts an explicit key
        // If we're in a flow sequence, this creates a mapping entry
        if (!IsCurrentCollectionMapping())
        {
            // We're in a flow sequence - start an implicit mapping for this entry
            _tokenType = YamlTokenType.MappingStart;
            _collectionStyle = CollectionStyle.Flow;
            PushCollectionType(true); // mapping
            PushFlowContext(true); // flow context
            _currentDepth++;
            SetExpectingKey(true);
            // Consume ? and prepare to parse the key content
            _consumed++; // Skip ?
            SkipFlowWhitespaceAndComments();
            return true;
        }
        
        // Already in a flow mapping - consume ? and emit Key token
        _consumed++; // Skip ?
        SkipFlowWhitespaceAndComments();
        
        _tokenType = YamlTokenType.Key;
        return true;
    }

    private bool ParseMappingValue(bool isExplicitValueIndicator)
    {
        // Reset token type to indicate we're starting a new node (the value).
        // This allows the value to have its own anchor even if the key had one.
        _tokenType = YamlTokenType.None;
        
        _consumed++; // Skip :
        int posBeforeSkip = _consumed;
        SkipSpaces();
        bool skippedTabs = HasTabBetween(posBeforeSkip, _consumed);
        
        // YAML spec: Tabs cannot be used for indentation.
        // If we skipped tabs and find another block indicator, that's invalid.
        if (skippedTabs && _consumed < _buffer.Length && IsBlockIndicatorFollowedByWhitespace(CurrentByte()))
        {
            throw new YamlException("Tabs cannot be used for indentation before block indicators", Position);
        }
        
        // We just parsed a key, now we're looking for a value
        _expectingMappingValue = true;
        
        // For node-level API, don't emit Value token - parse the value content directly
        // Check if there's actual content after the : on this line (not just a comment)
        // A comment (# followed by anything) means the value is on the next line
        if (_consumed < _buffer.Length && !IsLineBreak(CurrentByte()) && CurrentByte() != Comment)
        {
            // Parse the value based on what comes after :
            byte current = CurrentByte();
            
            // Block sequence entry (- ) is NOT valid on the same line as implicit :
            // e.g., "key: - a" is invalid, the sequence must start on a new line
            // But for EXPLICIT value indicators like "? key\n: - a", it IS valid
            if (!isExplicitValueIndicator && current == SequenceEntry && IsIndicatorFollowedByWhitespace(1))
            {
                throw new YamlException("Block sequence entry not allowed on same line as mapping value", Position);
            }
            
            // Handle block sequence entry on same line as EXPLICIT value indicator
            // e.g., ": - one" where : is at column 0 after explicit ? key
            if (isExplicitValueIndicator && current == SequenceEntry && IsIndicatorFollowedByWhitespace(1))
            {
                // Start a sequence at this position
                int seqIndent = _consumed - _lineStart;
                SetIndentLevel(_currentDepth, seqIndent);
                
                _tokenType = YamlTokenType.SequenceStart;
                _collectionStyle = CollectionStyle.Block;
                PushCollectionType(false); // sequence
                PushFlowContext(false); // block context
                _currentDepth++;
                _expectingMappingValue = false;
                return true;
            }
            
            // Check if this inline value is itself a compact mapping (e.g., "key: value")
            // This is ONLY valid for explicit value indicators like:
            //   ? earth: blue
            //   : moon: white   <- "moon: white" is a compact mapping value
            // NOT valid for implicit key:value on same line like:
            //   a: b: c: d      <- "b: c: d" is NOT a valid compact mapping
            if (isExplicitValueIndicator &&
                current != SequenceStart && current != MappingStartChar &&
                current != SingleQuote && current != DoubleQuote &&
                current != Literal && current != Folded &&
                current != AnchorChar && current != AliasChar && current != TagChar)
            {
                // Plain scalar - check if it looks like an implicit mapping key
                if (LooksLikeImplicitMappingKey())
                {
                    // This is a compact mapping value - emit MappingStart
                    // The key:value content will be parsed in subsequent Read() calls
                    _tokenType = YamlTokenType.MappingStart;
                    _collectionStyle = CollectionStyle.Block;
                    PushCollectionType(true); // mapping
                    PushFlowContext(false); // block context
                    int mappingIndent = _consumed - _lineStart;
                    SetIndentLevel(_currentDepth, mappingIndent);
                    _currentDepth++;
                    SetExpectingKey(true);
                    _expectingMappingValue = false;
                    return true;
                }
            }
            
            // For anchors and tags, we're still expecting the actual value after them
            // Aliases ARE values (references), so they clear the flag
            if (current != AnchorChar && current != TagChar)
            {
                _expectingMappingValue = false;
                
                // Track that we're completing a key:value on this line
                // (used to detect invalid mid-line colons like a: b: c)
                _hadValueOnLine = true;
                _lastValueLine = _line;
            }
            
            return current switch
            {
                SequenceStart => ParseFlowSequence(),
                MappingStartChar => ParseFlowMapping(),
                SingleQuote => ParseSingleQuotedScalar(),
                DoubleQuote => ParseDoubleQuotedScalar(),
                Literal => ParseLiteralBlockScalar(),
                Folded => ParseFoldedBlockScalar(),
                AnchorChar => ParseAnchor(),
                AliasChar => ParseAlias(),
                TagChar => ParseTag(),
                _ => ParsePlainScalar()
            };
        }
        
        // Value is on the next line (or missing) - skip whitespace and check
        SkipWhitespaceAndComments();
        
        if (_consumed >= _buffer.Length)
        {
            // End of input - empty/null value
            _valueSpan = default;
            _tokenType = YamlTokenType.Scalar;
            _scalarStyle = ScalarStyle.Plain;
            _expectingMappingValue = false;
            // Don't call SetExpectingKey here - state update happens in ParseBlockContent
            return true;
        }
        
        // Check what's on the next line
        byte next = CurrentByte();
        // Use column position for nested collection indent tracking
        int valueIndent = _consumed - _lineStart;
        
        // If it's a block sequence indicator, determine if it's a VALUE or a SIBLING
        // When we're in a mapping inside a sequence, and the `-` is at the sequence's
        // indentation level, it's a new sibling item (not a value for the current key)
        if (next == SequenceEntry && IsIndicatorFollowedByWhitespace(1))
        {
            // Check if we're inside a mapping that's inside a sequence (grandparent is sequence)
            // _currentDepth points to current mapping, so grandparent is at _currentDepth - 2
            bool grandparentIsSequence = _currentDepth >= 2 && 
                ((_collectionStack & (1L << (_currentDepth - 2))) == 0);
            
            if (grandparentIsSequence)
            {
                // Get the sequence's indentation level
                int sequenceIndent = GetIndentLevel(_currentDepth - 2);
                
                // If the `-` is at the sequence's indentation, it's a new sibling
                // The current key has a null value
                if (valueIndent <= sequenceIndent)
                {
                    _valueSpan = default;
                    _tokenType = YamlTokenType.Scalar;
                    _scalarStyle = ScalarStyle.Plain;
                    _expectingMappingValue = false;
                    // Don't call SetExpectingKey here - state update happens in ParseBlockContent
                    return true;
                }
            }
            
            // Otherwise, the `-` starts a nested sequence as the value
            // Record the indentation level for this nested collection
            SetIndentLevel(_currentDepth, valueIndent);
            
            // Update parent state: we are parsing a value, so next time we expect a key.
            if (IsCurrentCollectionMapping())
            {
                SetExpectingKey(true);
            }
            
            _tokenType = YamlTokenType.SequenceStart;
            _collectionStyle = CollectionStyle.Block;
            PushCollectionType(false); // sequence
            PushFlowContext(false); // block context
            _currentDepth++;
            return true;
        }

        // If it's a nested explicit mapping
        if (next == MappingKey && IsIndicatorFollowedByWhitespace(1))
        {
            // Record the indentation level for this nested collection
            SetIndentLevel(_currentDepth, valueIndent);
            
            // Update parent state: we are parsing a value, so next time we expect a key.
            if (IsCurrentCollectionMapping())
            {
                SetExpectingKey(true);
            }
            
            _tokenType = YamlTokenType.MappingStart;
            _collectionStyle = CollectionStyle.Block;
            PushCollectionType(true); // mapping
            PushFlowContext(false); // block context
            _currentDepth++;
            SetExpectingKey(true);
            return true;
        }
        
        // Check if the next content is at a dedented level (parent or less)
        // AND it's not a block indicator that could start a value
        // If so, the current key has a null value
        int parentIndent = _currentDepth > 0 ? GetIndentLevel(_currentDepth - 1) : 0;
        bool isBlockIndicator = (next == SequenceEntry || next == MappingKey) && IsIndicatorFollowedByWhitespace(1);
        if (valueIndent <= parentIndent && !IsFlowIndicator(next) && !isBlockIndicator)
        {
            // Content is at or before the parent's indentation and not a block indicator
            // This means it's a dedented key/value at a higher level, so current value is null
            _valueSpan = default;
            _tokenType = YamlTokenType.Scalar;
            _scalarStyle = ScalarStyle.Plain;
            _expectingMappingValue = false;
            // Don't call SetExpectingKey here - state update happens in ParseBlockContent
            return true;
        }
        
        // If it looks like a nested mapping (key: value pattern)
        if (!IsFlowIndicator(next) && LooksLikeImplicitMappingKey())
        {
            // Record the indentation level for this nested collection
            SetIndentLevel(_currentDepth, valueIndent);
            
            // Update parent state: we are parsing a value, so next time we expect a key.
            if (IsCurrentCollectionMapping())
            {
                SetExpectingKey(true);
            }
            
            _tokenType = YamlTokenType.MappingStart;
            _collectionStyle = CollectionStyle.Block;
            PushCollectionType(true); // mapping
            PushFlowContext(false); // block context
            _currentDepth++;
            SetExpectingKey(true);
            return true;
        }
        
        // Otherwise, parse as a scalar or flow content
        return next switch
        {
            SequenceStart => ParseFlowSequence(),
            MappingStartChar => ParseFlowMapping(),
            SingleQuote => ParseSingleQuotedScalar(),
            DoubleQuote => ParseDoubleQuotedScalar(),
            Literal => ParseLiteralBlockScalar(),
            Folded => ParseFoldedBlockScalar(),
            AnchorChar => ParseAnchor(),
            AliasChar => ParseAlias(),
            TagChar => ParseTag(),
            _ => ParsePlainScalar()
        };
    }

    private bool ParseFlowSequence()
    {
        // Record the column of the flow indicator for indentation checking
        // YAML spec: content on continuation lines must be indented more than the
        // current indentation level (which is the column where the flow started, minus 1 to be safe)
        int flowColumn = _consumed - _lineStart;
        
        _consumed++; // Skip [
        PushCollectionType(false); // sequence
        PushFlowContext(true); // mark as flow
        _currentDepth++;
        
        // Store the flow start column for checking on continuation lines
        // We use the column of [ as the minimum - content must be > this column's indent level
        SetIndentLevel(_currentDepth - 1, flowColumn);
        
        _awaitingFirstFlowEntry = true; // Next content will be first entry
        _needsFlowComma = false; // Reset comma requirement for new collection
        
        // If this is a root-level flow sequence, mark document content
        if (_currentDepth == 1)
        {
            _hasDocumentContent = true;
        }
        
        if (_currentDepth > _options.MaxDepth)
        {
            throw new YamlException($"Maximum depth of {_options.MaxDepth} exceeded", Position);
        }
        
        _tokenType = YamlTokenType.SequenceStart;
        _collectionStyle = CollectionStyle.Flow;
        _state = ParserState.InFlowContent;
        return true;
    }

    private bool ParseFlowSequenceEnd()
    {
        // If we're inside an implicit mapping within the sequence, close it first
        if (IsCurrentCollectionMapping())
        {
            // This is an implicit mapping entry (created by ? or implicit key:value)
            // Close the mapping first, we'll handle ] on the next Read()
            _currentDepth--;
            PopCollectionType();
            PopFlowContext();
            _tokenType = YamlTokenType.MappingEnd;
            _collectionStyle = CollectionStyle.Flow;
            return true;
        }
        
        _consumed++; // Skip ]
        _currentDepth--;
        PopCollectionType();
        PopFlowContext();
        
        _tokenType = YamlTokenType.SequenceEnd;
        _collectionStyle = CollectionStyle.Flow;
        
        // Check if we're still inside a flow context (nested flow or enclosing flow)
        if (!IsInsideFlowContext())
        {
            _state = ParserState.InBlockContent;
            
            // Validate nothing follows on the same line except valid content
            // (comments must have preceding whitespace)
            ValidateAfterFlowCollectionEnd();
        }
        
        return true;
    }

    private bool ParseFlowMapping()
    {
        // Record the column of the flow indicator for indentation checking
        // YAML spec: content on continuation lines must be indented more than the
        // current indentation level (which is the column where the flow started, minus 1 to be safe)
        int flowColumn = _consumed - _lineStart;
        
        _consumed++; // Skip {
        PushCollectionType(true); // mapping
        PushFlowContext(true); // mark as flow
        _currentDepth++;
        
        // Store the flow start column for checking on continuation lines
        // We use the column of { as the minimum - content must be > this column's indent level
        SetIndentLevel(_currentDepth - 1, flowColumn);
        
        SetExpectingKey(true); // In flow mapping, first expect a key
        _awaitingFirstFlowEntry = true; // Next content will be first entry
        _needsFlowComma = false; // Reset comma requirement for new collection
        
        // If this is a root-level flow mapping, mark document content
        if (_currentDepth == 1)
        {
            _hasDocumentContent = true;
        }
        
        if (_currentDepth > _options.MaxDepth)
        {
            throw new YamlException($"Maximum depth of {_options.MaxDepth} exceeded", Position);
        }
        
        _tokenType = YamlTokenType.MappingStart;
        _collectionStyle = CollectionStyle.Flow;
        _state = ParserState.InFlowContent;
        return true;
    }

    private bool ParseFlowMappingEnd()
    {
        _consumed++; // Skip }
        _currentDepth--;
        
        _tokenType = YamlTokenType.MappingEnd;
        _collectionStyle = CollectionStyle.Flow;
        
        // Check if we're still inside a flow context
        if (!IsInsideFlowContext())
        {
            _state = ParserState.InBlockContent;
            
            // Validate nothing follows on the same line except valid content
            // (comments must have preceding whitespace)
            ValidateAfterFlowCollectionEnd();
        }
        
        return true;
    }

    private bool ParseFlowEntry()
    {
        _consumed++; // Skip ,
        
        // Skip whitespace after comma, but track if we had whitespace
        bool hadWhitespace = false;
        while (_consumed < _buffer.Length)
        {
            byte b = CurrentByte();
            if (b == Space || b == Tab)
            {
                hadWhitespace = true;
                _consumed++;
            }
            else if (IsLineBreak(b))
            {
                hadWhitespace = true;
                _crossedLineBreakInFlow = true; // Track that we crossed a line break
                ConsumeLineBreak();
            }
            else if (b == Comment)
            {
                // # must be preceded by whitespace
                if (!hadWhitespace)
                {
                    throw new YamlException("Invalid comment after comma - comments must be preceded by whitespace", Position);
                }
                SkipToEndOfLine();
                hadWhitespace = false;
            }
            else
            {
                break;
            }
        }
        
        if (_consumed >= _buffer.Length)
        {
            throw new YamlException("Unexpected end of input after comma in flow content", Position);
        }
        
        byte current = CurrentByte();
        
        // Check for flow indentation after crossing a line break
        ValidateFlowIndentation(current);
        
        // Check for extra comma (invalid)
        if (current == CollectEntry)
        {
            throw new YamlException("Invalid extra comma in flow sequence", Position);
        }
        
        // The next Read() call will parse the actual entry
        return Read();
    }

    private bool ParseFlowMappingValue()
    {
        _consumed++; // Skip :
        SkipSpaces();
        
        _parsingFlowMappingValue = true; // Track that we're parsing a value in flow mapping
        
        // For node-level API, don't emit Value token - parse the value content directly
        if (_consumed >= _buffer.Length)
        {
            _parsingFlowMappingValue = false;
            throw new YamlException("Unexpected end of input in flow mapping", Position);
        }
        
        byte current = CurrentByte();
        
        // Check for flow indicators that end the value early
        if (current == CollectEntry || current == MappingEndChar)
        {
            // Empty value
            _valueSpan = default;
            _tokenType = YamlTokenType.Scalar;
            _scalarStyle = ScalarStyle.Plain;
            _parsingFlowMappingValue = false;
            return true;
        }
        
        // Check for line break - value might be on next line
        if (IsLineBreak(current))
        {
            // Value is on next line - return empty/null for now
            // The next Read() will handle the actual value
            _valueSpan = default;
            _tokenType = YamlTokenType.Scalar;
            _scalarStyle = ScalarStyle.Plain;
            _parsingFlowMappingValue = false;
            return true;
        }
        
        bool result = current switch
        {
            SequenceStart => ParseFlowSequence(),
            MappingStartChar => ParseFlowMapping(),
            SingleQuote => ParseSingleQuotedScalar(),
            DoubleQuote => ParseDoubleQuotedScalar(),
            AnchorChar => ParseAnchor(),
            AliasChar => ParseAlias(),
            TagChar => ParseTag(),
            _ => ParsePlainScalar()
        };
        
        _parsingFlowMappingValue = false;
        return result;
    }

    private bool ParseAnchor()
    {
        // An anchor cannot follow another anchor on the SAME node
        // e.g., "&anchor1 &anchor2 value" is invalid
        // But anchors on different nodes are valid:
        //   &keyAnchor key: &valueAnchor value
        // When called from ParseMappingValue, _tokenType will be None (reset at entry)
        // to indicate we're starting a new node.
        if (_tokenType == YamlTokenType.Anchor)
        {
            throw new YamlException("A node can only have one anchor", Position);
        }
        
        // Record the column of the anchor before consuming
        _lastAnchorOrTagColumn = _consumed - _lineStart + 1;
        _consumed++; // Skip &
        
        int nameStart = _consumed;
        while (_consumed < _buffer.Length && IsAnchorChar(CurrentByte()))
        {
            _consumed++;
        }
        
        if (_consumed == nameStart)
        {
            throw new YamlException("Expected anchor name after &", Position);
        }
        
        _valueSpan = _buffer.Slice(nameStart, _consumed - nameStart);
        _tokenType = YamlTokenType.Anchor;
        return true;
    }
    private bool ParseAlias()
    {
        // An alias cannot follow an anchor - anchors attach to content, aliases ARE content
        if (_tokenType == YamlTokenType.Anchor)
        {
            throw new YamlException("An alias cannot follow an anchor - anchors attach to nodes, aliases are node references", Position);
        }
        
        _consumed++; // Skip *
        
        int nameStart = _consumed;
        while (_consumed < _buffer.Length && IsAnchorChar(CurrentByte()))
        {
            _consumed++;
        }
        
        if (_consumed == nameStart)
        {
            throw new YamlException("Expected alias name after *", Position);
        }
        
        _valueSpan = _buffer.Slice(nameStart, _consumed - nameStart);
        _tokenType = YamlTokenType.Alias;
        return true;
    }

    private bool ParseTag()
    {
        // Record the column of the tag (1-based) before consuming
        _lastAnchorOrTagColumn = _consumed - _lineStart + 1;
        int tagStart = _consumed;
        _consumed++; // Skip first !
        
        if (_consumed < _buffer.Length && CurrentByte() == TagChar)
        {
            // Secondary tag handle !!
            _consumed++;
        }
        else if (_consumed < _buffer.Length && CurrentByte() == (byte)'<')
        {
            // Verbatim tag !<...>
            _consumed++;
            while (_consumed < _buffer.Length && CurrentByte() != (byte)'>')
            {
                _consumed++;
            }
            if (_consumed < _buffer.Length)
            {
                _consumed++; // Skip >
            }
            _valueSpan = _buffer.Slice(tagStart, _consumed - tagStart);
            _tokenType = YamlTokenType.Tag;
            SkipSpaces();
            return true;
        }
        
        // Check for named tag handle pattern: !name!suffix
        // The handle portion !name! must be defined via TAG directive
        int handleEnd = _consumed;
        while (handleEnd < _buffer.Length && IsTagChar(_buffer[handleEnd]) && _buffer[handleEnd] != TagChar)
        {
            handleEnd++;
        }
        
        // If there's a second ! after some characters, it's a named handle
        if (handleEnd < _buffer.Length && _buffer[handleEnd] == TagChar && handleEnd > _consumed)
        {
            // Found a named handle like !prefix!
            ReadOnlySpan<byte> handle = _buffer.Slice(tagStart, handleEnd - tagStart + 1);
            
            // Validate that this handle was declared via TAG directive
            if (!_tagHandles.IsRegistered(handle))
            {
                throw new YamlException($"Tag shorthand '{System.Text.Encoding.UTF8.GetString(handle)}' is not defined. A TAG directive is required.", Position);
            }
            
            // Move past the handle and consume the suffix
            _consumed = handleEnd + 1;
        }
        
        // Tag suffix
        while (_consumed < _buffer.Length && IsTagChar(CurrentByte()))
        {
            _consumed++;
        }
        
        _valueSpan = _buffer.Slice(tagStart, _consumed - tagStart);
        _tokenType = YamlTokenType.Tag;
        SkipSpaces();
        
        return true;
    }

    private bool ParseSingleQuotedScalar()
    {
        _consumed++; // Skip opening '
        int valueStart = _consumed;
        int startLine = _line; // Track starting line for multiline detection
        
        // Track the minimum required indentation for continuation lines in block context
        // This is the column where the opening quote started
        int quoteStartColumn = valueStart - 1 - _lineStart;
        bool inBlockContext = !IsInsideFlowContext();
        
        while (_consumed < _buffer.Length)
        {
            byte current = CurrentByte();
            
            if (current == SingleQuote)
            {
                // Check for escaped quote ''
                if (_consumed + 1 < _buffer.Length && _buffer[_consumed + 1] == SingleQuote)
                {
                    _consumed += 2;
                    continue;
                }
                
                // End of string
                _valueSpan = _buffer.Slice(valueStart, _consumed - valueStart);
                _consumed++; // Skip closing '
                _tokenType = YamlTokenType.Scalar;
                _scalarStyle = ScalarStyle.SingleQuoted;
                
                // Validate trailing content in block context
                // Pass whether the scalar spanned multiple lines
                if (!IsInsideFlowContext())
                {
                    ValidateAfterQuotedScalar(_line > startLine);
                }
                
                return true;
            }
            
            if (current == LineFeed)
            {
                _line++;
                _lineStart = _consumed + 1;
                _consumed++;
                
                // Check for document markers at start of line - these are invalid inside quoted strings
                if (_consumed < _buffer.Length && IsAtDocumentMarker(DocumentStartMarker))
                {
                    throw new YamlException("Document start marker '---' is not allowed inside a single-quoted string", Position);
                }
                if (_consumed < _buffer.Length && IsAtDocumentMarker(DocumentEndMarker))
                {
                    throw new YamlException("Document end marker '...' is not allowed inside a single-quoted string", Position);
                }
                
                // In block context, validate that continuation lines have proper indentation
                // YAML spec: continuation lines in multiline quoted scalars must be indented
                if (inBlockContext && _consumed < _buffer.Length)
                {
                    // Skip leading whitespace to find content
                    int lineContentStart = _consumed;
                    while (lineContentStart < _buffer.Length && 
                           (_buffer[lineContentStart] == Space || _buffer[lineContentStart] == Tab))
                    {
                        lineContentStart++;
                    }
                    
                    // If the line has content (not empty and not just closing quote at column 0)
                    int lineIndent = lineContentStart - _lineStart;
                    if (lineContentStart < _buffer.Length && 
                        !IsLineBreak(_buffer[lineContentStart]) &&
                        lineIndent == 0 && quoteStartColumn > 0)
                    {
                        // Content at column 0 when the quote started after column 0 - invalid
                        throw new YamlException("Multiline quoted scalar continuation line has wrong indentation", Position);
                    }
                }
                
                continue;
            }
            
            _consumed++;
        }
        
        if (_isFinalBlock)
        {
            throw new YamlException("Unterminated single-quoted string", Position);
        }
        
        return false; // Need more data
    }

    private bool ParseDoubleQuotedScalar()
    {
        _consumed++; // Skip opening "
        int valueStart = _consumed;
        int startLine = _line; // Track starting line for multiline detection
        
        // Track the minimum required indentation for continuation lines in block context
        // This is the column where the opening quote started
        int quoteStartColumn = valueStart - 1 - _lineStart;
        bool inBlockContext = !IsInsideFlowContext();
        
        while (_consumed < _buffer.Length)
        {
            byte current = CurrentByte();
            
            if (current == (byte)'\\' && _consumed + 1 < _buffer.Length)
            {
                // Validate escape sequence per YAML spec
                byte escaped = _buffer[_consumed + 1];
                if (!IsValidEscapeChar(escaped))
                {
                    throw new YamlException($"Invalid escape sequence: \\{(char)escaped}", Position);
                }
                
                // Skip the escape sequence
                _consumed += 2;
                
                // Handle hex escapes that need additional characters
                if (escaped == (byte)'x' && _consumed + 2 <= _buffer.Length)
                {
                    _consumed += 2; // 2 hex digits
                }
                else if (escaped == (byte)'u' && _consumed + 4 <= _buffer.Length)
                {
                    _consumed += 4; // 4 hex digits
                }
                else if (escaped == (byte)'U' && _consumed + 8 <= _buffer.Length)
                {
                    _consumed += 8; // 8 hex digits
                }
                
                continue;
            }
            
            if (current == DoubleQuote)
            {
                _valueSpan = _buffer.Slice(valueStart, _consumed - valueStart);
                _consumed++; // Skip closing "
                _tokenType = YamlTokenType.Scalar;
                _scalarStyle = ScalarStyle.DoubleQuoted;
                
                // Validate trailing content in block context
                // Pass whether the scalar spanned multiple lines
                if (!IsInsideFlowContext())
                {
                    ValidateAfterQuotedScalar(_line > startLine);
                }
                
                return true;
            }
            
            if (current == LineFeed)
            {
                _line++;
                _lineStart = _consumed + 1;
                _consumed++;
                
                // Check for document markers at start of line - these are invalid inside quoted strings
                if (_consumed < _buffer.Length && IsAtDocumentMarker(DocumentStartMarker))
                {
                    throw new YamlException("Document start marker '---' is not allowed inside a double-quoted string", Position);
                }
                if (_consumed < _buffer.Length && IsAtDocumentMarker(DocumentEndMarker))
                {
                    throw new YamlException("Document end marker '...' is not allowed inside a double-quoted string", Position);
                }
                
                // In block context, validate that continuation lines have proper indentation
                // YAML spec: continuation lines in multiline quoted scalars must be indented
                if (inBlockContext && _consumed < _buffer.Length)
                {
                    // Skip leading whitespace to find content
                    int lineContentStart = _consumed;
                    while (lineContentStart < _buffer.Length && 
                           (_buffer[lineContentStart] == Space || _buffer[lineContentStart] == Tab))
                    {
                        lineContentStart++;
                    }
                    
                    // If the line has content (not empty and not just closing quote at column 0)
                    int lineIndent = lineContentStart - _lineStart;
                    if (lineContentStart < _buffer.Length && 
                        !IsLineBreak(_buffer[lineContentStart]) &&
                        lineIndent == 0 && quoteStartColumn > 0)
                    {
                        // Content at column 0 when the quote started after column 0 - invalid
                        throw new YamlException("Multiline quoted scalar continuation line has wrong indentation", Position);
                    }
                }
                
                continue;
            }
            
            _consumed++;
        }
        
        if (_isFinalBlock)
        {
            throw new YamlException("Unterminated double-quoted string", Position);
        }
        
        return false; // Need more data
    }

    /// <summary>
    /// Checks if a character is a valid escape character in YAML double-quoted strings.
    /// Per YAML 1.2 spec section 5.7.
    /// </summary>
    private static bool IsValidEscapeChar(byte c)
    {
        return c switch
        {
            (byte)'0' => true,  // \0 null
            (byte)'a' => true,  // \a bell
            (byte)'b' => true,  // \b backspace
            (byte)'t' => true,  // \t tab
            (byte)'n' => true,  // \n newline
            (byte)'v' => true,  // \v vertical tab
            (byte)'f' => true,  // \f form feed
            (byte)'r' => true,  // \r carriage return
            (byte)'e' => true,  // \e escape
            (byte)' ' => true,  // \  space
            (byte)'"' => true,  // \" double quote
            (byte)'/' => true,  // \/ slash
            (byte)'\\' => true, // \\ backslash
            (byte)'N' => true,  // \N next line (U+0085)
            (byte)'_' => true,  // \_ non-breaking space (U+00A0)
            (byte)'L' => true,  // \L line separator (U+2028)
            (byte)'P' => true,  // \P paragraph separator (U+2029)
            (byte)'x' => true,  // \xNN 8-bit character
            (byte)'u' => true,  // \uNNNN 16-bit Unicode
            (byte)'U' => true,  // \UNNNNNNNN 32-bit Unicode
            LineFeed => true,   // escaped line break (line folding)
            CarriageReturn => true, // escaped line break
            _ => false
        };
    }

    private bool ParseLiteralBlockScalar()
    {
        // Get the current indentation level before consuming the indicator
        int currentIndent = GetCurrentIndentation();
        
        _consumed++; // Skip |
        
        var (chomping, explicitIndent) = ParseBlockScalarHeader();
        
        // Skip to end of header line
        SkipToEndOfLine();
        ConsumeLineBreak();
        
        // Determine content indentation
        // Per YAML spec, explicit indentation is added to the current indentation level
        int contentIndent = explicitIndent > 0 
            ? currentIndent + explicitIndent 
            : DetectBlockScalarIndentation();
        
        // Check for invalid indentation pattern (spaces-only lines with more indent than content)
        if (contentIndent < 0)
        {
            throw new YamlException("Block scalar has lines with more spaces than the first content line", Position);
        }
        
        // Store the content indentation for later processing in GetString()
        _blockScalarIndent = contentIndent;
        
        int valueStart = _consumed;
        
        // Collect all lines with proper indentation
        while (_consumed < _buffer.Length)
        {
            // Check for tabs at the start of the line (column 0) - this is always invalid
            if (_consumed == _lineStart && CurrentByte() == Tab)
            {
                throw new YamlException("Tabs cannot be used for indentation in block scalars", Position);
            }
            
            int lineIndent = CountLeadingSpaces();
            
            if (lineIndent < contentIndent && !IsEmptyLine())
            {
                break;
            }
            
            SkipToEndOfLine();
            if (_consumed < _buffer.Length && IsLineBreak(CurrentByte()))
            {
                ConsumeLineBreak();
            }
        }
        
        _valueSpan = _buffer.Slice(valueStart, _consumed - valueStart);
        _tokenType = YamlTokenType.Scalar;
        _scalarStyle = ScalarStyle.Literal;
        return true;
    }

    private bool ParseFoldedBlockScalar()
    {
        // Get the current indentation level before consuming the indicator
        int currentIndent = GetCurrentIndentation();
        
        _consumed++; // Skip >
        
        var (chomping, explicitIndent) = ParseBlockScalarHeader();
        
        // Skip to end of header line
        SkipToEndOfLine();
        ConsumeLineBreak();
        
        // Per YAML spec, explicit indentation is added to the current indentation level
        int contentIndent = explicitIndent > 0 
            ? currentIndent + explicitIndent 
            : DetectBlockScalarIndentation();
        
        // Check for invalid indentation pattern (spaces-only lines with more indent than content)
        if (contentIndent < 0)
        {
            throw new YamlException("Block scalar has lines with more spaces than the first content line", Position);
        }
        
        // Store the content indentation for later processing in GetString()
        _blockScalarIndent = contentIndent;
        
        int valueStart = _consumed;
        
        while (_consumed < _buffer.Length)
        {
            // Check for tabs at the start of the line (column 0) - this is always invalid
            if (_consumed == _lineStart && CurrentByte() == Tab)
            {
                throw new YamlException("Tabs cannot be used for indentation in block scalars", Position);
            }
            
            int lineIndent = CountLeadingSpaces();
            
            if (lineIndent < contentIndent && !IsEmptyLine())
            {
                break;
            }
            
            SkipToEndOfLine();
            if (_consumed < _buffer.Length && IsLineBreak(CurrentByte()))
            {
                ConsumeLineBreak();
            }
        }
        
        _valueSpan = _buffer.Slice(valueStart, _consumed - valueStart);
        _tokenType = YamlTokenType.Scalar;
        _scalarStyle = ScalarStyle.Folded;
        return true;
    }

    private (ChompingIndicator chomping, int explicitIndent) ParseBlockScalarHeader()
    {
        var chomping = ChompingIndicator.Clip;
        int explicitIndent = 0;
        bool hadWhitespace = false;
        
        while (_consumed < _buffer.Length && !IsLineBreak(CurrentByte()))
        {
            byte current = CurrentByte();
            
            if (current == (byte)'-')
            {
                chomping = ChompingIndicator.Strip;
                _consumed++;
                hadWhitespace = false;
            }
            else if (current == (byte)'+')
            {
                chomping = ChompingIndicator.Keep;
                _consumed++;
                hadWhitespace = false;
            }
            else if (current is >= (byte)'1' and <= (byte)'9')
            {
                explicitIndent = current - (byte)'0';
                _consumed++;
                hadWhitespace = false;
            }
            else if (current == Comment)
            {
                // Comment must be preceded by whitespace
                if (!hadWhitespace)
                {
                    throw new YamlException("Comment after block scalar indicator must be preceded by whitespace", Position);
                }
                SkipToEndOfLine();
                break;
            }
            else if (IsWhitespace(current))
            {
                hadWhitespace = true;
                _consumed++;
            }
            else
            {
                // Any other character is invalid in block scalar header
                throw new YamlException($"Invalid character '{(char)current}' after block scalar indicator", Position);
            }
        }
        
        return (chomping, explicitIndent);
    }

    private bool ParsePlainScalar()
    {
        int valueStart = _consumed;
        bool inFlow = _state == ParserState.InFlowContent;
        
        // Flow indicators cannot start a plain scalar in any context
        if (_consumed < _buffer.Length)
        {
            byte first = CurrentByte();
            if (first == CollectEntry || first == SequenceStart || first == SequenceEnd ||
                first == MappingStartChar || first == MappingEndChar)
            {
                throw new YamlException($"Plain scalar cannot start with flow indicator '{(char)first}'", Position);
            }
        }
        
        // In flow context, plain scalars cannot start with indicators like - ? : 
        // if they would be ambiguous (i.e., followed by a flow indicator or whitespace)
        if (inFlow && _consumed < _buffer.Length)
        {
            byte first = CurrentByte();
            if (first == SequenceEntry || first == MappingKey || first == MappingValue)
            {
                // Check what follows the indicator
                if (_consumed + 1 >= _buffer.Length)
                {
                    throw new YamlException($"Invalid plain scalar starting with '{(char)first}' in flow context", Position);
                }
                byte next = _buffer[_consumed + 1];
                if (IsFlowIndicator(next) || IsWhitespaceOrLineBreak(next))
                {
                    throw new YamlException($"Invalid plain scalar starting with '{(char)first}' in flow context", Position);
                }
            }
        }
        
        // Track if we've crossed a line break (for multiline scalar detection)
        bool crossedLineBreak = false;
        int lastLineBreakPos = -1;
        int lastLineBreakLine = -1;
        int lastLineBreakLineStart = -1;
        
        // For multiline plain scalars in block context, we need to track indent
        // Continuation lines must be more indented than the current block indent
        int blockIndent = _currentDepth > 0 ? GetIndentLevel(_currentDepth - 1) : -1;
        
        while (_consumed < _buffer.Length)
        {
            byte current = CurrentByte();
            
            // Check for line break - potential multiline continuation
            if (IsLineBreak(current))
            {
                int savedPosBlock = _consumed;
                int savedLineBlock = _line;
                int savedLineStartBlock = _lineStart;
                
                ConsumeLineBreak();
                
                // Skip empty lines
                while (_consumed < _buffer.Length && IsLineBreak(CurrentByte()))
                {
                    ConsumeLineBreak();
                }
                
                // Count leading spaces on continuation line
                int continuationIndent = 0;
                int indentStart = _consumed;
                while (_consumed < _buffer.Length && CurrentByte() == Space)
                {
                    continuationIndent++;
                    _consumed++;
                }
                
                if (_consumed >= _buffer.Length)
                {
                    // End of input - don't include trailing newline
                    _consumed = savedPosBlock;
                    _line = savedLineBlock;
                    _lineStart = savedLineStartBlock;
                    break;
                }
                
                byte nextByte = CurrentByte();
                
                // In flow context, check if next line continues the scalar or starts new content
                if (inFlow)
                {
                    // Flow indicators end the scalar
                    if (IsFlowIndicator(nextByte))
                    {
                        _consumed = savedPosBlock;
                        _line = savedLineBlock;
                        _lineStart = savedLineStartBlock;
                        break;
                    }
                    
                    // Comma ends the scalar
                    if (nextByte == CollectEntry)
                    {
                        _consumed = savedPosBlock;
                        _line = savedLineBlock;
                        _lineStart = savedLineStartBlock;
                        break;
                    }
                    
                    // Comment ends the scalar (comments can appear between flow entries)
                    if (nextByte == Comment)
                    {
                        _consumed = savedPosBlock;
                        _line = savedLineBlock;
                        _lineStart = savedLineStartBlock;
                        break;
                    }
                    
                    // : followed by whitespace or flow indicator ends the scalar (it's a mapping value)
                    // In flow SEQUENCE context, implicit keys must be on the same line as the ':'
                    // (because the ':' creates an implicit mapping entry)
                    // In flow MAPPING context, multiline keys are allowed - the ':' just separates key from value
                    if (nextByte == MappingValue &&
                        !IsCurrentCollectionMapping() &&
                        (_consumed + 1 >= _buffer.Length ||
                         IsWhitespace(_buffer[_consumed + 1]) ||
                         IsLineBreak(_buffer[_consumed + 1]) ||
                         IsFlowIndicator(_buffer[_consumed + 1])))
                    {
                        throw new YamlException("Implicit keys in flow context must be on a single line", Position);
                    }
                    
                    // In flow mapping context, check if continuation looks like a new entry
                    // (e.g., "bar:" where we'd expect a comma before the next entry)
                    // If we're in a mapping VALUE and this looks like a new key, end the current value
                    // Only do this when NOT expecting a key (i.e., we're parsing a value)
                    if (IsCurrentCollectionMapping() && !IsExpectingKey() && LooksLikeFlowMappingKey())
                    {
                        _consumed = savedPosBlock;
                        _line = savedLineBlock;
                        _lineStart = savedLineStartBlock;
                        break;
                    }
                    
                    // Mark that we crossed a line break and save position for potential rollback
                    crossedLineBreak = true;
                    lastLineBreakPos = savedPosBlock;
                    lastLineBreakLine = savedLineBlock;
                    lastLineBreakLineStart = savedLineStartBlock;
                    
                    // Otherwise this is a continuation of the multiline plain scalar
                    continue;
                }
                
                // Check for document markers at column 0 (block context)
                if (continuationIndent == 0)
                {
                    int tempPos = _consumed;
                    _consumed = indentStart; // Reset to check marker
                    if (IsAtDocumentMarker(DocumentStartMarker) || IsAtDocumentMarker(DocumentEndMarker))
                    {
                        _consumed = savedPosBlock;
                        _line = savedLineBlock;
                        _lineStart = savedLineStartBlock;
                        break;
                    }
                    _consumed = tempPos; // Restore
                }
                
                // Continuation must be more indented than the block context
                if (continuationIndent <= blockIndent)
                {
                    // Not a continuation - end the scalar
                    _consumed = savedPosBlock;
                    _line = savedLineBlock;
                    _lineStart = savedLineStartBlock;
                    break;
                }
                
                // Check for comment (ends scalar without error, but don't continue)
                if (nextByte == Comment)
                {
                    _consumed = savedPosBlock;
                    _line = savedLineBlock;
                    _lineStart = savedLineStartBlock;
                    break;
                }
                
                // Check for block indicators at proper indentation
                // - or ? or : followed by whitespace indicates structure, not continuation
                // BUT only if the indicator is at a valid indentation level for the parent collection
                // A sequence entry at wrong indentation should be absorbed into the scalar
                if ((nextByte == SequenceEntry || nextByte == MappingKey || nextByte == MappingValue) &&
                    IsIndicatorFollowedByWhitespace(1) && continuationIndent == blockIndent)
                {
                    _consumed = savedPosBlock;
                    _line = savedLineBlock;
                    _lineStart = savedLineStartBlock;
                    break;
                }
                
                // Check for implicit mapping key pattern on continuation line
                // If the line looks like "key: value" it's not a continuation, it's a new key
                // This prevents wrongly indented keys from being absorbed into the value
                if (IsCurrentCollectionMapping() && LooksLikeImplicitMappingKey())
                {
                    // This line is a mapping key - not a continuation
                    // It may be at wrong indentation (which will be caught later)
                    _consumed = savedPosBlock;
                    _line = savedLineBlock;
                    _lineStart = savedLineStartBlock;
                    break;
                }
                
                // This is a valid continuation line - keep going
                continue;
            }
            
            // In flow context, these characters end the scalar
            if (inFlow && IsFlowIndicator(current))
            {
                break;
            }
            
            // Check for : followed by whitespace (mapping value) OR at end of buffer
            // If : is at end of buffer, it's a null/empty value indicator (key:)
            if (current == MappingValue && 
                (_consumed + 1 >= _buffer.Length ||
                 IsWhitespace(_buffer[_consumed + 1]) || IsLineBreak(_buffer[_consumed + 1]) || 
                 (inFlow && IsFlowIndicator(_buffer[_consumed + 1]))))
            {
                // In flow SEQUENCE context, if we crossed a line break to get here,
                // this : is on a different line from the key. Roll back to before
                // the line break so the multiline key check in ParseFlowContent can fire.
                // In flow MAPPING context, multiline keys are valid, so don't roll back.
                if (inFlow && crossedLineBreak && !IsCurrentCollectionMapping())
                {
                    _consumed = lastLineBreakPos;
                    _line = lastLineBreakLine;
                    _lineStart = lastLineBreakLineStart;
                }
                break;
            }
            
            // Check for # preceded by whitespace (comment)
            if (current == Comment && _consumed > valueStart && IsWhitespace(_buffer[_consumed - 1]))
            {
                _consumed--; // Back up before the whitespace
                
                // After an inline comment ends a plain scalar, check if subsequent content
                // looks like an invalid continuation attempt.
                // Skip to end of line and check what follows.
                int commentPos = _consumed + 1; // Position of #
                while (commentPos < _buffer.Length && !IsLineBreak(_buffer[commentPos]))
                {
                    commentPos++;
                }
                
                // Skip the line break
                if (commentPos < _buffer.Length)
                {
                    if (_buffer[commentPos] == CarriageReturn)
                    {
                        commentPos++;
                        if (commentPos < _buffer.Length && _buffer[commentPos] == LineFeed)
                            commentPos++;
                    }
                    else if (_buffer[commentPos] == LineFeed)
                    {
                        commentPos++;
                    }
                }
                
                // Check the next line's indentation
                int nextLineIndent = 0;
                int nextLineStart = commentPos;
                while (commentPos < _buffer.Length && _buffer[commentPos] == Space)
                {
                    nextLineIndent++;
                    commentPos++;
                }
                
                // If next line is at continuation indent (> blockIndent) and has content,
                // it's an invalid attempt to continue the scalar after a comment
                if (commentPos < _buffer.Length && 
                    !IsLineBreak(_buffer[commentPos]) && 
                    _buffer[commentPos] != Comment &&
                    nextLineIndent > blockIndent)
                {
                    // Check if it looks like scalar content (not a structure indicator)
                    byte nextChar = _buffer[commentPos];
                    if (nextChar != SequenceEntry && nextChar != MappingKey && nextChar != MappingValue)
                    {
                        throw new YamlException("Invalid content after comment in multiline plain scalar", 
                            new Mark(_line + 1, nextLineIndent + 1, nextLineStart));
                    }
                }
                
                break;
            }
            
            _consumed++;
        }
        
        // Trim trailing whitespace (but not internal newlines for multiline)
        int valueEnd = _consumed;
        while (valueEnd > valueStart && IsWhitespace(_buffer[valueEnd - 1]))
        {
            valueEnd--;
        }
        
        _valueSpan = _buffer.Slice(valueStart, valueEnd - valueStart);
        _tokenType = YamlTokenType.Scalar;
        _scalarStyle = ScalarStyle.Plain;
        
        return true;
    }

    private bool HandleEndOfInput()
    {
        if (!_isFinalBlock)
        {
            return false;
        }

        // If we were expecting a document start (after directives), that's an error
        if (_expectDocumentStart)
        {
            throw new YamlException("Directive without document - expected '---' after directive", Position);
        }

        return _state switch
        {
            ParserState.InDocument or ParserState.InBlockContent => HandleEndOfBlockContent(),
            ParserState.InFlowContent => throw new YamlException("Unexpected end of input in flow content", Position),
            ParserState.InStream => EmitStreamEnd(),
            ParserState.Finished => false,
            _ => EmitStreamEnd()
        };
    }

    private bool HandleEndOfBlockContent()
    {
        // Close any open structures
        if (_currentDepth > 0)
        {
            bool isMapping = IsCurrentCollectionMapping();
            _currentDepth--;
            _tokenType = isMapping ? YamlTokenType.MappingEnd : YamlTokenType.SequenceEnd;
            return true;
        }
        
        // If we're at depth 0 and there's still content, check if it's valid
        // Content after the root collection has ended is invalid unless it's
        // a document marker or end of input
        SkipWhitespaceAndComments();
        if (_consumed < _buffer.Length)
        {
            byte current = CurrentByte();
            
            // Document markers are valid
            if (IsAtStartOfLine() && (IsAtDocumentMarker(DocumentStartMarker) || IsAtDocumentMarker(DocumentEndMarker)))
            {
                // Valid - emit document end and let the stream parser handle the markers
                _tokenType = YamlTokenType.DocumentEnd;
                _state = ParserState.InStream;
                return true;
            }
            
            // Directive at column 0 in document content is invalid (needs document end first)
            if (IsAtStartOfLine() && current == Directive && LooksLikeDirective())
            {
                throw new YamlException("Directive in document content requires preceding document end marker '...'", Position);
            }
            
            // Any other content at root level after the collection has ended is invalid
            if (_hasDocumentContent && !IsWhitespaceOrLineBreak(current))
            {
                int currentIndent = _consumed - _lineStart;
                if (currentIndent <= 0)
                {
                    throw new YamlException("Invalid content after root collection ends", Position);
                }
            }
        }
        
        // Emit document end
        _tokenType = YamlTokenType.DocumentEnd;
        _state = ParserState.InStream;
        return true;
    }

    private bool EmitStreamEnd()
    {
        _tokenType = YamlTokenType.StreamEnd;
        _state = ParserState.Finished;
        return true;
    }
}
