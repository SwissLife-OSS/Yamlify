using Yamlify.Exceptions;

namespace Yamlify.Core;

/// <summary>
/// Helper methods for character classification and navigation.
/// </summary>
public ref partial struct Utf8YamlReader
{
    private byte CurrentByte() => _buffer[_consumed];
    
    /// <summary>
    /// Push a collection type onto the stack (true = mapping, false = sequence).
    /// </summary>
    private void PushCollectionType(bool isMapping)
    {
        if (isMapping)
        {
            _collectionStack |= (1L << _currentDepth);
        }
        else
        {
            _collectionStack &= ~(1L << _currentDepth);
        }
    }
    
    /// <summary>
    /// Pop a collection type from the stack (just clears the bit at current depth).
    /// </summary>
    private void PopCollectionType()
    {
        // Clear the bit at the current depth to avoid stale data
        _collectionStack &= ~(1L << _currentDepth);
    }
    
    /// <summary>
    /// Check if the current collection is a mapping.
    /// </summary>
    private readonly bool IsCurrentCollectionMapping()
    {
        if (_currentDepth <= 0) return false;
        return (_collectionStack & (1L << (_currentDepth - 1))) != 0;
    }
    
    /// <summary>
    /// Finds the indentation level of the innermost enclosing block context.
    /// Returns -1 if there is no enclosing block context (i.e., all ancestors are flow).
    /// </summary>
    private readonly int GetEnclosingBlockIndent()
    {
        for (int d = _currentDepth - 1; d >= 0; d--)
        {
            if ((_flowStack & (1L << d)) == 0)
            {
                return GetIndentLevel(d);
            }
        }
        return -1;
    }
    
    /// <summary>
    /// Push whether the current collection is flow (true) or block (false).
    /// </summary>
    private void PushFlowContext(bool isFlow)
    {
        if (isFlow)
        {
            _flowStack |= (1L << _currentDepth);
        }
        else
        {
            _flowStack &= ~(1L << _currentDepth);
        }
    }
    
    /// <summary>
    /// Pop a flow context from the stack (just clears the bit at current depth).
    /// </summary>
    private void PopFlowContext()
    {
        // Clear the bit at the current depth to avoid stale data
        _flowStack &= ~(1L << _currentDepth);
    }
    
    /// <summary>
    /// Check if there's an enclosing flow context at depth-1.
    /// </summary>
    private readonly bool IsInsideFlowContext()
    {
        if (_currentDepth <= 0) return false;
        // Check if any level from 0 to current depth-1 is flow
        for (int i = 0; i < _currentDepth; i++)
        {
            if ((_flowStack & (1L << i)) != 0)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Sets the indentation level for a given depth.
    /// Each depth stores its indent as a 16-bit value packed into longs.
    /// </summary>
    private void SetIndentLevel(int depth, int indent)
    {
        if (depth < 0 || depth >= 16 || indent < 0) return;
        
        int slot = depth / 4;
        int shift = (depth % 4) * 16;
        long mask = ~(0xFFFFL << shift);
        long value = ((long)(indent & 0xFFFF)) << shift;
        
        switch (slot)
        {
            case 0: _indentLevels0 = (_indentLevels0 & mask) | value; break;
            case 1: _indentLevels1 = (_indentLevels1 & mask) | value; break;
            case 2: _indentLevels2 = (_indentLevels2 & mask) | value; break;
            case 3: _indentLevels3 = (_indentLevels3 & mask) | value; break;
        }
    }
    
    /// <summary>
    /// Gets the indentation level for a given depth.
    /// Returns -1 if no indent is set or depth is invalid.
    /// </summary>
    private readonly int GetIndentLevel(int depth)
    {
        if (depth < 0 || depth >= 16) return -1;
        
        int slot = depth / 4;
        int shift = (depth % 4) * 16;
        
        long packed = slot switch
        {
            0 => _indentLevels0,
            1 => _indentLevels1,
            2 => _indentLevels2,
            3 => _indentLevels3,
            _ => 0
        };
        
        return (int)((packed >> shift) & 0xFFFF);
    }
    
    /// <summary>
    /// Gets the expected indentation for the current collection (at depth - 1).
    /// For items in a collection, they should be at least at this indentation + 1.
    /// </summary>
    private readonly int GetCurrentCollectionIndent()
    {
        if (_currentDepth <= 0) return -1;
        return GetIndentLevel(_currentDepth - 1);
    }

    /// <summary>
    /// Sets whether we are expecting a key at the current depth.
    /// </summary>
    private void SetExpectingKey(bool expectingKey)
    {
        if (expectingKey)
        {
            _expectingKeyStack |= (1L << _currentDepth);
        }
        else
        {
            _expectingKeyStack &= ~(1L << _currentDepth);
        }
    }

    /// <summary>
    /// Check if we are expecting a key at the current depth.
    /// </summary>
    private readonly bool IsExpectingKey()
    {
        if (_currentDepth < 0) return false;
        return (_expectingKeyStack & (1L << _currentDepth)) != 0;
    }

    private void SkipByteOrderMark()
    {
        // UTF-8 BOM: EF BB BF
        if (_buffer.Length >= 3 && 
            _buffer[0] == 0xEF && 
            _buffer[1] == 0xBB && 
            _buffer[2] == 0xBF)
        {
            _consumed += 3;
        }
        // UTF-16 LE BOM: FF FE
        else if (_buffer.Length >= 2 && _buffer[0] == 0xFF && _buffer[1] == 0xFE)
        {
            _consumed += 2;
        }
        // UTF-16 BE BOM: FE FF
        else if (_buffer.Length >= 2 && _buffer[0] == 0xFE && _buffer[1] == 0xFF)
        {
            _consumed += 2;
        }
    }

    private void SkipWhitespaceAndComments()
    {
        // Comment is valid at start of line OR if preceded by whitespace
        bool hadWhitespace = IsAtStartOfLine() || 
            (_consumed > 0 && IsWhitespace(_buffer[_consumed - 1]));
        
        while (_consumed < _buffer.Length)
        {
            byte current = CurrentByte();

            if (current == Space || current == Tab)
            {
                hadWhitespace = true;
                _consumed++;
            }
            else if (IsLineBreak(current))
            {
                hadWhitespace = true; // Comment valid after line break
                ConsumeLineBreak();
            }
            else if (current == Comment)
            {
                // Comment must be preceded by whitespace or be at start of line
                if (!hadWhitespace)
                {
                    // This is not a comment - it's invalid content
                    // Let the caller handle it
                    break;
                }
                
                if (_options.ReadComments)
                {
                    return; // Let the caller handle the comment token
                }
                SkipToEndOfLine();
                hadWhitespace = false;
            }
            else
            {
                break;
            }
        }
    }

    private void SkipSpaces()
    {
        while (_consumed < _buffer.Length && (CurrentByte() == Space || CurrentByte() == Tab))
        {
            _consumed++;
        }
    }
    
    /// <summary>
    /// Skips whitespace and comments in flow context, validating that comments have proper spacing.
    /// Returns true if a comment character is encountered without proper preceding space.
    /// </summary>
    private void SkipFlowWhitespaceAndComments()
    {
        bool hadWhitespace = false;
        bool atStartOfLine = false; // True when we just crossed a line break
        // DON'T reset - preserve the flag if it was set from previous call
        // _crossedLineBreakInFlow = false;
        
        while (_consumed < _buffer.Length)
        {
            byte current = CurrentByte();

            if (current == Tab)
            {
                hadWhitespace = true;
                // Tabs are only valid in flow context if they're followed by:
                // - more whitespace/line break (empty line or tab-only indentation)
                // - flow indicators (], }, ,, :)
                // Tabs before CONTENT (scalars, [, {, etc.) are invalid
                if (atStartOfLine)
                {
                    // Check if this tab is followed by more whitespace/line break, flow end, or content
                    int peekPos = _consumed + 1;
                    while (peekPos < _buffer.Length && _buffer[peekPos] == Tab)
                    {
                        peekPos++;
                    }
                    // If there's non-whitespace content after the tab(s), check if it's a flow indicator
                    if (peekPos < _buffer.Length && !IsWhitespaceOrLineBreak(_buffer[peekPos]))
                    {
                        byte afterTabs = _buffer[peekPos];
                        // Flow end indicators and comma are OK after tabs
                        if (afterTabs != SequenceEnd && afterTabs != MappingEndChar && 
                            afterTabs != CollectEntry && afterTabs != MappingValue)
                        {
                            throw new YamlException("Tabs cannot be used at the start of a line in flow context before content", Position);
                        }
                    }
                }
                _consumed++;
            }
            else if (current == Space)
            {
                hadWhitespace = true;
                atStartOfLine = false; // Spaces clear the "at start of line" state
                _consumed++;
            }
            else if (IsLineBreak(current))
            {
                hadWhitespace = true;
                _crossedLineBreakInFlow = true; // Track that we crossed a line break
                atStartOfLine = true; // We're now at the start of a new line
                ConsumeLineBreak();
            }
            else if (current == Comment)
            {
                // In flow context, # must be preceded by whitespace to be a comment
                if (!hadWhitespace)
                {
                    // This is not a comment, it's invalid - let the caller handle it
                    return;
                }
                SkipToEndOfLine();
                hadWhitespace = false;
                atStartOfLine = false;
            }
            else
            {
                break;
            }
        }
    }

    private void SkipToEndOfLine()
    {
        while (_consumed < _buffer.Length && !IsLineBreak(CurrentByte()))
        {
            _consumed++;
        }
    }
    
    /// <summary>
    /// Checks if the current position is the first non-whitespace content on this line.
    /// This is true if everything between _lineStart and _consumed is whitespace.
    /// </summary>
    private readonly bool IsFirstNonWhitespaceOnLine()
    {
        for (int i = _lineStart; i < _consumed; i++)
        {
            byte b = _buffer[i];
            if (b != Space && b != Tab)
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Gets the indentation (column) of the first non-whitespace character on the current line.
    /// This is used for flow indentation checking - content on continuation lines must be
    /// indented more than the line where the flow context started.
    /// </summary>
    private readonly int GetLineIndent()
    {
        for (int i = _lineStart; i < _buffer.Length; i++)
        {
            byte b = _buffer[i];
            if (b != Space && b != Tab)
            {
                return i - _lineStart;
            }
            if (IsLineBreak(b))
            {
                break;
            }
        }
        return 0;
    }

    private void ConsumeLineBreak()
    {
        if (_consumed >= _buffer.Length)
            return;

        byte current = CurrentByte();
        
        if (current == CarriageReturn)
        {
            _consumed++;
            if (_consumed < _buffer.Length && CurrentByte() == LineFeed)
            {
                _consumed++;
            }
        }
        else if (current == LineFeed)
        {
            _consumed++;
        }
        
        _line++;
        _lineStart = _consumed;
    }

    private static bool IsLineBreak(byte b) => b == LineFeed || b == CarriageReturn;

    private static bool IsWhitespace(byte b) => b == Space || b == Tab;

    private static bool IsWhitespaceOrLineBreak(byte b) => 
        b == Space || b == Tab || b == LineFeed || b == CarriageReturn;
    
    private static bool IsDigit(byte b) => b >= (byte)'0' && b <= (byte)'9';

    private bool IsAtStartOfLine() => _consumed == _lineStart;

    /// <summary>
    /// Checks if the line starts with tab characters or has tabs before the indentation spaces.
    /// YAML spec forbids tabs for indentation - only spaces are allowed.
    /// Tabs after the leading spaces (as separation whitespace) are allowed.
    /// </summary>
    private bool HasTabsInIndentation()
    {
        // Check if line starts with a tab (before any spaces)
        if (_lineStart < _buffer.Length && _buffer[_lineStart] == Tab)
        {
            return true;
        }
        
        // Check for tabs before content while still in the indentation zone
        // Tabs are only invalid if they appear at positions that would affect indentation
        int pos = _lineStart;
        
        while (pos < _consumed && pos < _buffer.Length)
        {
            byte b = _buffer[pos];
            if (b == Space)
            {
                pos++;
            }
            else if (b == Tab)
            {
                // Tab at position 0 is always invalid (checked above)
                // Tab after spaces could be:
                // - Part of indentation (invalid) if we're still counting indent
                // - Separation whitespace (valid) if after indentation
                // For block content, if we see space-tab-space or similar patterns,
                // the tab is still in the indentation zone and invalid.
                // But if it's space(s)-tab followed by content, it's separation space.
                // We can't easily distinguish here, so let's be conservative:
                // Only flag tabs that appear between spaces (like "  \t  x")
                int nextPos = pos + 1;
                if (nextPos < _consumed && nextPos < _buffer.Length && _buffer[nextPos] == Space)
                {
                    // Tab followed by space in indentation zone - invalid
                    return true;
                }
                pos++;
            }
            else
            {
                break;
            }
        }
        return false;
    }

    private int GetCurrentIndentation()
    {
        int indent = 0;
        int pos = _lineStart;
        
        while (pos < _consumed && pos < _buffer.Length)
        {
            if (_buffer[pos] == Space)
            {
                indent++;
                pos++;
            }
            else if (_buffer[pos] == Tab)
            {
                // Tabs should not be used for indentation per YAML spec,
                // but we handle them for robustness
                indent += 8 - (indent % 8);
                pos++;
            }
            else
            {
                break;
            }
        }
        
        return indent;
    }

    private int CountLeadingSpaces()
    {
        int count = 0;
        int pos = _consumed;
        
        while (pos < _buffer.Length && _buffer[pos] == Space)
        {
            count++;
            pos++;
        }
        
        return count;
    }

    private bool IsEmptyLine()
    {
        int pos = _consumed;
        
        while (pos < _buffer.Length)
        {
            byte b = _buffer[pos];
            if (b == Space || b == Tab)
            {
                pos++;
            }
            else if (IsLineBreak(b))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        return true; // End of input counts as empty
    }

    private int DetectBlockScalarIndentation()
    {
        int pos = _consumed;
        int maxSpacesOnlyIndent = 0; // Track max indentation seen on spaces-only lines
        
        // Find first non-empty line
        while (pos < _buffer.Length)
        {
            int lineStart = pos;
            
            // Count leading spaces on this line
            int spaces = 0;
            while (pos < _buffer.Length && _buffer[pos] == Space)
            {
                spaces++;
                pos++;
            }
            
            // Check what comes after the spaces
            if (pos < _buffer.Length && !IsLineBreak(_buffer[pos]))
            {
                // Found non-linebreak content - this line determines the indentation.
                // Tabs after spaces are CONTENT, not indentation, so this is a content line.
                // Check if previous spaces-only lines had MORE spaces - that's invalid.
                if (maxSpacesOnlyIndent > 0 && spaces < maxSpacesOnlyIndent)
                {
                    // Content at lower indentation than preceding spaces-only lines
                    // Return -1 to signal an error
                    return -1;
                }
                return spaces;
            }
            
            // This is an empty line (spaces followed immediately by line break)
            // Track the maximum spaces seen on empty lines
            if (spaces > maxSpacesOnlyIndent)
            {
                maxSpacesOnlyIndent = spaces;
            }
            
            // Skip the line break
            if (pos < _buffer.Length && IsLineBreak(_buffer[pos]))
            {
                pos++;
            }
        }
        
        return 0;
    }

    private bool IsIndicatorFollowedByWhitespace(int offset)
    {
        int pos = _consumed + offset;
        return pos >= _buffer.Length || IsWhitespaceOrLineBreak(_buffer[pos]);
    }
    
    /// <summary>
    /// Check if any byte in the range [start, end) is a tab character.
    /// </summary>
    private bool HasTabBetween(int start, int end)
    {
        for (int i = start; i < end && i < _buffer.Length; i++)
        {
            if (_buffer[i] == Tab)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Check if the current byte is a block indicator (-, ?, :) followed by whitespace.
    /// Used to detect when tabs are incorrectly used before block indicators.
    /// </summary>
    private bool IsBlockIndicatorFollowedByWhitespace(byte current)
    {
        return (current == SequenceEntry || current == MappingKey || current == MappingValue) 
            && IsIndicatorFollowedByWhitespace(1);
    }

    private bool IsAtDocumentMarker(ReadOnlySpan<byte> marker)
    {
        if (_consumed + 3 > _buffer.Length)
            return false;

        if (!_buffer.Slice(_consumed, 3).SequenceEqual(marker))
            return false;

        // Must be followed by whitespace or end of input
        return _consumed + 3 >= _buffer.Length || 
               IsWhitespaceOrLineBreak(_buffer[_consumed + 3]);
    }

    private bool MatchesBytes(ReadOnlySpan<byte> expected)
    {
        if (_consumed + expected.Length > _buffer.Length)
            return false;

        return _buffer.Slice(_consumed, expected.Length).SequenceEqual(expected);
    }

    /// <summary>
    /// Checks if the current position matches a directive name followed by whitespace or end of line.
    /// This ensures that %YAML is not confused with %YAMLL (a reserved directive).
    /// </summary>
    private bool MatchesDirective(ReadOnlySpan<byte> directiveName)
    {
        if (!MatchesBytes(directiveName))
        {
            return false;
        }
        
        // After the directive name, there must be whitespace or end of buffer
        int afterDirective = _consumed + directiveName.Length;
        if (afterDirective >= _buffer.Length)
        {
            return true; // End of input after directive name is OK
        }
        
        byte next = _buffer[afterDirective];
        return IsWhitespaceOrLineBreak(next);
    }

    private static bool IsFlowIndicator(byte b) => 
        b == SequenceStart || b == SequenceEnd || 
        b == MappingStartChar || b == MappingEndChar || 
        b == CollectEntry;

    private static bool IsAnchorChar(byte b)
    {
        // Anchor names can contain any non-whitespace, non-flow-indicator character
        if (IsWhitespaceOrLineBreak(b)) return false;
        if (IsFlowIndicator(b)) return false;
        return true;
    }

    private static bool IsTagChar(byte b)
    {
        // Tag characters per YAML spec
        if (IsWhitespaceOrLineBreak(b)) return false;
        if (IsFlowIndicator(b)) return false;
        return true;
    }

    /// <summary>
    /// Validates content after a flow collection ends at the top level (back to block context).
    /// Checks that any remaining content on the same line is valid (e.g., proper comments with whitespace).
    /// </summary>
    private void ValidateAfterFlowCollectionEnd()
    {
        if (_consumed >= _buffer.Length)
        {
            return;
        }
        
        byte current = CurrentByte();
        
        // Line break is fine - nothing on rest of line
        if (IsLineBreak(current))
        {
            return;
        }
        
        // ':' directly after flow collection is valid - this allows flow collections as implicit keys
        // e.g., "[flow]: block" where the sequence is the mapping key
        if (current == MappingValue && _consumed + 1 < _buffer.Length && 
            (IsWhitespace(_buffer[_consumed + 1]) || IsLineBreak(_buffer[_consumed + 1])))
        {
            return;
        }
        
        // If it's a # without preceding whitespace, that's invalid
        if (current == Comment)
        {
            throw new YamlException("Invalid comment without preceding whitespace after flow collection", Position);
        }
        
        // If it's whitespace, skip it and check what follows on the same line
        bool hadWhitespace = false;
        while (_consumed < _buffer.Length)
        {
            current = CurrentByte();
            
            if (current == Space || current == Tab)
            {
                hadWhitespace = true;
                _consumed++;
            }
            else if (IsLineBreak(current))
            {
                // End of line - all good
                return;
            }
            else if (current == Comment)
            {
                // Comment after whitespace is valid
                if (hadWhitespace)
                {
                    return; // Will be skipped by SkipWhitespaceAndComments later
                }
                throw new YamlException("Invalid comment without preceding whitespace after flow collection", Position);
            }
            else if (current == MappingValue)
            {
                // ':' with or without preceding whitespace is valid - for implicit keys
                return;
            }
            else
            {
                // Non-whitespace content after flow collection on same line is invalid
                throw new YamlException($"Unexpected content after flow collection: '{(char)current}'", Position);
            }
        }
    }

    /// <summary>
    /// Validates content after a quoted scalar in block context.
    /// After a quoted string in block context, only whitespace, comments (with preceding space),
    /// newline, or ':' (for mapping keys) are valid.
    /// </summary>
    private void ValidateAfterQuotedScalar(bool spannedLines = false)
    {
        if (_consumed >= _buffer.Length)
        {
            return;
        }
        
        byte current = CurrentByte();
        
        // Line break is fine - nothing on rest of line
        if (IsLineBreak(current))
        {
            return;
        }
        
        // If it's a # without preceding whitespace, that's invalid
        if (current == Comment)
        {
            throw new YamlException("Invalid comment without preceding whitespace after quoted scalar", Position);
        }
        
        // ':' is valid if the quoted string is a mapping key
        // The ':' must be followed by whitespace or end of line
        // BUT: if the quoted scalar spanned multiple lines, it cannot be an implicit key
        if (current == MappingValue)
        {
            // Check if : is followed by whitespace or end of line/buffer
            if (_consumed + 1 >= _buffer.Length || 
                IsWhitespaceOrLineBreak(_buffer[_consumed + 1]))
            {
                // If the scalar spanned lines, this is an invalid implicit key
                if (spannedLines)
                {
                    throw new YamlException("Implicit keys must be on a single line", Position);
                }
                return; // Valid mapping key
            }
            // Otherwise, ':' without following whitespace is invalid
            // (unless part of the value content, which we'd catch elsewhere)
        }
        
        // If it's whitespace, skip it and check what follows on the same line
        bool hadWhitespace = false;
        while (_consumed < _buffer.Length)
        {
            current = CurrentByte();
            
            if (current == Space || current == Tab)
            {
                hadWhitespace = true;
                _consumed++;
            }
            else if (IsLineBreak(current))
            {
                // End of line - all good
                return;
            }
            else if (current == Comment)
            {
                // Comment after whitespace is valid
                if (hadWhitespace)
                {
                    return; // Will be skipped by SkipWhitespaceAndComments later
                }
                throw new YamlException("Invalid comment without preceding whitespace after quoted scalar", Position);
            }
            else if (current == MappingValue)
            {
                // ':' after whitespace is valid if followed by whitespace (mapping key)
                if (_consumed + 1 >= _buffer.Length || 
                    IsWhitespaceOrLineBreak(_buffer[_consumed + 1]))
                {
                    // If the scalar spanned lines, this is an invalid implicit key
                    if (spannedLines)
                    {
                        throw new YamlException("Implicit keys must be on a single line", Position);
                    }
                    return; // Valid mapping key
                }
                throw new YamlException($"Trailing content after quoted scalar: '{(char)current}'", Position);
            }
            else
            {
                // Non-whitespace content after quoted scalar on same line is invalid
                throw new YamlException($"Trailing content after quoted scalar: '{(char)current}'", Position);
            }
        }
    }

    /// <summary>
    /// Validates content after a document marker (--- or ...).
    /// Only whitespace, comments (with preceding whitespace), or newlines are allowed.
    /// </summary>
    private void ValidateAfterDocumentMarker()
    {
        if (_consumed >= _buffer.Length)
        {
            return;
        }
        
        byte current = CurrentByte();
        
        // Line break is fine
        if (IsLineBreak(current))
        {
            return;
        }
        
        // Must have whitespace before anything else
        if (current != Space && current != Tab)
        {
            throw new YamlException("Invalid content after document marker", Position);
        }
        
        // Skip whitespace and check what follows
        while (_consumed < _buffer.Length)
        {
            current = CurrentByte();
            
            if (current == Space || current == Tab)
            {
                _consumed++;
            }
            else if (IsLineBreak(current))
            {
                return; // All good
            }
            else if (current == Comment)
            {
                return; // Comment is valid after whitespace
            }
            else
            {
                throw new YamlException("Invalid content after document marker", Position);
            }
        }
    }

    private readonly bool IsNullValue(ReadOnlySpan<byte> value)
    {
        return value.SequenceEqual(NullLower) ||
               value.SequenceEqual(NullTitle) ||
               value.SequenceEqual(NullUpper) ||
               value.SequenceEqual(NullTilde);
    }

    private readonly bool TryMatchBoolean(out bool value)
    {
        if (_valueSpan.SequenceEqual(TrueLower) ||
            _valueSpan.SequenceEqual(TrueTitle) ||
            _valueSpan.SequenceEqual(TrueUpper))
        {
            value = true;
            return true;
        }

        if (_valueSpan.SequenceEqual(FalseLower) ||
            _valueSpan.SequenceEqual(FalseTitle) ||
            _valueSpan.SequenceEqual(FalseUpper))
        {
            value = false;
            return true;
        }

        value = default;
        return false;
    }

    private readonly bool TryMatchSpecialFloat(out double value)
    {
        // Positive infinity
        if (_valueSpan.SequenceEqual(InfLower) ||
            _valueSpan.SequenceEqual(InfTitle) ||
            _valueSpan.SequenceEqual(InfUpper) ||
            _valueSpan.SequenceEqual(PosInfLower) ||
            _valueSpan.SequenceEqual(PosInfTitle) ||
            _valueSpan.SequenceEqual(PosInfUpper))
        {
            value = double.PositiveInfinity;
            return true;
        }

        // Negative infinity
        if (_valueSpan.SequenceEqual(NegInfLower) ||
            _valueSpan.SequenceEqual(NegInfTitle) ||
            _valueSpan.SequenceEqual(NegInfUpper))
        {
            value = double.NegativeInfinity;
            return true;
        }

        // NaN
        if (_valueSpan.SequenceEqual(NanLower) ||
            _valueSpan.SequenceEqual(NanTitle) ||
            _valueSpan.SequenceEqual(NanUpper))
        {
            value = double.NaN;
            return true;
        }

        value = default;
        return false;
    }
    
    /// <summary>
    /// Checks if the current position looks like a flow mapping key (text followed by : with whitespace).
    /// Used in flow context to detect when a new entry starts on a continuation line.
    /// </summary>
    private bool LooksLikeFlowMappingKey()
    {
        int pos = _consumed;
        
        // Skip non-indicator characters (the potential key)
        while (pos < _buffer.Length)
        {
            byte b = _buffer[pos];
            
            // If we hit a flow indicator, end of line, or whitespace before seeing ':', 
            // this is not a simple key pattern
            if (IsFlowIndicator(b) || IsLineBreak(b))
            {
                return false;
            }
            
            // If we hit ':', check if it's followed by whitespace/flow-indicator/end
            if (b == MappingValue)
            {
                if (pos + 1 >= _buffer.Length)
                {
                    return true; // ':' at end of buffer = valid key
                }
                byte next = _buffer[pos + 1];
                return IsWhitespace(next) || IsLineBreak(next) || IsFlowIndicator(next);
            }
            
            // Whitespace before ':' is OK (part of the key)
            pos++;
        }
        
        return false;
    }
    
    /// <summary>
    /// Checks if the current position looks like the start of an implicit mapping (key: value pattern).
    /// </summary>
    private bool LooksLikeImplicitMappingKey()
    {
        int pos = _consumed;
        byte current = _buffer[pos];
        
        // Skip over anchor (&name) or tag (!tag) at start of potential key
        // The anchor/tag applies to the key, not the mapping
        while (current == AnchorChar || current == TagChar)
        {
            pos++;
            // Skip anchor/tag name
            if (current == AnchorChar)
            {
                while (pos < _buffer.Length && IsAnchorChar(_buffer[pos]))
                {
                    pos++;
                }
            }
            else // TagChar
            {
                // Skip tag - handle !! and !<...> and !name
                if (pos < _buffer.Length && _buffer[pos] == TagChar)
                {
                    pos++; // Skip second !
                }
                else if (pos < _buffer.Length && _buffer[pos] == (byte)'<')
                {
                    pos++;
                    while (pos < _buffer.Length && _buffer[pos] != (byte)'>')
                    {
                        pos++;
                    }
                    if (pos < _buffer.Length) pos++; // Skip >
                }
                // Skip tag name
                while (pos < _buffer.Length && !IsWhitespaceOrLineBreak(_buffer[pos]) && !IsFlowIndicator(_buffer[pos]))
                {
                    pos++;
                }
            }
            // Skip whitespace after anchor/tag
            while (pos < _buffer.Length && (_buffer[pos] == Space || _buffer[pos] == Tab))
            {
                pos++;
            }
            if (pos >= _buffer.Length) return false;
            current = _buffer[pos];
        }
        
        // Skip flow collections (they can be mapping keys)
        if (current == SequenceStart || current == MappingStartChar)
        {
            byte open = current;
            byte close = (current == SequenceStart) ? SequenceEnd : MappingEndChar;
            int depth = 1;
            pos++;
            bool crossedLineBreak = false;
            
            while (pos < _buffer.Length && depth > 0)
            {
                byte b = _buffer[pos];
                
                // Track line breaks - implicit keys must be on a single line
                if (IsLineBreak(b))
                {
                    crossedLineBreak = true;
                }
                
                // Handle nested collections
                if (b == SequenceStart || b == MappingStartChar)
                {
                    depth++;
                }
                else if (b == SequenceEnd || b == MappingEndChar)
                {
                    depth--;
                }
                // Skip quoted strings inside flow collections
                else if (b == SingleQuote || b == DoubleQuote)
                {
                    byte quote = b;
                    pos++;
                    while (pos < _buffer.Length && _buffer[pos] != quote)
                    {
                        if (IsLineBreak(_buffer[pos]))
                        {
                            crossedLineBreak = true;
                        }
                        if (_buffer[pos] == (byte)'\\' && quote == DoubleQuote)
                        {
                            pos++; // Skip escape
                        }
                        pos++;
                    }
                }
                pos++;
            }
            
            // YAML spec: Implicit keys must be on a single line
            // If the flow collection spans multiple lines, it cannot be an implicit key
            if (crossedLineBreak)
            {
                return false;
            }
            
            // After the flow collection, skip whitespace and check for :
            while (pos < _buffer.Length && (_buffer[pos] == Space || _buffer[pos] == Tab))
            {
                pos++;
            }
            
            if (pos < _buffer.Length && _buffer[pos] == MappingValue)
            {
                if (pos + 1 >= _buffer.Length || 
                    IsWhitespaceOrLineBreak(_buffer[pos + 1]) ||
                    IsFlowIndicator(_buffer[pos + 1]))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        // Skip quoted strings first
        if (current == SingleQuote || current == DoubleQuote)
        {
            byte quote = current;
            pos++;
            while (pos < _buffer.Length && _buffer[pos] != quote)
            {
                if (_buffer[pos] == (byte)'\\' && current == DoubleQuote)
                {
                    pos++; // Skip escape
                }
                pos++;
            }
            if (pos < _buffer.Length)
            {
                pos++; // Skip closing quote
            }
        }
        else
        {
            // Scan plain scalar until we find : or end of line
            while (pos < _buffer.Length)
            {
                byte b = _buffer[pos];
                
                if (IsLineBreak(b))
                {
                    return false; // No : found before end of line
                }
                
                // Check for : followed by whitespace or end of content
                if (b == MappingValue)
                {
                    // In block context, ':' must be followed by whitespace to be a key indicator
                    // Flow indicators (like ,) after ':' only matter in flow context
                    if (pos + 1 >= _buffer.Length || 
                        IsWhitespaceOrLineBreak(_buffer[pos + 1]) ||
                        (IsInsideFlowContext() && IsFlowIndicator(_buffer[pos + 1])))
                    {
                        return true;
                    }
                }
                
                // Flow indicators only terminate plain scalars in flow context
                // In block context, flow indicators like [{]} are allowed in plain scalars
                if (IsInsideFlowContext() && IsFlowIndicator(b))
                {
                    return false;
                }
                
                pos++;
            }
        }
        
        // After quoted key, check for :
        while (pos < _buffer.Length && (_buffer[pos] == Space || _buffer[pos] == Tab))
        {
            pos++;
        }
        
        if (pos < _buffer.Length && _buffer[pos] == MappingValue)
        {
            if (pos + 1 >= _buffer.Length || 
                IsWhitespaceOrLineBreak(_buffer[pos + 1]) ||
                IsFlowIndicator(_buffer[pos + 1]))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Checks if the current position looks like a YAML directive (%YAML or %TAG).
    /// This is used to detect invalid directive placement in document content.
    /// </summary>
    private bool LooksLikeDirective()
    {
        // We're at '%', check if followed by YAML or TAG
        int pos = _consumed + 1;
        
        // Check for %YAML
        if (pos + 4 <= _buffer.Length)
        {
            if (_buffer[pos] == (byte)'Y' && 
                _buffer[pos + 1] == (byte)'A' && 
                _buffer[pos + 2] == (byte)'M' && 
                _buffer[pos + 3] == (byte)'L')
            {
                // Make sure it's followed by whitespace
                if (pos + 4 >= _buffer.Length || IsWhitespace(_buffer[pos + 4]))
                {
                    return true;
                }
            }
        }
        
        // Check for %TAG
        if (pos + 3 <= _buffer.Length)
        {
            if (_buffer[pos] == (byte)'T' && 
                _buffer[pos + 1] == (byte)'A' && 
                _buffer[pos + 2] == (byte)'G')
            {
                // Make sure it's followed by whitespace
                if (pos + 3 >= _buffer.Length || IsWhitespace(_buffer[pos + 3]))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    // ========================================================================
    // Token Buffer and Simple Key Infrastructure (Option C Architecture)
    // ========================================================================
    
    /// <summary>
    /// Consumes a token from the buffer and updates reader state.
    /// </summary>
    private bool ConsumeBufferedToken()
    {
        var token = _tokenBuffer.Consume();
        
        // Update reader state from the buffered token
        _tokenType = token.Type;
        _scalarStyle = token.Style;
        _tokenStart = token.GetMark();
        
        // Set value span if the token has content
        if (token.ValueLength > 0)
        {
            _valueSpan = _buffer.Slice(token.ValueStart, token.ValueLength);
            _hasValueSequence = true;
        }
        else
        {
            _valueSpan = default;
            _hasValueSequence = false;
        }
        
        // Update depth for collection tokens
        switch (token.Type)
        {
            case YamlTokenType.MappingStart:
            case YamlTokenType.SequenceStart:
                _currentDepth++;
                break;
            case YamlTokenType.MappingEnd:
            case YamlTokenType.SequenceEnd:
                _currentDepth--;
                break;
        }
        
        return true;
    }
    
    /// <summary>
    /// Enqueues a token into the lookahead buffer.
    /// </summary>
    private void EnqueueToken(YamlTokenType type, int valueStart = 0, int valueLength = 0, 
        ScalarStyle style = ScalarStyle.Any)
    {
        var token = RawToken.Create(
            type,
            valueStart,
            valueLength,
            style,
            _line,
            _consumed - _lineStart + 1,
            _totalConsumed + _consumed
        );
        _tokenBuffer.Enqueue(token);
    }
    
    /// <summary>
    /// Saves a potential simple key at the current position.
    /// Called when we encounter content that could be an implicit mapping key.
    /// </summary>
    private void SaveSimpleKey()
    {
        // A simple key is allowed at the beginning of each line in block context,
        // and after [ { , in flow context.
        bool allowed = _flowLevel > 0 || IsFirstNonWhitespaceOnLine();
        
        if (!allowed)
        {
            return;
        }
        
        // Remove any stale simple key
        RemoveStaleSimpleKeys();
        
        // Save this position as a potential simple key
        var key = SimpleKeyInfo.Create(
            possible: true,
            required: false,
            tokenIndex: _tokenBuffer.TotalEmitted + _tokenBuffer.Count,
            flowLevel: _flowLevel,
            line: _line,
            column: _consumed - _lineStart + 1,
            offset: _totalConsumed + _consumed
        );
        
        if (_simpleKeyStack.Count > _flowLevel)
        {
            // Replace the existing simple key at this flow level
            _simpleKeyStack.Top() = key;
        }
        else
        {
            // Push a new simple key context
            while (_simpleKeyStack.Count <= _flowLevel)
            {
                _simpleKeyStack.Push(default);
            }
            _simpleKeyStack.Top() = key;
        }
    }
    
    /// <summary>
    /// Removes stale simple keys that can no longer be valid.
    /// In block context, a simple key cannot span multiple lines.
    /// In any context, a simple key cannot exceed 1024 characters.
    /// </summary>
    private void RemoveStaleSimpleKeys()
    {
        if (_simpleKeyStack.IsEmpty)
        {
            return;
        }
        
        ref var key = ref _simpleKeyStack.Top();
        if (key.IsStale(_line, _totalConsumed + _consumed, _flowLevel == 0))
        {
            if (key.Required)
            {
                throw new YamlException("Could not find expected ':'", Position);
            }
            key.Invalidate();
        }
    }
    
    /// <summary>
    /// Handles the ':' character by checking if there's a pending simple key.
    /// If so, inserts a KEY token at the saved position.
    /// </summary>
    private bool HandlePossibleSimpleKey()
    {
        if (_simpleKeyStack.IsEmpty)
        {
            return false;
        }
        
        ref var key = ref _simpleKeyStack.Top();
        if (!key.Possible)
        {
            return false;
        }
        
        // Insert KEY token at the saved position
        var keyToken = RawToken.Create(
            YamlTokenType.Key,
            line: key.Line,
            column: key.Column,
            offset: key.Offset
        );
        
        _tokenBuffer.InsertAt(key.TokenIndex, keyToken);
        
        // Mark the simple key as consumed
        key.Invalidate();
        
        return true;
    }
    
    /// <summary>
    /// Increases the flow level when entering a flow collection.
    /// </summary>
    private void IncreaseFlowLevel()
    {
        _flowLevel++;
        
        // Push a simple key context for this flow level
        _simpleKeyStack.Push(SimpleKeyInfo.Create(
            possible: true,
            required: false,
            tokenIndex: _tokenBuffer.TotalEmitted + _tokenBuffer.Count,
            flowLevel: _flowLevel,
            line: _line,
            column: _consumed - _lineStart + 1,
            offset: _totalConsumed + _consumed
        ));
    }
    
    /// <summary>
    /// Decreases the flow level when exiting a flow collection.
    /// </summary>
    private void DecreaseFlowLevel()
    {
        if (_flowLevel > 0)
        {
            _flowLevel--;
            if (!_simpleKeyStack.IsEmpty && _simpleKeyStack.Top().FlowLevel > _flowLevel)
            {
                _simpleKeyStack.Pop();
            }
        }
    }
}
