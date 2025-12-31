using Yamlify.Serialization;

namespace Yamlify.Tests.TestSuite;

/// <summary>
/// Serializer context for loading yaml-test-suite test case files.
/// </summary>
[YamlSerializable(typeof(List<YamlTestCaseRaw>))]
[YamlSerializable(typeof(YamlTestCaseRaw))]
public partial class TestSuiteSerializerContext : YamlSerializerContext
{
}

/// <summary>
/// Raw test case from yaml-test-suite files.
/// </summary>
public class YamlTestCaseRaw
{
    [YamlPropertyName("name")]
    public string? Name { get; set; }

    [YamlPropertyName("from")]
    public string? From { get; set; }

    [YamlPropertyName("tags")]
    public string? Tags { get; set; }

    [YamlPropertyName("yaml")]
    public string? Yaml { get; set; }

    [YamlPropertyName("tree")]
    public string? Tree { get; set; }

    [YamlPropertyName("json")]
    public string? Json { get; set; }

    [YamlPropertyName("dump")]
    public string? Dump { get; set; }

    [YamlPropertyName("fail")]
    public bool Fail { get; set; }

    [YamlPropertyName("skip")]
    public bool Skip { get; set; }
}
