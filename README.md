# Yamlify

<p align="center">
  <i>✨ 100% vibe engineered ✨</i>
</p>

A high-performance, AOT-compatible YAML 1.2 serializer for .NET.

[![NuGet](https://img.shields.io/nuget/v/Yamlify.svg)](https://www.nuget.org/packages/Yamlify)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Features

- **100% AOT-compatible** - No reflection at runtime, works with Native AOT, iOS, and WebAssembly
- **Source-generated serialization** - Type-safe serialization code generated at compile time
- **Zero-allocation parsing** - Uses `ref struct` and `Span<T>` for allocation-free operation
- **YAML 1.2 compliant** - Full support for the latest YAML specification
- **Familiar API** - Similar patterns to `System.Text.Json`

## YAML Compliance

Full [YAML 1.2](https://yaml.org/spec/1.2.2/) support. Tested against the official [yaml-test-suite](https://github.com/yaml/yaml-test-suite) (v2022-01-17).

## Installation

```bash
dotnet add package Yamlify
```

## Quick Start

### 1. Define your types and context

```csharp
using Yamlify.Serialization;

// Your data types
public class Person
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public List<string>? Tags { get; set; }
}

// Register types for source generation
[YamlSerializable(typeof(Person))]
public partial class MySerializerContext : YamlSerializerContext { }
```

### 2. Serialize to YAML

```csharp
var person = new Person
{
    Name = "John Doe",
    Age = 30,
    Tags = ["developer", "yaml"]
};

// Option A: Pass context explicitly
string yaml = YamlSerializer.Serialize(person, MySerializerContext.Default.Person);

// Option B: Set default resolver once, use simple API everywhere
YamlSerializerOptions.Default.TypeInfoResolver = MySerializerContext.Default;
string yaml = YamlSerializer.Serialize(person);  // No context needed!

// Output:
// name: John Doe
// age: 30
// tags:
//   - developer
//   - yaml
```

### 3. Deserialize from YAML

```csharp
var yaml = """
    name: Jane Smith
    age: 25
    tags:
      - designer
    """;

// With explicit context
var person = YamlSerializer.Deserialize<Person>(yaml, MySerializerContext.Default.Person);

// Or with default resolver configured
var person = YamlSerializer.Deserialize<Person>(yaml);
```

## Serialization Options

```csharp
var options = new YamlSerializerOptions
{
    // Property naming
    PropertyNamingPolicy = YamlNamingPolicy.CamelCase,  // or SnakeCase, KebabCase (default)
    
    // Null handling
    IgnoreNullValues = true,        // Omit properties with null values
    IgnoreEmptyObjects = true,      // Omit nested objects where all nullable props are null
    
    // Circular references
    ReferenceHandler = ReferenceHandler.IgnoreCycles,
    
    // Formatting
    WriteIndented = true,
    IndentSize = 2
};

var yaml = YamlSerializer.Serialize(person, MySerializerContext.Default.Person, options);
```

> See the [Serialization Guide](docs/serialization.md#serialization-options) for more details.

## Attributes

### Property Customization

```csharp
public class Product
{
    [YamlPropertyName("product-id")]
    public string Id { get; set; } = "";
    
    [YamlIgnore]
    public string InternalCode { get; set; } = "";
    
    [YamlPropertyOrder(1)]
    public string Name { get; set; } = "";
    
    [YamlPropertyOrder(2)]
    public decimal Price { get; set; }
}
```

### Polymorphic Serialization

Yamlify supports polymorphic type hierarchies with type discriminators:

```csharp
[YamlPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[YamlDerivedType(typeof(Dog), "dog")]
[YamlDerivedType(typeof(Cat), "cat")]
public abstract class Animal
{
    public string Name { get; set; } = "";
}

public class Dog : Animal { public string Breed { get; set; } = ""; }
public class Cat : Animal { public bool IsIndoor { get; set; } }
```

```yaml
$type: dog
name: Buddy
breed: Golden Retriever
```

### Sibling Discrimination

For configurations where the type is determined by a sibling property (e.g., an enum):

```csharp
public class ConfigItem
{
    public string Name { get; set; } = "";
    public ValueType Type { get; set; }
    
    [YamlSiblingDiscriminator(nameof(Type))]
    [YamlDiscriminatorMapping(nameof(ValueType.String), typeof(StringValue))]
    [YamlDiscriminatorMapping(nameof(ValueType.Integer), typeof(IntegerValue))]
    public ConfigValue? Value { get; set; }
}
```

> See the [Serialization Guide](docs/serialization.md#polymorphic-serialization) for complete polymorphism documentation.

## Low-Level Reader API

For advanced scenarios, use the low-level reader directly:

```csharp
using Yamlify.Core;

var yaml = """
    name: John
    age: 30
    """u8;

var reader = new Utf8YamlReader(yaml);
while (reader.Read())
{
    Console.WriteLine($"{reader.TokenType}: {reader.GetString()}");
}
```

## Documentation

- [Serialization Guide](docs/serialization.md) - Polymorphism, sibling discrimination, options, and attributes
- [Architecture & Internals](docs/architecture.md) - Deep dive into parser design

## Building & Testing

### Clone the repository

```bash
# Clone with submodules (recommended)
git clone --recurse-submodules https://github.com/SwissLife-OSS/yamlify.git

# Or if already cloned, initialize submodules
git submodule update --init --recursive
```

### Run tests

```bash
dotnet test
```

The test suite includes the official [yaml-test-suite](https://github.com/yaml/yaml-test-suite) as a git submodule for YAML compliance testing.

## License

MIT License - see [LICENSE](LICENSE) for details.
