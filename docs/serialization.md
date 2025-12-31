# Yamlify Serialization Guide

Comprehensive guide to Yamlify's source-generated serialization features.

> For basic usage, see the main [README](../README.md).  
> For parser internals, see [Architecture](./architecture.md).

## Table of Contents

- [Source Generation](#source-generation)
- [Polymorphic Serialization](#polymorphic-serialization)
- [Sibling Discrimination](#sibling-discrimination)
- [Serialization Options](#serialization-options)
- [Property Attributes](#property-attributes)

---

## Source Generation

Yamlify uses C# source generators to create type-safe serialization code at compile time, ensuring 100% AOT compatibility.

### Basic Setup

```csharp
using Yamlify.Serialization;

// Define your types
public class Person
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

// Register types in a serializer context
[YamlSerializable(typeof(Person))]
public partial class MySerializerContext : YamlSerializerContext { }
```

### Using the Context

```csharp
// Option A: Explicit context (recommended for libraries)
var yaml = YamlSerializer.Serialize(person, MySerializerContext.Default.Person);
var result = YamlSerializer.Deserialize(yaml, MySerializerContext.Default.Person);

// Option B: Default resolver (convenient for applications)
YamlSerializerOptions.Default.TypeInfoResolver = MySerializerContext.Default;
var yaml = YamlSerializer.Serialize(person);
var result = YamlSerializer.Deserialize<Person>(yaml);
```

---

## Polymorphic Serialization

Yamlify supports serializing and deserializing polymorphic type hierarchies using type discriminators.

### Attribute-Based Polymorphism

Use `[YamlPolymorphic]` and `[YamlDerivedType]` attributes on the base type:

```csharp
[YamlPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[YamlDerivedType(typeof(Dog), "dog")]
[YamlDerivedType(typeof(Cat), "cat")]
[YamlDerivedType(typeof(Bird), "bird")]
public abstract class Animal
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

public class Dog : Animal
{
    public string Breed { get; set; } = "";
    public bool IsTrained { get; set; }
}

public class Cat : Animal
{
    public bool IsIndoor { get; set; }
}

public class Bird : Animal
{
    public double WingSpan { get; set; }
}
```

Register all types in your context:

```csharp
[YamlSerializable(typeof(Animal))]
[YamlSerializable(typeof(Dog))]
[YamlSerializable(typeof(Cat))]
[YamlSerializable(typeof(Bird))]
public partial class MySerializerContext : YamlSerializerContext { }
```

**Serialization output:**

```yaml
$type: dog
name: Buddy
age: 3
breed: Golden Retriever
is-trained: true
```

### Context-Based Polymorphism

Alternatively, configure polymorphism in the `[YamlSerializable]` attribute:

```csharp
[YamlSerializable(typeof(Animal),
    TypeDiscriminatorPropertyName = "$type",
    DerivedTypes = new[] { typeof(Dog), typeof(Cat), typeof(Bird) },
    DerivedTypeDiscriminators = new[] { "dog", "cat", "bird" })]
public partial class MySerializerContext : YamlSerializerContext { }
```

### Polymorphic Collections

Collections of base types automatically serialize with discriminators:

```csharp
public class Zoo
{
    public string Name { get; set; } = "";
    public List<Animal>? Animals { get; set; }
}
```

```yaml
name: City Zoo
animals:
  - $type: dog
    name: Buddy
    breed: Labrador
  - $type: cat
    name: Whiskers
    is-indoor: true
  - $type: bird
    name: Tweety
    wing-span: 0.3
```

### Interface Polymorphism

Interfaces work the same way as abstract classes:

```csharp
[YamlPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[YamlDerivedType(typeof(Rectangle), "rectangle")]
[YamlDerivedType(typeof(Circle), "circle")]
public interface IShape
{
    double Area { get; }
}

public class Rectangle : IShape
{
    public double Width { get; set; }
    public double Height { get; set; }
    public double Area => Width * Height;
}

public class Circle : IShape
{
    public double Radius { get; set; }
    public double Area => Math.PI * Radius * Radius;
}
```

---

## Sibling Discrimination

Sibling discrimination allows the type discriminator to be a separate property (sibling) rather than embedded in the polymorphic object itself. This is useful for configurations where the type is determined by an enum or string field.

### Basic Sibling Discrimination

```csharp
public enum ValueType
{
    String,
    Integer,
    Boolean
}

public abstract class ConfigValue
{
    public string? Description { get; set; }
}

public class StringValue : ConfigValue
{
    public string? Value { get; set; }
}

public class IntegerValue : ConfigValue
{
    public int? Value { get; set; }
}

public class BooleanValue : ConfigValue
{
    public bool? Value { get; set; }
}

public class ConfigItem
{
    public string Name { get; set; } = "";
    
    public ValueType Type { get; set; }
    
    [YamlSiblingDiscriminator(nameof(Type))]
    [YamlDiscriminatorMapping(nameof(ValueType.String), typeof(StringValue))]
    [YamlDiscriminatorMapping(nameof(ValueType.Integer), typeof(IntegerValue))]
    [YamlDiscriminatorMapping(nameof(ValueType.Boolean), typeof(BooleanValue))]
    public ConfigValue? Value { get; set; }
}
```

**YAML output:**

```yaml
name: port
type: Integer
value:
  value: 8080
```

### Sibling Discrimination with Dictionaries

Sibling discrimination also works with dictionary values:

```csharp
public class ConfigContainer
{
    public ValueType Type { get; set; }
    
    [YamlSiblingDiscriminator(nameof(Type))]
    [YamlDiscriminatorMapping(nameof(ValueType.String), typeof(StringValue))]
    [YamlDiscriminatorMapping(nameof(ValueType.Integer), typeof(IntegerValue))]
    public Dictionary<string, ConfigValue?>? Overrides { get; set; }
}
```

### How It Works

1. During serialization, the runtime type of the `Value` property determines which derived type converter to use
2. During deserialization, the `Type` property is read first, then used to select the correct derived type for `Value`
3. The sibling discriminator property must appear before the polymorphic property in the YAML (property order matters)

---

## Serialization Options

### Context-Level Options

Configure options at the context level using `[YamlSourceGenerationOptions]`:

```csharp
[YamlSerializable(typeof(MyClass))]
[YamlSourceGenerationOptions(
    IgnoreNullValues = true,
    IgnoreEmptyObjects = true,
    PropertyNamingPolicy = YamlNamingPolicy.KebabCase)]
public partial class MySerializerContext : YamlSerializerContext { }
```

### Runtime Options

Override options at runtime using `YamlSerializerOptions`:

```csharp
var options = new YamlSerializerOptions
{
    PropertyNamingPolicy = YamlNamingPolicy.CamelCase,
    IgnoreNullValues = true,
    WriteIndented = true,
    IndentSize = 2,
    ReferenceHandler = ReferenceHandler.IgnoreCycles
};

var yaml = YamlSerializer.Serialize(obj, context.MyClass, options);
```

### IgnoreNullValues

When enabled, properties with `null` values are omitted from the output:

```csharp
public class Person
{
    public string? Name { get; set; }
    public string? Email { get; set; }  // null - will be omitted
}
```

```yaml
name: John
# email is omitted
```

### IgnoreEmptyObjects

When enabled, nested objects where all nullable properties are `null` are treated as empty and omitted:

```csharp
public class Container
{
    public string? Name { get; set; }
    public Metadata? Metadata { get; set; }
}

public class Metadata
{
    public string? Author { get; set; }
    public string? Version { get; set; }
}

// If Metadata = new Metadata { Author = null, Version = null }
// The entire metadata property is omitted
```

```yaml
name: MyContainer
# metadata is omitted because it's empty
```

**Important:** `IgnoreEmptyObjects` correctly handles polymorphic types. It checks the actual derived type's properties, not just the base type's properties:

```csharp
// This IntegerValue will NOT be incorrectly skipped
var item = new ConfigItem
{
    Type = ValueType.Integer,
    Value = new IntegerValue
    {
        Description = null,  // Base class property is null
        Value = 8080         // Derived class property has value
    }
};
// Output includes value: because IntegerValue.Value is not null
```

### Property Naming Policies

| Policy | Example |
|--------|---------|
| `KebabCase` (default) | `firstName` → `first-name` |
| `CamelCase` | `FirstName` → `firstName` |
| `SnakeCase` | `FirstName` → `first_name` |
| `PascalCase` | `firstName` → `FirstName` |

---

## Property Attributes

### YamlPropertyName

Override the YAML property name:

```csharp
public class Product
{
    [YamlPropertyName("product-id")]
    public string Id { get; set; } = "";
}
```

### YamlIgnore

Exclude a property from serialization:

```csharp
public class User
{
    public string Name { get; set; } = "";
    
    [YamlIgnore]
    public string PasswordHash { get; set; } = "";
}
```

### YamlPropertyOrder

Control the order of properties in the output:

```csharp
public class Config
{
    [YamlPropertyOrder(0)]
    public string Name { get; set; } = "";
    
    [YamlPropertyOrder(1)]
    public string Version { get; set; } = "";
    
    [YamlPropertyOrder(2)]
    public Dictionary<string, string>? Settings { get; set; }
}
```

### YamlIncludePrivate

Include private or internal properties:

```csharp
public class Entity
{
    public string Name { get; set; } = "";
    
    [YamlIncludePrivate]
    internal string InternalId { get; set; } = "";
}
```

---

## Best Practices

### 1. Register All Types

Ensure all types in your object graph are registered:

```csharp
[YamlSerializable(typeof(Parent))]
[YamlSerializable(typeof(Child))]       // Don't forget nested types
[YamlSerializable(typeof(GrandChild))]  // Or deeply nested ones
public partial class MyContext : YamlSerializerContext { }
```

### 2. Use Property Order for Readability

Order important properties first:

```csharp
public class Service
{
    [YamlPropertyOrder(0)]
    public string Name { get; set; } = "";  // Name first
    
    [YamlPropertyOrder(1)]
    public string Type { get; set; } = "";  // Then type
    
    // Other properties follow
    public List<string>? Tags { get; set; }
}
```

### 3. Prefer Explicit Contexts for Libraries

If you're building a library, accept `YamlTypeInfo<T>` or context parameters rather than relying on default resolvers:

```csharp
public class MyLibrary
{
    public T LoadConfig<T>(string path, YamlTypeInfo<T> typeInfo)
    {
        var yaml = File.ReadAllText(path);
        return YamlSerializer.Deserialize(yaml, typeInfo)!;
    }
}
```

### 4. Test Round-Trips

Always test that your types can round-trip correctly:

```csharp
[Fact]
public void Config_RoundTrips_Correctly()
{
    var original = new Config { Name = "test", Value = 42 };
    
    var yaml = YamlSerializer.Serialize(original, MyContext.Default.Config);
    var result = YamlSerializer.Deserialize(yaml, MyContext.Default.Config);
    
    Assert.Equal(original.Name, result?.Name);
    Assert.Equal(original.Value, result?.Value);
}
```
