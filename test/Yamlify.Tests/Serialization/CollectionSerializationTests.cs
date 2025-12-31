using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Class with collection properties.
/// </summary>
public class CollectionsClass
{
    public List<string>? StringList { get; set; }
    public int[]? IntArray { get; set; }
    public Dictionary<string, int>? StringIntDictionary { get; set; }
    public HashSet<string>? StringHashSet { get; set; }
    public IEnumerable<double>? DoubleEnumerable { get; set; }
    public IReadOnlyList<SimpleClass>? ObjectList { get; set; }
}

/// <summary>
/// Class with complex nested collections.
/// </summary>
public class NestedCollectionsClass
{
    public List<List<int>>? Matrix { get; set; }
    public Dictionary<string, List<string>>? TagGroups { get; set; }
    public Dictionary<string, Dictionary<string, int>>? NestedDictionary { get; set; }
}

/// <summary>
/// Tests for serializing and deserializing collections.
/// </summary>
public class CollectionSerializationTests
{
    [Fact]
    public void SerializeStringList()
    {
        var obj = new CollectionsClass
        {
            StringList = new List<string> { "one", "two", "three" }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.CollectionsClass);

        Assert.Contains("string-list:", yaml);
        Assert.Contains("one", yaml);
        Assert.Contains("two", yaml);
        Assert.Contains("three", yaml);
    }

    [Fact]
    public void DeserializeStringList()
    {
        var yaml = """
            string-list:
              - apple
              - banana
              - cherry
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.CollectionsClass);

        Assert.NotNull(obj);
        Assert.NotNull(obj.StringList);
        Assert.Equal(3, obj.StringList.Count);
        Assert.Contains("apple", obj.StringList);
        Assert.Contains("banana", obj.StringList);
        Assert.Contains("cherry", obj.StringList);
    }

    [Fact]
    public void SerializeIntArray()
    {
        var obj = new CollectionsClass
        {
            IntArray = new[] { 1, 2, 3, 4, 5 }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.CollectionsClass);

        Assert.Contains("int-array:", yaml);
        Assert.Contains("1", yaml);
        Assert.Contains("5", yaml);
    }

    [Fact]
    public void DeserializeIntArray()
    {
        var yaml = """
            int-array:
              - 10
              - 20
              - 30
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.CollectionsClass);

        Assert.NotNull(obj);
        Assert.NotNull(obj.IntArray);
        Assert.Equal(3, obj.IntArray.Length);
        Assert.Equal(10, obj.IntArray[0]);
        Assert.Equal(20, obj.IntArray[1]);
        Assert.Equal(30, obj.IntArray[2]);
    }

    [Fact]
    public void SerializeDictionary()
    {
        var obj = new CollectionsClass
        {
            StringIntDictionary = new Dictionary<string, int>
            {
                ["first"] = 1,
                ["second"] = 2
            }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.CollectionsClass);

        Assert.Contains("string-int-dictionary:", yaml);
        Assert.Contains("first:", yaml);
        Assert.Contains("second:", yaml);
    }

    [Fact]
    public void DeserializeDictionary()
    {
        var yaml = """
            string-int-dictionary:
              alpha: 100
              beta: 200
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.CollectionsClass);

        Assert.NotNull(obj);
        Assert.NotNull(obj.StringIntDictionary);
        Assert.Equal(2, obj.StringIntDictionary.Count);
        Assert.Equal(100, obj.StringIntDictionary["alpha"]);
        Assert.Equal(200, obj.StringIntDictionary["beta"]);
    }

    [Fact]
    public void SerializeObjectList()
    {
        var obj = new CollectionsClass
        {
            ObjectList = new List<SimpleClass>
            {
                new() { Name = "Item1", Value = 1, IsActive = true },
                new() { Name = "Item2", Value = 2, IsActive = false }
            }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.CollectionsClass);

        Assert.Contains("object-list:", yaml);
        Assert.Contains("Item1", yaml);
        Assert.Contains("Item2", yaml);
    }

    [Fact]
    public void DeserializeObjectList()
    {
        var yaml = """
            object-list:
              - name: First
                value: 10
                is-active: true
              - name: Second
                value: 20
                is-active: false
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.CollectionsClass);

        Assert.NotNull(obj);
        Assert.NotNull(obj.ObjectList);
        Assert.Equal(2, obj.ObjectList.Count);
        Assert.Equal("First", obj.ObjectList[0].Name);
        Assert.Equal(10, obj.ObjectList[0].Value);
        Assert.Equal("Second", obj.ObjectList[1].Name);
    }

    [Fact]
    public void SerializeNestedCollections()
    {
        var obj = new NestedCollectionsClass
        {
            Matrix = new List<List<int>>
            {
                new() { 1, 2, 3 },
                new() { 4, 5, 6 }
            },
            TagGroups = new Dictionary<string, List<string>>
            {
                ["group1"] = new() { "tag1", "tag2" },
                ["group2"] = new() { "tag3" }
            }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.NestedCollectionsClass);

        Assert.Contains("matrix:", yaml);
        Assert.Contains("tag-groups:", yaml);
        Assert.Contains("group1:", yaml);
        Assert.Contains("tag1", yaml);
    }

    [Fact]
    public void SerializeClassWithCollections()
    {
        var obj = new CollectionsClass
        {
            StringList = new List<string> { "a", "b" },
            IntArray = new[] { 1, 2 },
            StringIntDictionary = new Dictionary<string, int> { ["key"] = 42 }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.CollectionsClass);

        Assert.Contains("string-list:", yaml);
        Assert.Contains("int-array:", yaml);
        Assert.Contains("string-int-dictionary:", yaml);
    }

    [Fact]
    public void SerializeEmptyCollections()
    {
        var obj = new CollectionsClass
        {
            StringList = new List<string>(),
            IntArray = Array.Empty<int>(),
            StringIntDictionary = new Dictionary<string, int>()
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.CollectionsClass);

        // Empty collections should still be present
        Assert.Contains("string-list:", yaml);
        Assert.Contains("int-array:", yaml);
        Assert.Contains("string-int-dictionary:", yaml);
    }

    [Fact]
    public void SerializeHashSet()
    {
        var obj = new CollectionsClass
        {
            StringHashSet = new HashSet<string> { "apple", "banana", "cherry" }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.CollectionsClass);

        Assert.Contains("string-hash-set:", yaml);
        Assert.Contains("apple", yaml);
        Assert.Contains("banana", yaml);
        Assert.Contains("cherry", yaml);
    }

    [Fact]
    public void DeserializeHashSet()
    {
        var yaml = """
            string-hash-set:
              - red
              - green
              - blue
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.CollectionsClass);

        Assert.NotNull(obj);
        Assert.NotNull(obj.StringHashSet);
        Assert.Equal(3, obj.StringHashSet.Count);
        Assert.Contains("red", obj.StringHashSet);
        Assert.Contains("green", obj.StringHashSet);
        Assert.Contains("blue", obj.StringHashSet);
    }

    [Fact]
    public void DeserializeNestedCollections()
    {
        var yaml = """
            matrix:
              - - 1
                - 2
                - 3
              - - 4
                - 5
                - 6
            tag-groups:
              group1:
                - tag1
                - tag2
              group2:
                - tag3
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.NestedCollectionsClass);

        Assert.NotNull(obj);
        Assert.NotNull(obj.Matrix);
        Assert.Equal(2, obj.Matrix.Count);
        Assert.Equal(3, obj.Matrix[0].Count);
        Assert.Equal(1, obj.Matrix[0][0]);
        Assert.Equal(6, obj.Matrix[1][2]);
        
        Assert.NotNull(obj.TagGroups);
        Assert.Equal(2, obj.TagGroups.Count);
        Assert.Equal(2, obj.TagGroups["group1"].Count);
        Assert.Contains("tag1", obj.TagGroups["group1"]);
    }

    [Fact]
    public void SerializeReadOnlyDictionary()
    {
        var obj = new ReadOnlyCollectionsClass
        {
            ReadOnlyDict = new Dictionary<string, int>
            {
                ["alpha"] = 1,
                ["beta"] = 2
            }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.ReadOnlyCollectionsClass);

        Assert.Contains("read-only-dict:", yaml);
        Assert.Contains("alpha:", yaml);
        Assert.Contains("beta:", yaml);
    }

    [Fact]
    public void DeserializeReadOnlyDictionary()
    {
        var yaml = """
            read-only-dict:
              one: 100
              two: 200
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ReadOnlyCollectionsClass);

        Assert.NotNull(obj);
        Assert.NotNull(obj.ReadOnlyDict);
        Assert.Equal(2, obj.ReadOnlyDict.Count);
        Assert.Equal(100, obj.ReadOnlyDict["one"]);
        Assert.Equal(200, obj.ReadOnlyDict["two"]);
    }

    [Fact]
    public void SerializeReadOnlyList()
    {
        var obj = new ReadOnlyCollectionsClass
        {
            ReadOnlyList = new List<string> { "first", "second", "third" }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.ReadOnlyCollectionsClass);

        Assert.Contains("read-only-list:", yaml);
        Assert.Contains("first", yaml);
        Assert.Contains("second", yaml);
        Assert.Contains("third", yaml);
    }

    [Fact]
    public void DeserializeReadOnlyList()
    {
        var yaml = """
            read-only-list:
              - item1
              - item2
              - item3
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ReadOnlyCollectionsClass);

        Assert.NotNull(obj);
        Assert.NotNull(obj.ReadOnlyList);
        Assert.Equal(3, obj.ReadOnlyList.Count);
        Assert.Equal("item1", obj.ReadOnlyList[0]);
        Assert.Equal("item2", obj.ReadOnlyList[1]);
        Assert.Equal("item3", obj.ReadOnlyList[2]);
    }

    [Fact]
    public void SerializeDictionaryWithObjectValues()
    {
        var obj = new ReadOnlyCollectionsClass
        {
            ObjectDict = new Dictionary<string, SimpleClass>
            {
                ["key1"] = new SimpleClass { Name = "First", Value = 10, IsActive = true },
                ["key2"] = new SimpleClass { Name = "Second", Value = 20, IsActive = false }
            }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.ReadOnlyCollectionsClass);

        Assert.Contains("object-dict:", yaml);
        Assert.Contains("key1:", yaml);
        Assert.Contains("First", yaml);
        Assert.Contains("key2:", yaml);
        Assert.Contains("Second", yaml);
    }

    [Fact]
    public void DeserializeDictionaryWithObjectValues()
    {
        var yaml = """
            object-dict:
              item1:
                name: Object1
                value: 100
                is-active: true
              item2:
                name: Object2
                value: 200
                is-active: false
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ReadOnlyCollectionsClass);

        Assert.NotNull(obj);
        Assert.NotNull(obj.ObjectDict);
        Assert.Equal(2, obj.ObjectDict.Count);
        Assert.Equal("Object1", obj.ObjectDict["item1"].Name);
        Assert.Equal(100, obj.ObjectDict["item1"].Value);
        Assert.Equal("Object2", obj.ObjectDict["item2"].Name);
    }

    [Fact]
    public void RoundTripReadOnlyCollections()
    {
        var original = new ReadOnlyCollectionsClass
        {
            ReadOnlyDict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 },
            ReadOnlyList = new List<string> { "x", "y", "z" }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.ReadOnlyCollectionsClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ReadOnlyCollectionsClass);

        Assert.NotNull(result);
        Assert.NotNull(result.ReadOnlyDict);
        Assert.NotNull(result.ReadOnlyList);
        Assert.Equal(original.ReadOnlyDict.Count, result.ReadOnlyDict.Count);
        Assert.Equal(original.ReadOnlyList.Count, result.ReadOnlyList.Count);
    }

    [Fact]
    public void RoundTripNestedCollections()
    {
        var original = new NestedCollectionsClass
        {
            Matrix = new List<List<int>>
            {
                new() { 1, 2, 3 },
                new() { 4, 5, 6 },
                new() { 7, 8, 9 }
            },
            TagGroups = new Dictionary<string, List<string>>
            {
                ["colors"] = new() { "red", "green", "blue" },
                ["sizes"] = new() { "small", "medium", "large" }
            }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.NestedCollectionsClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.NestedCollectionsClass);

        Assert.NotNull(result);
        Assert.NotNull(result.Matrix);
        Assert.Equal(original.Matrix.Count, result.Matrix.Count);
        Assert.Equal(original.Matrix[0][0], result.Matrix[0][0]);
        Assert.Equal(original.Matrix[2][2], result.Matrix[2][2]);
        
        Assert.NotNull(result.TagGroups);
        Assert.Equal(original.TagGroups.Count, result.TagGroups.Count);
        Assert.Contains("red", result.TagGroups["colors"]);
    }

    [Fact]
    public void RoundTripCollectionsClass()
    {
        var original = new CollectionsClass
        {
            StringList = new List<string> { "one", "two", "three" },
            IntArray = new[] { 10, 20, 30 },
            StringIntDictionary = new Dictionary<string, int> { ["key"] = 42 },
            StringHashSet = new HashSet<string> { "a", "b", "c" }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.CollectionsClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.CollectionsClass);

        Assert.NotNull(result);
        Assert.NotNull(result.StringList);
        Assert.Equal(original.StringList.Count, result.StringList.Count);
        Assert.NotNull(result.IntArray);
        Assert.Equal(original.IntArray.Length, result.IntArray.Length);
        Assert.NotNull(result.StringIntDictionary);
        Assert.Equal(original.StringIntDictionary["key"], result.StringIntDictionary["key"]);
        Assert.NotNull(result.StringHashSet);
        Assert.Equal(original.StringHashSet.Count, result.StringHashSet.Count);
    }

    [Fact]
    public void RoundTripDictionaryWithObjectValues()
    {
        var original = new ReadOnlyCollectionsClass
        {
            ObjectDict = new Dictionary<string, SimpleClass>
            {
                ["obj1"] = new SimpleClass { Name = "First", Value = 100, IsActive = true },
                ["obj2"] = new SimpleClass { Name = "Second", Value = 200, IsActive = false }
            }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.ReadOnlyCollectionsClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ReadOnlyCollectionsClass);

        Assert.NotNull(result);
        Assert.NotNull(result.ObjectDict);
        Assert.Equal(2, result.ObjectDict.Count);
        Assert.Equal("First", result.ObjectDict["obj1"].Name);
        Assert.Equal(100, result.ObjectDict["obj1"].Value);
        Assert.Equal("Second", result.ObjectDict["obj2"].Name);
    }

    #region Dictionary with Enum Keys

    [Fact]
    public void SerializeDictionaryWithEnumKeys()
    {
        var obj = new DictionaryWithEnumKeyClass
        {
            PriorityLabels = new Dictionary<Priority, string>
            {
                [Priority.Low] = "low-priority",
                [Priority.High] = "high-priority"
            }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.DictionaryWithEnumKeyClass);

        Assert.Contains("priority-labels:", yaml);
        Assert.Contains("Low:", yaml);
        Assert.Contains("High:", yaml);
        Assert.Contains("low-priority", yaml);
        Assert.Contains("high-priority", yaml);
    }

    [Fact]
    public void DeserializeDictionaryWithEnumKeys()
    {
        var yaml = """
            priority-labels:
              Low: low-priority
              Medium: medium-priority
              High: high-priority
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.DictionaryWithEnumKeyClass);

        Assert.NotNull(result?.PriorityLabels);
        Assert.Equal(3, result.PriorityLabels.Count);
        Assert.Equal("low-priority", result.PriorityLabels[Priority.Low]);
        Assert.Equal("medium-priority", result.PriorityLabels[Priority.Medium]);
        Assert.Equal("high-priority", result.PriorityLabels[Priority.High]);
    }

    [Fact]
    public void RoundTripDictionaryWithEnumKeys()
    {
        var original = new DictionaryWithEnumKeyClass
        {
            PriorityLabels = new Dictionary<Priority, string>
            {
                [Priority.Low] = "not-urgent",
                [Priority.Critical] = "urgent"
            },
            StatusCounts = new Dictionary<Status, int>
            {
                [Status.Active] = 10,
                [Status.Pending] = 5
            }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.DictionaryWithEnumKeyClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.DictionaryWithEnumKeyClass);

        Assert.NotNull(result?.PriorityLabels);
        Assert.Equal(2, result.PriorityLabels.Count);
        Assert.Equal("not-urgent", result.PriorityLabels[Priority.Low]);
        Assert.NotNull(result.StatusCounts);
        Assert.Equal(10, result.StatusCounts[Status.Active]);
    }

    #endregion

    #region Dictionary with Array Values

    [Fact]
    public void SerializeDictionaryWithArrayValues()
    {
        var obj = new DictionaryWithArrayValueClass
        {
            TagsByCategory = new Dictionary<string, string[]>
            {
                ["colors"] = ["red", "green", "blue"],
                ["sizes"] = ["small", "large"]
            }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.DictionaryWithArrayValueClass);

        Assert.Contains("tags-by-category:", yaml);
        Assert.Contains("colors:", yaml);
        Assert.Contains("red", yaml);
        Assert.Contains("sizes:", yaml);
    }

    [Fact]
    public void DeserializeDictionaryWithArrayValues()
    {
        var yaml = """
            tags-by-category:
              fruits:
                - apple
                - banana
              vegetables:
                - carrot
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.DictionaryWithArrayValueClass);

        Assert.NotNull(result?.TagsByCategory);
        Assert.Equal(2, result.TagsByCategory.Count);
        Assert.Equal(2, result.TagsByCategory["fruits"].Length);
        Assert.Equal("apple", result.TagsByCategory["fruits"][0]);
        Assert.Single(result.TagsByCategory["vegetables"]);
    }

    [Fact]
    public void SerializeDictionaryWithEnumKeysAndArrayValues()
    {
        var obj = new DictionaryWithArrayValueClass
        {
            ItemsByPriority = new Dictionary<Priority, ItemInfo[]>
            {
                [Priority.High] = [new ItemInfo { Name = "item-a", Count = 1 }],
                [Priority.Low] = [new ItemInfo { Name = "item-b", Count = 2 }, new ItemInfo { Name = "item-c", Count = 3 }]
            }
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.DictionaryWithArrayValueClass);

        Assert.Contains("items-by-priority:", yaml);
        Assert.Contains("High:", yaml);
        Assert.Contains("Low:", yaml);
        Assert.Contains("name: item-a", yaml);
    }

    [Fact]
    public void DeserializeDictionaryWithEnumKeysAndArrayValues()
    {
        var yaml = """
            items-by-priority:
              High:
                - name: urgent-item
                  count: 10
              Low:
                - name: normal-item
                  count: 5
                - name: another-item
                  count: 3
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.DictionaryWithArrayValueClass);

        Assert.NotNull(result?.ItemsByPriority);
        Assert.Equal(2, result.ItemsByPriority.Count);
        Assert.Single(result.ItemsByPriority[Priority.High]);
        Assert.Equal("urgent-item", result.ItemsByPriority[Priority.High][0].Name);
        Assert.Equal(2, result.ItemsByPriority[Priority.Low].Length);
    }

    [Fact]
    public void RoundTripDictionaryWithArrayValues()
    {
        var original = new DictionaryWithArrayValueClass
        {
            TagsByCategory = new Dictionary<string, string[]>
            {
                ["group1"] = ["a", "b", "c"],
                ["group2"] = ["x", "y"]
            },
            ItemsByPriority = new Dictionary<Priority, ItemInfo[]>
            {
                [Priority.Critical] = [new ItemInfo { Name = "critical-item", Count = 100 }]
            }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.DictionaryWithArrayValueClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.DictionaryWithArrayValueClass);

        Assert.NotNull(result?.TagsByCategory);
        Assert.Equal(3, result.TagsByCategory["group1"].Length);
        Assert.NotNull(result.ItemsByPriority);
        Assert.Equal("critical-item", result.ItemsByPriority[Priority.Critical][0].Name);
    }

    #endregion

    #region Root-Level Collection Types

    [Fact]
    public void SerializeRootLevelList()
    {
        var list = new List<SimpleClass>
        {
            new() { Name = "Item1", Value = 10, IsActive = true },
            new() { Name = "Item2", Value = 20, IsActive = false }
        };

        var yaml = YamlSerializer.Serialize(list, TestSerializerContext.Default.ListSimpleClass);

        Assert.Contains("name: Item1", yaml);
        Assert.Contains("value: 10", yaml);
        Assert.Contains("name: Item2", yaml);
        Assert.Contains("value: 20", yaml);
    }

    [Fact]
    public void DeserializeRootLevelList()
    {
        var yaml = """
            - name: First
              value: 100
              is-active: true
            - name: Second
              value: 200
              is-active: false
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ListSimpleClass);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("First", result[0].Name);
        Assert.Equal(100, result[0].Value);
        Assert.True(result[0].IsActive);
        Assert.Equal("Second", result[1].Name);
        Assert.Equal(200, result[1].Value);
        Assert.False(result[1].IsActive);
    }

    [Fact]
    public void RoundTripRootLevelList()
    {
        var original = new List<SimpleClass>
        {
            new() { Name = "Alpha", Value = 1, IsActive = true },
            new() { Name = "Beta", Value = 2, IsActive = false },
            new() { Name = "Gamma", Value = 3, IsActive = true }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.ListSimpleClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ListSimpleClass);

        Assert.NotNull(result);
        Assert.Equal(original.Count, result.Count);
        for (int i = 0; i < original.Count; i++)
        {
            Assert.Equal(original[i].Name, result[i].Name);
            Assert.Equal(original[i].Value, result[i].Value);
            Assert.Equal(original[i].IsActive, result[i].IsActive);
        }
    }

    [Fact]
    public void SerializeRootLevelDictionary()
    {
        var dict = new Dictionary<string, SimpleClass>
        {
            ["first"] = new() { Name = "First", Value = 1, IsActive = true },
            ["second"] = new() { Name = "Second", Value = 2, IsActive = false }
        };

        var yaml = YamlSerializer.Serialize(dict, TestSerializerContext.Default.DictionaryStringSimpleClass);

        Assert.Contains("first:", yaml);
        Assert.Contains("name: First", yaml);
        Assert.Contains("second:", yaml);
        Assert.Contains("name: Second", yaml);
    }

    [Fact]
    public void DeserializeRootLevelDictionary()
    {
        var yaml = """
            alpha:
              name: Alpha Item
              value: 50
              is-active: true
            beta:
              name: Beta Item
              value: 60
              is-active: false
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.DictionaryStringSimpleClass);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha Item", result["alpha"].Name);
        Assert.Equal(50, result["alpha"].Value);
        Assert.Equal("Beta Item", result["beta"].Name);
        Assert.Equal(60, result["beta"].Value);
    }

    [Fact]
    public void RoundTripRootLevelDictionary()
    {
        var original = new Dictionary<string, SimpleClass>
        {
            ["x"] = new() { Name = "X", Value = 10, IsActive = true },
            ["y"] = new() { Name = "Y", Value = 20, IsActive = false },
            ["z"] = new() { Name = "Z", Value = 30, IsActive = true }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.DictionaryStringSimpleClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.DictionaryStringSimpleClass);

        Assert.NotNull(result);
        Assert.Equal(original.Count, result.Count);
        foreach (var key in original.Keys)
        {
            Assert.Equal(original[key].Name, result[key].Name);
            Assert.Equal(original[key].Value, result[key].Value);
            Assert.Equal(original[key].IsActive, result[key].IsActive);
        }
    }

    [Fact]
    public void DeserializeEmptyRootLevelList()
    {
        var yaml = "[]";

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ListSimpleClass);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void DeserializeEmptyRootLevelDictionary()
    {
        var yaml = "{}";

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.DictionaryStringSimpleClass);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion
}
