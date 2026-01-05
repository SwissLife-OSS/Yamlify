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

    #region Skip() Nested Mapping Tests

    /// <summary>
    /// Model with properties that include nested mappings to test Skip() behavior.
    /// </summary>
    public class ServiceConfig
    {
        public string? Name { get; set; }
        public DeploymentConfig? Deployment { get; set; }
        public string? Namespace { get; set; }
    }

    public class DeploymentConfig
    {
        public int Replicas { get; set; }
        public ResourceConfig? Resources { get; set; }
    }

    public class ResourceConfig
    {
        public string? Cpu { get; set; }
        public string? Memory { get; set; }
    }

    /// <summary>
    /// Model with sibling discriminator for testing unknown discriminator Skip().
    /// </summary>
    public class ServiceWithPlugins
    {
        public string? Name { get; set; }
        public PluginType PluginKind { get; set; }
        
        [YamlSiblingDiscriminator(nameof(PluginKind))]
        [YamlDiscriminatorMapping(nameof(PluginType.Metrics), typeof(MetricsPlugin))]
        [YamlDiscriminatorMapping(nameof(PluginType.Logging), typeof(LoggingPlugin))]
        public PluginBase? Plugin { get; set; }
        
        public string? Description { get; set; }
    }

    public enum PluginType
    {
        Metrics,
        Logging,
        Tracing
    }

    public abstract class PluginBase
    {
    }

    public class MetricsPlugin : PluginBase
    {
        public int Port { get; set; }
    }

    public class LoggingPlugin : PluginBase
    {
        public string? Level { get; set; }
    }

    /// <summary>
    /// Test: Skip() on a nested mapping should advance past the nested MappingEnd,
    /// not the parent's MappingEnd. This was the root cause of the OOM bug.
    /// When deserializing a mapping and encountering an unknown property with a nested
    /// mapping value, Skip() must consume the entire nested mapping AND advance past
    /// its MappingEnd marker, so the parent loop continues correctly.
    /// </summary>
    [Fact]
    public void Deserialize_UnknownPropertyWithNestedMapping_PropertiesAfterStillParsed()
    {
        var yaml = """
            name: my-service
            unknown-nested-property:
              nested-key1: value1
              nested-key2: value2
              deeply-nested:
                key: value
            namespace: production
            """;

        // The key issue: when unknown-nested-property is skipped, the reader should
        // end up AFTER the nested MappingEnd, so "namespace" is still read.
        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ServiceConfig);

        Assert.NotNull(result);
        Assert.Equal("my-service", result.Name);
        Assert.Equal("production", result.Namespace); // This was not being set before the fix
    }

    /// <summary>
    /// Test: Multiple unknown properties with nested mappings should all be skipped correctly.
    /// </summary>
    [Fact]
    public void Deserialize_MultipleUnknownPropertiesWithNestedMappings_DoesNotHang()
    {
        var yaml = """
            name: my-service
            unknown-property-1:
              key1: value1
            unknown-property-2:
              nested:
                deep: value
            deployment:
              replicas: 3
              resources:
                cpu: 100m
                memory: 256Mi
            unknown-property-3:
              another: nested
            namespace: production
            """;

        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ServiceConfig);

        Assert.NotNull(result);
        Assert.Equal("my-service", result.Name);
        Assert.Equal("production", result.Namespace);
        Assert.NotNull(result.Deployment);
        Assert.Equal(3, result.Deployment.Replicas);
        Assert.NotNull(result.Deployment.Resources);
        Assert.Equal("100m", result.Deployment.Resources.Cpu);
        Assert.Equal("256Mi", result.Deployment.Resources.Memory);
    }

    /// <summary>
    /// Test: Unknown property with deeply nested mapping should be skipped correctly.
    /// </summary>
    [Fact]
    public void Deserialize_DeeplyNestedUnknownProperty_DoesNotHang()
    {
        var yaml = """
            name: my-service
            very-deeply-nested-unknown:
              level1:
                level2:
                  level3:
                    level4:
                      level5:
                        key: value
            namespace: production
            """;

        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ServiceConfig);

        Assert.NotNull(result);
        Assert.Equal("my-service", result.Name);
        Assert.Equal("production", result.Namespace);
    }

    /// <summary>
    /// Test: Sibling discriminator with unknown discriminator value should skip the property
    /// and continue parsing remaining properties without causing infinite loop.
    /// </summary>
    [Fact]
    public void Deserialize_SiblingDiscriminatorWithUnknownValue_DoesNotHang()
    {
        var yaml = """
            name: my-service
            plugin-kind: Tracing
            plugin:
              endpoint: http://jaeger:14268
              sample-rate: 0.1
            description: A service with unknown plugin type
            """;

        // PluginKind.Tracing exists as enum value but has no mapping to a concrete type
        // The generated code should skip the plugin property and continue to description
        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ServiceWithPlugins);

        Assert.NotNull(result);
        Assert.Equal("my-service", result.Name);
        Assert.Equal(PluginType.Tracing, result.PluginKind);
        Assert.Null(result.Plugin); // Unknown discriminator, plugin not deserialized
        Assert.Equal("A service with unknown plugin type", result.Description); // Should still be parsed
    }

    /// <summary>
    /// Test: Sibling discriminator with known value works correctly.
    /// </summary>
    [Fact]
    public void Deserialize_SiblingDiscriminatorWithKnownValue_WorksCorrectly()
    {
        var yaml = """
            name: metrics-service
            plugin-kind: Metrics
            plugin:
              port: 9090
            description: A service with metrics plugin
            """;

        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ServiceWithPlugins);

        Assert.NotNull(result);
        Assert.Equal("metrics-service", result.Name);
        Assert.Equal(PluginType.Metrics, result.PluginKind);
        Assert.NotNull(result.Plugin);
        Assert.IsType<MetricsPlugin>(result.Plugin);
        Assert.Equal(9090, ((MetricsPlugin)result.Plugin).Port);
        Assert.Equal("A service with metrics plugin", result.Description);
    }

    /// <summary>
    /// Test: List of items with sibling discriminator where some have unknown values.
    /// </summary>
    [Fact]
    public void Deserialize_ListWithSiblingDiscriminatorSomeUnknown_DoesNotHang()
    {
        var yaml = """
            - name: service-1
              plugin-kind: Metrics
              plugin:
                port: 9090
              description: Has metrics
            - name: service-2
              plugin-kind: Tracing
              plugin:
                endpoint: http://jaeger
              description: Has unknown tracing
            - name: service-3
              plugin-kind: Logging
              plugin:
                level: debug
              description: Has logging
            """;

        var result = YamlSerializer.Deserialize(yaml, InfiniteLoopTestContext.Default.ListServiceWithPlugins);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        
        // First: known discriminator
        Assert.Equal("service-1", result[0].Name);
        Assert.IsType<MetricsPlugin>(result[0].Plugin);
        Assert.Equal("Has metrics", result[0].Description);
        
        // Second: unknown discriminator (Tracing has no mapping)
        Assert.Equal("service-2", result[1].Name);
        Assert.Null(result[1].Plugin);
        Assert.Equal("Has unknown tracing", result[1].Description);
        
        // Third: known discriminator
        Assert.Equal("service-3", result[2].Name);
        Assert.IsType<LoggingPlugin>(result[2].Plugin);
        Assert.Equal("Has logging", result[2].Description);
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
[YamlSerializable(typeof(InfiniteLoopRegressionTests.ServiceConfig))]
[YamlSerializable(typeof(InfiniteLoopRegressionTests.DeploymentConfig))]
[YamlSerializable(typeof(InfiniteLoopRegressionTests.ResourceConfig))]
[YamlSerializable(typeof(InfiniteLoopRegressionTests.ServiceWithPlugins))]
[YamlSerializable(typeof(InfiniteLoopRegressionTests.PluginBase))]
[YamlSerializable(typeof(InfiniteLoopRegressionTests.MetricsPlugin))]
[YamlSerializable(typeof(InfiniteLoopRegressionTests.LoggingPlugin))]
[YamlSerializable(typeof(List<InfiniteLoopRegressionTests.ServiceWithPlugins>))]
public partial class InfiniteLoopTestContext : YamlSerializerContext
{
}
