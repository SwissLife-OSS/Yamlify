using Yamlify.Serialization;

namespace Yamlify.Tests.TestSuite;

/// <summary>
/// Loader for yaml-test-suite-extension test cases.
/// These are additional tests that cover edge cases not in the official yaml-test-suite.
/// </summary>
public static class YamlTestSuiteExtensionLoader
{
    private static readonly YamlSerializerOptions SerializerOptions = new()
    {
        TypeInfoResolver = new TestSuiteSerializerContext()
    };

    /// <summary>
    /// Gets all test cases from the extension test suite.
    /// </summary>
    public static IEnumerable<YamlTestCase> GetAllTestCases()
    {
        var testSuiteDir = FindTestSuiteDirectory();
        if (testSuiteDir == null)
        {
            yield break;
        }

        foreach (var file in Directory.GetFiles(testSuiteDir, "*.yaml"))
        {
            var testCases = LoadTestCasesFromFile(file);
            foreach (var testCase in testCases)
            {
                yield return testCase;
            }
        }
    }

    /// <summary>
    /// Gets a specific test case by ID.
    /// </summary>
    public static YamlTestCase? GetTestCaseById(string id)
    {
        return GetAllTestCases().FirstOrDefault(tc => tc.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    private static string? FindTestSuiteDirectory()
    {
        // Look for TestSuiteExtensionData directory in output
        var outputDir = AppContext.BaseDirectory;
        var testSuiteDir = Path.Combine(outputDir, "TestSuiteExtensionData");
        if (Directory.Exists(testSuiteDir))
        {
            return testSuiteDir;
        }

        // Look in parent directories for the extension folder
        var current = new DirectoryInfo(outputDir);
        while (current != null)
        {
            var extensionDir = Path.Combine(current.FullName, "test", "Yamlify.Tests", "yaml-test-suite-extension", "src");
            if (Directory.Exists(extensionDir))
            {
                return extensionDir;
            }
            current = current.Parent;
        }

        return null;
    }

    private static IEnumerable<YamlTestCase> LoadTestCasesFromFile(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var content = File.ReadAllText(filePath);
        
        List<YamlTestCaseRaw>? testCases;
        try
        {
            testCases = YamlSerializer.Deserialize<List<YamlTestCaseRaw>>(content, SerializerOptions);
        }
        catch
        {
            // If deserialization fails, skip this file
            yield break;
        }
        
        if (testCases == null)
        {
            yield break;
        }

        foreach (var raw in testCases)
        {
            yield return new YamlTestCase
            {
                Id = fileName,
                Name = raw.Name ?? fileName,
                From = raw.From ?? "",
                Tags = raw.Tags ?? "",
                Yaml = raw.Yaml ?? "",
                Tree = raw.Tree ?? "",
                Json = raw.Json,
                Dump = raw.Dump,
                Fail = raw.Fail,
                Skip = raw.Skip
            };
        }
    }
}
