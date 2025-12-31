# Yamlify Architecture

High-level overview of the parser design and architectural decisions.

> For usage documentation, see the main [README](../README.md).  
> For serialization features, see the [Serialization Guide](./serialization.md).

## Design Philosophy

Yamlify is built around four core principles:

1. **Zero-Copy Parsing** - Tokens reference ranges in the source buffer rather than copying data
2. **Stack-First Allocation** - Common cases use inline storage on the stack, with heap fallback only for deeply nested structures
3. **Pull-Based API** - Similar to `System.Text.Json.Utf8JsonReader`, the caller controls iteration
4. **Full YAML 1.2 Compliance** - Complete spec coverage including edge cases like implicit mapping keys

## Architecture Overview

The reader is implemented as a `ref struct` for stack allocation:

```
┌─────────────────────────────────────────────────────────────┐
│                    Utf8YamlReader                           │
│                    (ref struct)                             │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │Token Buffer │  │ Simple Key  │  │ Indentation/Flow    │  │
│  │ (lookahead) │  │   Stack     │  │    Tracking         │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│                  ReadOnlySpan<byte> source                  │
└─────────────────────────────────────────────────────────────┘
```

### Key Components

#### Token Buffer

A lookahead buffer that stores pending tokens. Uses inline storage for the common case with automatic overflow to heap for complex documents.

This enables:
- O(1) token peek and consume operations
- Retroactive token insertion (needed for implicit keys)
- Efficient handling of anchors, tags, and other lookahead scenarios

#### Simple Key Tracking

YAML allows implicit mapping keys (keys without explicit `?`). The parser doesn't know something is a key until it sees the following `:`. 

The simple key stack tracks potential keys at each nesting level. When `:` is encountered, a `Key` token is retroactively inserted at the correct position.

#### Indentation & Flow Tracking

- **Block context**: Indentation-based nesting using a stack of indent levels
- **Flow context**: Bracket-based nesting (`[]`, `{}`) tracked separately
- Both support deep nesting via bit-packed storage

## Performance Characteristics

| Operation | Complexity | Notes |
|-----------|------------|-------|
| `Read()` | O(1) amortized | Consumes from buffer or tokenizes next |
| Token peek | O(1) | Direct array access |
| Memory | O(1) typical | Stack allocation; heap only for edge cases |

## Low-Level Reader API

### Basic Usage

```csharp
var reader = new Utf8YamlReader(yamlBytes);

while (reader.Read())
{
    switch (reader.TokenType)
    {
        case YamlTokenType.MappingStart:
            // Begin processing a mapping
            break;
        case YamlTokenType.Scalar:
            string? value = reader.GetString();
            break;
    }
}
```

### Streaming Large Files

For files larger than available memory, use chunked reading with `YamlReaderState`:

```csharp
using var stream = File.OpenRead("large.yaml");
var buffer = new byte[4096];
var state = new YamlReaderState();

while (true)
{
    int bytesRead = stream.Read(buffer);
    bool isFinalBlock = bytesRead < buffer.Length;
    
    var reader = new Utf8YamlReader(
        buffer.AsSpan(0, bytesRead), 
        isFinalBlock, 
        state);
    
    while (reader.Read())
    {
        // Process tokens
    }
    
    state = reader.CurrentState;
    if (isFinalBlock) break;
}
```

### Skipping Content

```csharp
// Skip entire collections without processing
if (reader.TokenType == YamlTokenType.MappingStart)
{
    reader.Skip(); // Skips to matching MappingEnd
}
```

## Source Generator

Yamlify includes a source generator that produces AOT-compatible serialization code at compile time. See the [Serialization Guide](./serialization.md) for details on:

- Type registration with `[YamlSerializable]`
- Polymorphic serialization
- Custom converters
- Serialization options