namespace Yamlify.Tests.TestSuite;

/// <summary>
/// Tests based on the yaml-test-suite-extension.
/// These tests cover edge cases not covered by the official yaml-test-suite,
/// discovered during real-world usage.
/// </summary>
public class YamlTestSuiteExtensionTests
{
    [Fact]
    public void ExtensionTestSuiteCanBeLoaded()
    {
        var testCases = YamlTestSuiteExtensionLoader.GetAllTestCases().ToList();
        Assert.NotEmpty(testCases);
        Assert.True(testCases.Count >= 5, $"Expected at least 5 extension tests, found {testCases.Count}");
    }

    [Theory]
    [MemberData(nameof(GetAllTestCases))]
    public void ParseTest(string testId, string testName)
    {
        var testCase = YamlTestSuiteExtensionLoader.GetTestCaseById(testId);
        Assert.NotNull(testCase);

        if (testCase.Skip)
        {
            return;
        }

        if (testCase.Fail)
        {
            Assert.ThrowsAny<Exception>(() =>
            {
                EventEmitter.EmitEvents(testCase.Yaml);
            });
        }
        else
        {
            var actualEvents = EventEmitter.EmitEvents(testCase.Yaml);
            Assert.NotEmpty(actualEvents);

            if (!string.IsNullOrEmpty(testCase.Tree))
            {
                var differences = EventComparer.GetDifferences(testCase.Tree, actualEvents);
                if (differences.Count > 0)
                {
                    var message = $"Event tree mismatch for {testId} ({testName}):\n" +
                                  $"YAML:\n{testCase.Yaml}\n" +
                                  $"Expected:\n{testCase.Tree}\n" +
                                  $"Actual:\n{actualEvents}\n" +
                                  $"Differences:\n{string.Join("\n", differences)}";
                    Assert.Fail(message);
                }
            }
        }
    }

    public static IEnumerable<object[]> GetAllTestCases()
    {
        return YamlTestSuiteExtensionLoader.GetAllTestCases()
            .Where(tc => !tc.Skip)
            .Select(tc => new object[] { tc.Id, tc.Name });
    }
}
