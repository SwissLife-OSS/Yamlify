using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yamlify;

/// <summary>
/// Represents a raw YAML token in the lookahead buffer.
/// Uses Range to reference source bytes without copying.
/// </summary>
/// <remarks>
/// This struct is designed for zero-copy token storage. The actual token
/// content remains in the source buffer and is referenced by ValueRange.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct RawToken
{
    /// <summary>
    /// The type of this token.
    /// </summary>
    public YamlTokenType Type;

    /// <summary>
    /// The scalar style (for scalar tokens) or collection style info.
    /// </summary>
    public ScalarStyle Style;

    /// <summary>
    /// Start index into the source buffer for the token value.
    /// </summary>
    public int ValueStart;

    /// <summary>
    /// Length of the token value in the source buffer.
    /// </summary>
    public int ValueLength;

    /// <summary>
    /// The line number where this token starts (1-based).
    /// </summary>
    public int StartLine;

    /// <summary>
    /// The column number where this token starts (1-based).
    /// </summary>
    public int StartColumn;

    /// <summary>
    /// The byte offset where this token starts.
    /// </summary>
    public long StartOffset;

    /// <summary>
    /// Creates a new token with the specified properties.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawToken Create(
        YamlTokenType type,
        int valueStart = 0,
        int valueLength = 0,
        ScalarStyle style = ScalarStyle.Any,
        int line = 0,
        int column = 0,
        long offset = 0)
    {
        return new RawToken
        {
            Type = type,
            Style = style,
            ValueStart = valueStart,
            ValueLength = valueLength,
            StartLine = line,
            StartColumn = column,
            StartOffset = offset
        };
    }

    /// <summary>
    /// Gets the value span from the source buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<byte> GetValueSpan(ReadOnlySpan<byte> sourceBuffer)
    {
        if (ValueLength == 0) return default;
        return sourceBuffer.Slice(ValueStart, ValueLength);
    }

    /// <summary>
    /// Gets the mark (position) for this token.
    /// </summary>
    public readonly Mark GetMark() => new(StartOffset, StartLine, StartColumn);
}

/// <summary>
/// Tracks a potential implicit key (simple key) for retroactive KEY token insertion.
/// </summary>
/// <remarks>
/// In YAML, an implicit key is a key without an explicit '?' indicator.
/// We need to track potential keys because we don't know if something
/// is a key until we see the ':' that follows it.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct SimpleKeyInfo
{
    /// <summary>
    /// Is a simple key possible at this position?
    /// </summary>
    public bool Possible;

    /// <summary>
    /// Is a simple key required? (Would be an error if not found)
    /// </summary>
    public bool Required;

    /// <summary>
    /// The position in the token buffer where KEY should be inserted.
    /// </summary>
    public int TokenIndex;

    /// <summary>
    /// The flow level at which this key was saved.
    /// </summary>
    public int FlowLevel;

    /// <summary>
    /// The line where the potential key started.
    /// </summary>
    public int Line;

    /// <summary>
    /// The column where the potential key started.
    /// </summary>
    public int Column;

    /// <summary>
    /// The byte offset where the potential key started.
    /// </summary>
    public long Offset;

    /// <summary>
    /// Creates a new simple key info.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SimpleKeyInfo Create(
        bool possible,
        bool required,
        int tokenIndex,
        int flowLevel,
        int line,
        int column,
        long offset)
    {
        return new SimpleKeyInfo
        {
            Possible = possible,
            Required = required,
            TokenIndex = tokenIndex,
            FlowLevel = flowLevel,
            Line = line,
            Column = column,
            Offset = offset
        };
    }

    /// <summary>
    /// Checks if this simple key is stale (too old to be valid).
    /// A key is stale if it spans multiple lines in block context,
    /// or if it exceeds 1024 characters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsStale(int currentLine, long currentOffset, bool inBlockContext)
    {
        if (!Possible) return false;

        // In block context, keys cannot span lines
        if (inBlockContext && Line < currentLine)
            return true;

        // Keys cannot exceed 1024 bytes (YAML spec limit)
        if (currentOffset - Offset > 1024)
            return true;

        return false;
    }

    /// <summary>
    /// Marks this simple key as no longer possible.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invalidate()
    {
        Possible = false;
    }
}

/// <summary>
/// A fixed-size inline buffer for tokens with heap overflow support.
/// Provides O(1) access and amortized O(1) insertion.
/// </summary>
/// <remarks>
/// This buffer stores up to 8 tokens inline (on the stack) and falls
/// back to heap allocation only for pathological cases like deeply
/// nested flow collections used as mapping keys.
/// </remarks>
internal ref struct TokenBuffer
{
    // Inline storage for common cases (8 tokens = ~320 bytes)
    // These fields are accessed via Unsafe.Add from _token0
#pragma warning disable CS0169
    private RawToken _token0, _token1, _token2, _token3;
    private RawToken _token4, _token5, _token6, _token7;
#pragma warning restore CS0169

    // Overflow storage for rare complex cases
    private RawToken[]? _overflow;
    private int _overflowCount;

    // Buffer state
    private int _head;     // Index of first token
    private int _count;    // Number of tokens in buffer
    private int _totalEmitted; // Total tokens emitted (for simple key tracking)

    private const int InlineCapacity = 8;

    /// <summary>
    /// Gets the number of tokens currently in the buffer.
    /// </summary>
    public readonly int Count => _count + _overflowCount;

    /// <summary>
    /// Gets the total number of tokens that have been emitted.
    /// </summary>
    public readonly int TotalEmitted => _totalEmitted;

    /// <summary>
    /// Gets whether the buffer is empty.
    /// </summary>
    public readonly bool IsEmpty => _count == 0 && _overflowCount == 0;

    /// <summary>
    /// Peeks at a token at the specified offset from the head.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref RawToken Peek(int offset = 0)
    {
        if (offset >= Count)
            throw new InvalidOperationException("Token buffer underflow");

        if (offset < _count)
        {
            int index = (_head + offset) % InlineCapacity;
            return ref GetInlineRef(index);
        }

        // In overflow
        return ref _overflow![offset - _count];
    }

    /// <summary>
    /// Consumes (removes) the token at the head of the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RawToken Consume()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Token buffer underflow");

        RawToken token;

        if (_count > 0)
        {
            token = GetInlineRef(_head);
            _head = (_head + 1) % InlineCapacity;
            _count--;
        }
        else
        {
            // Consume from overflow
            token = _overflow![0];
            Array.Copy(_overflow, 1, _overflow, 0, --_overflowCount);
        }

        _totalEmitted++;
        return token;
    }

    /// <summary>
    /// Enqueues a token at the tail of the buffer.
    /// </summary>
    public void Enqueue(RawToken token)
    {
        if (_count < InlineCapacity)
        {
            int tail = (_head + _count) % InlineCapacity;
            SetInline(tail, token);
            _count++;
        }
        else
        {
            // Use overflow
            _overflow ??= new RawToken[8];
            if (_overflowCount >= _overflow.Length)
            {
                Array.Resize(ref _overflow, _overflow.Length * 2);
            }
            _overflow[_overflowCount++] = token;
        }
    }

    /// <summary>
    /// Inserts a token at a specific logical position.
    /// Used for retroactive KEY token insertion.
    /// </summary>
    public void InsertAt(int logicalPosition, RawToken token)
    {
        // Calculate where in the buffer this position maps to
        int insertOffset = logicalPosition - (_totalEmitted - Count);

        if (insertOffset < 0 || insertOffset > Count)
        {
            // Position is before our buffer or after - just enqueue
            Enqueue(token);
            return;
        }

        // Need to shift tokens to make room
        if (_count < InlineCapacity && _overflowCount == 0)
        {
            // All in inline buffer - shift within inline
            int insertIndex = (_head + insertOffset) % InlineCapacity;

            // Shift tokens from insertIndex to end
            for (int i = _count; i > insertOffset; i--)
            {
                int from = (_head + i - 1) % InlineCapacity;
                int to = (_head + i) % InlineCapacity;
                SetInline(to, GetInlineRef(from));
            }

            SetInline(insertIndex, token);
            _count++;
        }
        else
        {
            // Complex case with overflow - use temporary list
            var temp = new List<RawToken>(Count + 1);
            for (int i = 0; i < Count; i++)
            {
                if (i == insertOffset)
                    temp.Add(token);
                temp.Add(Peek(i));
            }
            if (insertOffset >= Count)
                temp.Add(token);

            // Rebuild buffer
            Clear();
            foreach (var t in temp)
                Enqueue(t);
        }
    }

    /// <summary>
    /// Clears all tokens from the buffer.
    /// </summary>
    public void Clear()
    {
        _head = 0;
        _count = 0;
        _overflowCount = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly ref RawToken GetInlineRef(int index)
    {
        // Unsafe but necessary for ref return from value types
        // In production, consider using Unsafe.Add
        return ref Unsafe.Add(ref Unsafe.AsRef(in _token0), index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetInline(int index, RawToken value)
    {
        Unsafe.Add(ref _token0, index) = value;
    }
}

/// <summary>
/// Stack for tracking simple keys at each flow level.
/// Inline storage for up to 8 levels, heap fallback for deeper nesting.
/// </summary>
internal ref struct SimpleKeyStack
{
    // Inline storage (8 levels covers most real-world cases)
    // These fields are accessed via Unsafe.Add from _key0
#pragma warning disable CS0169
    private SimpleKeyInfo _key0, _key1, _key2, _key3;
    private SimpleKeyInfo _key4, _key5, _key6, _key7;
#pragma warning restore CS0169

    // Overflow for deeply nested flows
    private SimpleKeyInfo[]? _overflow;

    private int _count;

    private const int InlineCapacity = 8;

    /// <summary>
    /// Gets the current nesting depth.
    /// </summary>
    public readonly int Count => _count;

    /// <summary>
    /// Pushes a new simple key context onto the stack.
    /// </summary>
    public void Push(SimpleKeyInfo key)
    {
        if (_count < InlineCapacity)
        {
            SetInline(_count, key);
        }
        else
        {
            _overflow ??= new SimpleKeyInfo[4];
            int overflowIndex = _count - InlineCapacity;
            if (overflowIndex >= _overflow.Length)
            {
                Array.Resize(ref _overflow, _overflow.Length * 2);
            }
            _overflow[overflowIndex] = key;
        }
        _count++;
    }

    /// <summary>
    /// Pops the top simple key context from the stack.
    /// </summary>
    public SimpleKeyInfo Pop()
    {
        if (_count == 0)
            throw new InvalidOperationException("SimpleKey stack underflow");

        _count--;

        if (_count < InlineCapacity)
        {
            return GetInline(_count);
        }
        else
        {
            return _overflow![_count - InlineCapacity];
        }
    }

    /// <summary>
    /// Gets a reference to the top simple key context.
    /// </summary>
    public ref SimpleKeyInfo Top()
    {
        if (_count == 0)
            throw new InvalidOperationException("SimpleKey stack empty");

        int index = _count - 1;
        if (index < InlineCapacity)
        {
            return ref GetInlineRef(index);
        }
        else
        {
            return ref _overflow![index - InlineCapacity];
        }
    }

    /// <summary>
    /// Gets whether the stack is empty.
    /// </summary>
    public readonly bool IsEmpty => _count == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly SimpleKeyInfo GetInline(int index)
    {
        return Unsafe.Add(ref Unsafe.AsRef(in _key0), index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetInline(int index, SimpleKeyInfo value)
    {
        Unsafe.Add(ref _key0, index) = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref SimpleKeyInfo GetInlineRef(int index)
    {
        return ref Unsafe.Add(ref Unsafe.AsRef(in _key0), index);
    }
}

/// <summary>
/// Storage for custom tag handles declared via TAG directives.
/// Uses inline storage for up to 4 handles, each up to 32 bytes.
/// TAG directives are document-scoped and must be cleared on document start.
/// </summary>
internal struct TagHandleStorage
{
    private const int MaxHandleLength = 32;
    private const int MaxHandles = 4;

    // Inline storage for handles (fixed-size arrays)
    // These fields are accessed via Unsafe.Add from _handle0
#pragma warning disable CS0169, CS0649
    private TagHandleEntry _handle0;
    private TagHandleEntry _handle1;
    private TagHandleEntry _handle2;
    private TagHandleEntry _handle3;
#pragma warning restore CS0169, CS0649

    private int _count;

    /// <summary>
    /// Registers a custom tag handle from a TAG directive.
    /// </summary>
    /// <param name="handle">The tag handle (e.g., "!prefix!").</param>
    public void Register(ReadOnlySpan<byte> handle)
    {
        if (_count >= MaxHandles)
        {
            throw new InvalidOperationException($"Too many TAG directives (max {MaxHandles})");
        }

        if (handle.Length > MaxHandleLength)
        {
            throw new InvalidOperationException($"Tag handle too long (max {MaxHandleLength} bytes)");
        }

        SetEntry(_count, handle);
        _count++;
    }

    /// <summary>
    /// Checks if a named tag handle has been registered.
    /// </summary>
    /// <param name="handle">The tag handle to check (e.g., "!prefix!").</param>
    /// <returns>True if the handle was registered via a TAG directive.</returns>
    public readonly bool IsRegistered(ReadOnlySpan<byte> handle)
    {
        for (int i = 0; i < _count; i++)
        {
            if (MatchesEntry(i, handle))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Clears all registered tag handles. Called at document boundaries.
    /// </summary>
    public void Clear()
    {
        _count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetEntry(int index, ReadOnlySpan<byte> handle)
    {
        ref var entry = ref Unsafe.Add(ref Unsafe.AsRef(in _handle0), index);
        handle.CopyTo(entry.Data.AsSpan());
        entry.Length = (byte)handle.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly bool MatchesEntry(int index, ReadOnlySpan<byte> handle)
    {
        ref readonly var entry = ref Unsafe.Add(ref Unsafe.AsRef(in _handle0), index);
        return handle.Length == entry.Length && 
               handle.SequenceEqual(entry.Data.AsReadOnlySpan()[..entry.Length]);
    }

    /// <summary>
    /// Entry for a single tag handle.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct TagHandleEntry
    {
        // Fixed buffer for handle bytes
        public InlineBuffer32 Data;
        public byte Length;
    }

    /// <summary>
    /// Inline buffer for 32 bytes without using unsafe fixed arrays.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [InlineArray(32)]
    private struct InlineBuffer32
    {
        private byte _element0;

        public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _element0, 32);
        
        public readonly ReadOnlySpan<byte> AsReadOnlySpan() => MemoryMarshal.CreateReadOnlySpan(in _element0, 32);
    }
}
