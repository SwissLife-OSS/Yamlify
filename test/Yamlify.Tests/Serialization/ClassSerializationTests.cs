using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Simple class for basic serialization tests.
/// </summary>
public class SimpleClass
{
    public string? Name { get; set; }
    public int Value { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Class with nested objects for hierarchy tests.
/// </summary>
public class ParentClass
{
    public string? Title { get; set; }
    public SimpleClass? Child { get; set; }
}

/// <summary>
/// Deeply nested class structure.
/// </summary>
public class Level1
{
    public string? Name { get; set; }
    public Level2? Next { get; set; }
}

public class Level2
{
    public string? Name { get; set; }
    public Level3? Next { get; set; }
}

public class Level3
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}

/// <summary>
/// Class for testing circular reference handling.
/// </summary>
public class CircularReferenceClass
{
    public string? Name { get; set; }
    public CircularReferenceClass? Parent { get; set; }
    public List<CircularReferenceClass>? Children { get; set; }
}

/// <summary>
/// Tests for serializing and deserializing classes.
/// </summary>
public class ClassSerializationTests
{
    [Fact]
    public void SerializeSimpleClass()
    {
        var obj = new SimpleClass
        {
            Name = "Test",
            Value = 42,
            IsActive = true
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SimpleClass);

        Assert.Contains("name:", yaml);
        Assert.Contains("Test", yaml);
        Assert.Contains("value:", yaml);
        Assert.Contains("42", yaml);
        Assert.Contains("is-active:", yaml);
        Assert.Contains("true", yaml);
    }

    [Fact]
    public void DeserializeSimpleClass()
    {
        var yaml = """
            name: Test
            value: 42
            is-active: true
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(obj);
        Assert.Equal("Test", obj.Name);
        Assert.Equal(42, obj.Value);
        Assert.True(obj.IsActive);
    }

    [Fact]
    public void SerializeNestedClass()
    {
        var obj = new ParentClass
        {
            Title = "Parent",
            Child = new SimpleClass
            {
                Name = "Child",
                Value = 100,
                IsActive = false
            }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.ParentClass);

        Assert.Contains("title:", yaml);
        Assert.Contains("Parent", yaml);
        Assert.Contains("child:", yaml);
        Assert.Contains("name:", yaml);
        Assert.Contains("Child", yaml);
    }

    [Fact]
    public void DeserializeNestedClass()
    {
        var yaml = """
            title: Parent
            child:
              name: Child
              value: 100
              is-active: false
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ParentClass);

        Assert.NotNull(obj);
        Assert.Equal("Parent", obj.Title);
        Assert.NotNull(obj.Child);
        Assert.Equal("Child", obj.Child.Name);
        Assert.Equal(100, obj.Child.Value);
        Assert.False(obj.Child.IsActive);
    }

    [Fact]
    public void SerializeDeeplyNestedClass()
    {
        var obj = new Level1
        {
            Name = "L1",
            Next = new Level2
            {
                Name = "L2",
                Next = new Level3
                {
                    Name = "L3",
                    Value = "DeepValue"
                }
            }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.Level1);

        Assert.Contains("L1", yaml);
        Assert.Contains("L2", yaml);
        Assert.Contains("L3", yaml);
        Assert.Contains("DeepValue", yaml);
    }

    [Fact]
    public void DeserializeDeeplyNestedClass()
    {
        var yaml = """
            name: L1
            next:
              name: L2
              next:
                name: L3
                value: DeepValue
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.Level1);

        Assert.NotNull(obj);
        Assert.Equal("L1", obj.Name);
        Assert.NotNull(obj.Next);
        Assert.Equal("L2", obj.Next.Name);
        Assert.NotNull(obj.Next.Next);
        Assert.Equal("L3", obj.Next.Next.Name);
        Assert.Equal("DeepValue", obj.Next.Next.Value);
    }

    [Fact]
    public void SerializeClassWithNullProperties()
    {
        var obj = new SimpleClass { Name = null, Value = 0, IsActive = false };
        var options = new YamlSerializerOptions { IgnoreNullValues = false };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SimpleClass, options);

        Assert.Contains("null", yaml);
    }

    [Fact]
    public void IgnoreNullPropertiesWhenConfigured()
    {
        var obj = new SimpleClass { Name = null, Value = 0, IsActive = false };
        var options = new YamlSerializerOptions { IgnoreNullValues = true };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.SimpleClass, options);

        Assert.DoesNotContain("name:", yaml);
    }

    [Fact]
    public void HandleCircularReferenceWithIgnoreCycles()
    {
        var parent = new CircularReferenceClass { Name = "Parent" };
        var child = new CircularReferenceClass { Name = "Child", Parent = parent };
        parent.Children = [child];
        var options = new YamlSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };

        var yaml = YamlSerializer.Serialize(parent, TestSerializerContext.Default.CircularReferenceClass, options);

        // Parent should be serialized with its name
        Assert.Contains("Parent", yaml);
        // Child should be serialized with its name
        Assert.Contains("Child", yaml);
        // The circular reference back to parent should be null (cycle broken)
        // Check that we don't get a stack overflow and the YAML is valid
        Assert.NotEmpty(yaml);
    }

    [Fact]
    public void SerializeWithYamlPropertyNameAttribute()
    {
        var obj = new ClassWithAttributedProperties
        {
            CustomProperty = "CustomValue",
            IgnoredProperty = "ShouldNotAppear",
            RegularProperty = "RegularValue"
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.ClassWithAttributedProperties);

        Assert.Contains("custom-name:", yaml);
        Assert.Contains("CustomValue", yaml);
        Assert.DoesNotContain("IgnoredProperty", yaml);
        Assert.DoesNotContain("ignored-property", yaml);
        Assert.DoesNotContain("ShouldNotAppear", yaml);
        Assert.Contains("regular-property:", yaml);
        Assert.Contains("RegularValue", yaml);
    }

    [Fact]
    public void DeserializeWithYamlPropertyNameAttribute()
    {
        var yaml = """
            custom-name: CustomValue
            regular-property: RegularValue
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ClassWithAttributedProperties);

        Assert.NotNull(obj);
        Assert.Equal("CustomValue", obj.CustomProperty);
        Assert.Equal("RegularValue", obj.RegularProperty);
        Assert.Null(obj.IgnoredProperty);
    }

    [Fact]
    public void SerializeWithPropertyOrderAttribute()
    {
        var obj = new ClassWithPropertyOrder
        {
            First = "1",
            Second = "2",
            Third = "3",
            Unordered = "Last"
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.ClassWithPropertyOrder);

        // Verify order: First should come before Second, Second before Third, Unordered last
        var firstIndex = yaml.IndexOf("first:");
        var secondIndex = yaml.IndexOf("second:");
        var thirdIndex = yaml.IndexOf("third:");
        var unorderedIndex = yaml.IndexOf("unordered:");

        Assert.True(firstIndex < secondIndex, "First should come before Second");
        Assert.True(secondIndex < thirdIndex, "Second should come before Third");
        Assert.True(thirdIndex < unorderedIndex, "Third should come before Unordered");
    }

    #region Type Name Collision Tests

    [Fact]
    public void SerializeTypesWithSameSimpleNameFromDifferentNamespaces()
    {
        var configA = new TypeCollision.NamespaceA.Config { Setting = "value-a" };
        var configB = new TypeCollision.NamespaceB.Config { Value = "value-b", Level = 5 };

        var yamlA = YamlSerializer.Serialize(configA, TestSerializerContext.Default.TypeCollision_NamespaceA_Config);
        var yamlB = YamlSerializer.Serialize(configB, TestSerializerContext.Default.TypeCollision_NamespaceB_Config);

        Assert.Contains("setting:", yamlA);
        Assert.Contains("value-a", yamlA);
        Assert.Contains("value:", yamlB);
        Assert.Contains("value-b", yamlB);
        Assert.Contains("level:", yamlB);
    }

    [Fact]
    public void DeserializeTypesWithSameSimpleNameFromNamespaceA()
    {
        var yaml = """
            setting: test-setting
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.TypeCollision_NamespaceA_Config);

        Assert.NotNull(result);
        Assert.Equal("test-setting", result.Setting);
    }

    [Fact]
    public void DeserializeTypesWithSameSimpleNameFromNamespaceB()
    {
        var yaml = """
            value: test-value
            level: 10
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.TypeCollision_NamespaceB_Config);

        Assert.NotNull(result);
        Assert.Equal("test-value", result.Value);
        Assert.Equal(10, result.Level);
    }

    [Fact]
    public void RoundTripTypesWithSameSimpleName()
    {
        var originalA = new TypeCollision.NamespaceA.Config { Setting = "setting-value" };
        var originalB = new TypeCollision.NamespaceB.Config { Value = "config-value", Level = 3 };

        var yamlA = YamlSerializer.Serialize(originalA, TestSerializerContext.Default.TypeCollision_NamespaceA_Config);
        var yamlB = YamlSerializer.Serialize(originalB, TestSerializerContext.Default.TypeCollision_NamespaceB_Config);

        var resultA = YamlSerializer.Deserialize(yamlA, TestSerializerContext.Default.TypeCollision_NamespaceA_Config);
        var resultB = YamlSerializer.Deserialize(yamlB, TestSerializerContext.Default.TypeCollision_NamespaceB_Config);

        Assert.Equal(originalA.Setting, resultA?.Setting);
        Assert.Equal(originalB.Value, resultB?.Value);
        Assert.Equal(originalB.Level, resultB?.Level);
    }

    #endregion

    #region Mixed Types Tests

    [Fact]
    public void SerializeMixedTypesClass()
    {
        var obj = new MixedTypesClass
        {
            Name = "Test",
            Count = 42,
            Ratio = 3.14,
            Created = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Tags = new List<string> { "tag1", "tag2" },
            Scores = new Dictionary<string, int> { ["a"] = 100 },
            Nested = new SimpleClass { Name = "Inner", Value = 10, IsActive = true }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.MixedTypesClass);

        Assert.Contains("name: Test", yaml);
        Assert.Contains("count: 42", yaml);
        Assert.Contains("ratio: 3.14", yaml);
        Assert.Contains("11111111-1111-1111-1111-111111111111", yaml);
        Assert.Contains("tag1", yaml);
        Assert.Contains("tag2", yaml);
        Assert.Contains("nested:", yaml);
        Assert.Contains("Inner", yaml);
    }

    [Fact]
    public void DeserializeMixedTypesClass()
    {
        var yaml = """
            name: ComplexTest
            count: 100
            ratio: 2.5
            created: 2024-06-15T10:30:00Z
            id: 22222222-2222-2222-2222-222222222222
            tags:
              - alpha
              - beta
            scores:
              x: 50
              y: 75
            nested:
              name: InnerObject
              value: 999
              is-active: true
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.MixedTypesClass);

        Assert.NotNull(obj);
        Assert.Equal("ComplexTest", obj.Name);
        Assert.Equal(100, obj.Count);
        Assert.Equal(2.5, obj.Ratio);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), obj.Id);
        Assert.NotNull(obj.Tags);
        Assert.Equal(2, obj.Tags.Count);
        Assert.Contains("alpha", obj.Tags);
        Assert.NotNull(obj.Scores);
        Assert.Equal(50, obj.Scores["x"]);
        Assert.NotNull(obj.Nested);
        Assert.Equal("InnerObject", obj.Nested.Name);
        Assert.Equal(999, obj.Nested.Value);
    }

    [Fact]
    public void RoundTripMixedTypesClass()
    {
        var original = new MixedTypesClass
        {
            Name = "RoundTrip",
            Count = 123,
            Ratio = 9.99,
            Created = new DateTime(2025, 12, 25, 0, 0, 0, DateTimeKind.Utc),
            Id = Guid.NewGuid(),
            Tags = new List<string> { "test" },
            Scores = new Dictionary<string, int> { ["score"] = 100 },
            Nested = new SimpleClass { Name = "Nested", Value = 1, IsActive = false }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.MixedTypesClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.MixedTypesClass);

        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Count, result.Count);
        Assert.Equal(original.Ratio, result.Ratio);
        Assert.Equal(original.Id, result.Id);
        Assert.NotNull(result.Tags);
        Assert.Equal(original.Tags.Count, result.Tags.Count);
        Assert.NotNull(result.Nested);
        Assert.Equal(original.Nested.Name, result.Nested.Name);
    }

    #endregion

    #region Nested Null Tests

    [Fact]
    public void SerializeNestedNull()
    {
        var obj = new ParentClass
        {
            Title = "Parent",
            Child = null
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.ParentClass);

        Assert.Contains("title: Parent", yaml);
    }

    [Fact]
    public void DeserializeNestedNull()
    {
        var yaml = """
            title: ParentOnly
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ParentClass);

        Assert.NotNull(obj);
        Assert.Equal("ParentOnly", obj.Title);
        Assert.Null(obj.Child);
    }

    [Fact]
    public void RoundTripDeeplyNestedObject()
    {
        var original = new ParentClass
        {
            Title = "Level1",
            Child = new SimpleClass { Name = "Level2", Value = 2, IsActive = true }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.ParentClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ParentClass);

        Assert.NotNull(result);
        Assert.Equal(original.Title, result.Title);
        Assert.NotNull(result.Child);
        Assert.Equal(original.Child.Name, result.Child.Name);
        Assert.Equal(original.Child.Value, result.Child.Value);
        Assert.Equal(original.Child.IsActive, result.Child.IsActive);
    }

    #endregion
}
