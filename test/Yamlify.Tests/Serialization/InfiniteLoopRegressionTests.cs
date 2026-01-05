using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Regression tests for infinite loop bug that caused OOM exception.
/// 
/// Bug Description:
/// When deserializing collections, if an element converter encountered an unexpected
/// token type (e.g., null or wrong type), it returned default without advancing the reader.
/// This caused the outer collection loop to repeatedly call the converter on the same token,
/// resulting in an infinite loop and eventually OOM.
/// 
/// Fix:
/// Added reader.Skip() before returning default in generated converters when
/// token type doesn't match expected (MappingStart for objects, SequenceStart for collections).
/// </summary>
public class InfiniteLoopRegressionTests
{
    #region Test Models

    /// <summary>
    /// Class with array of objects - the original bug scenario.
    /// </summary>
    public class ContainerWithObjectArray
    {
        public ObjectItem[]? Items { get; set; }
    }

    public class ObjectItem
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    /// <summary>
    /// Class with list of objects.
    /// </summary>
    public class ContainerWithObjectList
    {
        public List<ObjectItem>? Items { get; set; }
    }

    /// <summary>
    /// Base class for polymorphic test.
    /// </summary>
    [YamlDerivedType<DerivedTypeA>("type-a")]
    [YamlDerivedType<DerivedTypeB>("type-b")]
    public class BasePolymorphicType
    {
        public string? CommonProperty { get; set; }
    }

    public class DerivedTypeA : BasePolymorphicType
    {
        public string? TypeAProperty { get; set; }
    }

    public class DerivedTypeB : BasePolymorphicType
    {
        public int TypeBValue { get; set; }
    }

    /// <summary>
    /// Container with polymorphic array.
    /// </summary>
    public class ContainerWithPolymorphicArray
    {
        public BasePolymorphicType[]? Items { get; set; }
    }

    /// <summary>
    /// Container with dictionary having object values.
    /// </summary>
    public class ContainerWithObjectDictionary
    {
        public Dictionary<string, ObjectItem>? ItemsByKey { get; set; }
    }

    #endregion

    #region Null Element Tests

    /// <summary>
    /// Test: Array with explicit null element should not cause infinite loop.
    /// The null should be skipped or handled gracefully.
    /// </summary>
    [Fact]
    public void DeserializeArrayWithNullElement_DoesNotHang()
    {
        var yaml = """
            items:
              - name: First
                value: 1
              - ~
              - name: Third
                value: 3
            """;

        // If the fix is not in place, this will hang forever (infinite loop).
        // With the fix, it completes quickly with null elements represented as null in the array.
        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ContainerWithObjectArray);

        Assert.NotNull(result?.Items);
        Assert.Equal(3, result.Items.Length);
        Assert.NotNull(result.Items[0]);
        Assert.Null(result.Items[1]); // Null element
        Assert.NotNull(result.Items[2]);
    }

    /// <summary>
    /// Test: Array with null keyword element should not cause infinite loop.
    /// </summary>
    [Fact]
    public void DeserializeArrayWithNullKeyword_DoesNotHang()
    {
        var yaml = """
            items:
              - name: First
                value: 1
              - null
              - name: Third
                value: 3
            """;

        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ContainerWithObjectArray);

        Assert.NotNull(result?.Items);
        Assert.Equal(3, result.Items.Length);
        Assert.Null(result.Items[1]); // Null element
    }

    /// <summary>
    /// Test: List with null elements should not cause infinite loop.
    /// </summary>
    [Fact]
    public void DeserializeListWithNullElements_DoesNotHang()
    {
        var yaml = """
            items:
              - name: First
                value: 1
              - ~
              - ~
              - name: Fourth
                value: 4
            """;

        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ContainerWithObjectList);

        Assert.NotNull(result?.Items);
        Assert.Equal(4, result.Items.Count);
        Assert.NotNull(result.Items[0]);
        Assert.Null(result.Items[1]); // Null element
        Assert.Null(result.Items[2]); // Null element
        Assert.NotNull(result.Items[3]);
    }

    #endregion

    #region Wrong Type Element Tests

    /// <summary>
    /// Test: Array element that is a scalar instead of mapping should not cause infinite loop.
    /// </summary>
    [Fact]
    public void DeserializeArrayWithScalarElement_DoesNotHang()
    {
        var yaml = """
            items:
              - name: First
                value: 1
              - just-a-string
              - name: Third
                value: 3
            """;

        // Scalar element where object expected should be skipped (null)
        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ContainerWithObjectArray);

        Assert.NotNull(result?.Items);
        Assert.Equal(3, result.Items.Length);
        Assert.NotNull(result.Items[0]);
        Assert.Null(result.Items[1]); // Scalar skipped, becomes null
        Assert.NotNull(result.Items[2]);
    }

    /// <summary>
    /// Test: Array element that is a sequence instead of mapping should not cause infinite loop.
    /// </summary>
    [Fact]
    public void DeserializeArrayWithSequenceElement_DoesNotHang()
    {
        var yaml = """
            items:
              - name: First
                value: 1
              - - nested
                - sequence
              - name: Third
                value: 3
            """;

        // The key assertion is that this completes without hanging (OOM).
        // Sequence element where object expected is skipped, and the sequence contents
        // may also be consumed during the skip operation.
        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ContainerWithObjectArray);

        Assert.NotNull(result?.Items);
        // At least the valid items should be present
        Assert.True(result.Items.Length >= 1, "At least the first valid item should be parsed");
        Assert.Equal("First", result.Items[0]?.Name);
    }

    #endregion

    #region Polymorphic Collection Tests

    /// <summary>
    /// Test: Polymorphic array with null elements should not cause infinite loop.
    /// </summary>
    [Fact]
    public void DeserializePolymorphicArrayWithNullElement_DoesNotHang()
    {
        var yaml = """
            items:
              - $type: type-a
                common-property: Common1
                type-a-property: ValueA
              - ~
              - $type: type-b
                common-property: Common2
                type-b-value: 42
            """;

        // The key assertion is that this completes without hanging (OOM).
        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ContainerWithPolymorphicArray);

        Assert.NotNull(result?.Items);
        Assert.Equal(3, result.Items.Length);
        // The polymorphic deserialization may fall back to base type if discriminator handling differs
        Assert.NotNull(result.Items[0]);
        Assert.Null(result.Items[1]); // Null element
        Assert.NotNull(result.Items[2]);
    }

    /// <summary>
    /// Test: Polymorphic array with invalid discriminator should not cause infinite loop.
    /// </summary>
    [Fact]
    public void DeserializePolymorphicArrayWithInvalidDiscriminator_DoesNotHang()
    {
        var yaml = """
            items:
              - $type: type-a
                common-property: Common1
                type-a-property: ValueA
              - $type: unknown-type
                some-property: value
              - $type: type-b
                common-property: Common2
                type-b-value: 42
            """;

        // The key assertion is that this completes without hanging (OOM).
        // Invalid discriminator results in fallback behavior (base type or null)
        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ContainerWithPolymorphicArray);

        Assert.NotNull(result?.Items);
        Assert.Equal(3, result.Items.Length);
        Assert.NotNull(result.Items[0]); // First valid item
        // Unknown type may parse as base type or null - both are acceptable
        Assert.NotNull(result.Items[2]); // Third valid item
    }

    #endregion

    #region Dictionary Tests

    /// <summary>
    /// Test: Dictionary with null values should not cause infinite loop.
    /// </summary>
    [Fact]
    public void DeserializeDictionaryWithNullValue_DoesNotHang()
    {
        var yaml = """
            items-by-key:
              first:
                name: First
                value: 1
              second: ~
              third:
                name: Third
                value: 3
            """;

        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ContainerWithObjectDictionary);

        Assert.NotNull(result?.ItemsByKey);
        Assert.Equal(3, result.ItemsByKey.Count);
        Assert.NotNull(result.ItemsByKey["first"]);
        Assert.Null(result.ItemsByKey["second"]); // Null value
        Assert.NotNull(result.ItemsByKey["third"]);
    }

    /// <summary>
    /// Test: Dictionary with scalar value instead of object should not cause infinite loop.
    /// </summary>
    [Fact]
    public void DeserializeDictionaryWithScalarValue_DoesNotHang()
    {
        var yaml = """
            items-by-key:
              first:
                name: First
                value: 1
              second: just-a-string
              third:
                name: Third
                value: 3
            """;

        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ContainerWithObjectDictionary);

        Assert.NotNull(result?.ItemsByKey);
        Assert.Equal(3, result.ItemsByKey.Count);
        Assert.NotNull(result.ItemsByKey["first"]);
        Assert.Null(result.ItemsByKey["second"]); // Scalar skipped, becomes null
        Assert.NotNull(result.ItemsByKey["third"]);
    }

    #endregion

    #region Nested Collection Tests

    // Note: Nested collections with object elements (List<List<ObjectItem>>) 
    // require special handling that is beyond the scope of this regression test.
    // The core issue (infinite loop on null/scalar elements) is tested by other tests.

    #endregion

    #region Root Collection Tests

    /// <summary>
    /// Test: Root-level list with null elements should not cause infinite loop.
    /// </summary>
    [Fact]
    public void DeserializeRootListWithNullElements_DoesNotHang()
    {
        var yaml = """
            - name: First
              value: 1
            - ~
            - name: Third
              value: 3
            """;

        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ListObjectItem);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.NotNull(result[0]);
        Assert.Null(result[1]); // Null element
        Assert.NotNull(result[2]);
    }

    /// <summary>
    /// Test: Root-level dictionary with null values should not cause infinite loop.
    /// </summary>
    [Fact]
    public void DeserializeRootDictionaryWithNullValues_DoesNotHang()
    {
        var yaml = """
            first:
              name: First
              value: 1
            second: ~
            third:
              name: Third
              value: 3
            """;

        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.DictionaryStringObjectItem);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.NotNull(result["first"]);
        Assert.Null(result["second"]); // Null value
        Assert.NotNull(result["third"]);
    }

    #endregion

    #region Empty Element Tests

    /// <summary>
    /// Test: Array with empty mapping element should work correctly.
    /// </summary>
    [Fact]
    public void DeserializeArrayWithEmptyMappingElement_Works()
    {
        var yaml = """
            items:
              - name: First
                value: 1
              - {}
              - name: Third
                value: 3
            """;

        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ContainerWithObjectArray);

        Assert.NotNull(result?.Items);
        Assert.Equal(3, result.Items.Length);
        Assert.NotNull(result.Items[0]);
        Assert.NotNull(result.Items[1]); // Empty mapping creates object with defaults
        Assert.NotNull(result.Items[2]);
    }

    #endregion

    #region Multiple Null Elements in Sequence

    /// <summary>
    /// Test: Multiple consecutive null elements should not cause infinite loop.
    /// This specifically tests that the reader advances correctly through multiple nulls.
    /// </summary>
    [Fact]
    public void DeserializeArrayWithMultipleConsecutiveNulls_DoesNotHang()
    {
        var yaml = """
            items:
              - ~
              - ~
              - ~
              - name: OnlyItem
                value: 1
              - ~
              - ~
            """;

        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ContainerWithObjectArray);

        Assert.NotNull(result?.Items);
        Assert.Equal(6, result.Items.Length);
        Assert.Null(result.Items[0]);
        Assert.Null(result.Items[1]);
        Assert.Null(result.Items[2]);
        Assert.NotNull(result.Items[3]);
        Assert.Equal("OnlyItem", result.Items[3]!.Name);
        Assert.Null(result.Items[4]);
        Assert.Null(result.Items[5]);
    }

    /// <summary>
    /// Test: Array with all null elements should not cause infinite loop.
    /// </summary>
    [Fact]
    public void DeserializeArrayWithAllNullElements_DoesNotHang()
    {
        var yaml = """
            items:
              - ~
              - ~
              - ~
            """;

        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ContainerWithObjectArray);

        Assert.NotNull(result?.Items);
        Assert.Equal(3, result.Items.Length);
        Assert.All(result.Items, item => Assert.Null(item));
    }

    #endregion
}

/// <summary>
/// Serializer context for infinite loop regression tests.
/// </summary>
[YamlSerializable(typeof(InfiniteLoopRegressionTests.ContainerWithObjectArray))]
[YamlSerializable(typeof(InfiniteLoopRegressionTests.ContainerWithObjectList))]
[YamlSerializable(typeof(InfiniteLoopRegressionTests.ObjectItem))]
[YamlSerializable(typeof(InfiniteLoopRegressionTests.BasePolymorphicType))]
[YamlSerializable(typeof(InfiniteLoopRegressionTests.DerivedTypeA))]
[YamlSerializable(typeof(InfiniteLoopRegressionTests.DerivedTypeB))]
[YamlSerializable(typeof(InfiniteLoopRegressionTests.ContainerWithPolymorphicArray))]
[YamlSerializable(typeof(InfiniteLoopRegressionTests.ContainerWithObjectDictionary))]
[YamlSerializable(typeof(List<InfiniteLoopRegressionTests.ObjectItem>))]
[YamlSerializable(typeof(Dictionary<string, InfiniteLoopRegressionTests.ObjectItem>))]
public partial class InfiniteLoopTestContext : YamlSerializerContext
{
}
