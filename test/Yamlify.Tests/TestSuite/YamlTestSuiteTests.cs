namespace Yamlify.Tests.TestSuite;

/// <summary>
/// Tests based on the yaml-test-suite.
/// Provides comprehensive YAML 1.2 specification compliance testing.
/// </summary>
public class YamlTestSuiteTests
{
    [Fact]
    public void TestSuiteCanBeLoaded()
    {
        var testCases = YamlTestSuiteLoader.GetAllTestCases().ToList();
        Assert.NotEmpty(testCases);
    }

    [Theory]
    [MemberData(nameof(GetAllTestCases))]
    public void ParseTest(string testId, string testName)
    {
        var testCase = YamlTestSuiteLoader.GetTestCaseById(testId);
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
                    var message = $"Event tree mismatch for {testId}:\n" +
                                  $"Expected:\n{testCase.Tree}\n" +
                                  $"Actual:\n{actualEvents}\n" +
                                  $"Differences:\n{string.Join("\n", differences)}";
                }
            }
        }
    }

    public static IEnumerable<object[]> GetAllTestCases()
    {
        return YamlTestSuiteLoader.GetAllTestCases()
            .Where(tc => !tc.Skip)
            .Select(tc => new object[] { tc.Id, tc.Name });
    }
}
