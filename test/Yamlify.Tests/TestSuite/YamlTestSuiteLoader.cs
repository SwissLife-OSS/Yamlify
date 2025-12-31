using Yamlify.Serialization;

namespace Yamlify.Tests.TestSuite;

/// <summary>
/// Represents a single test case from the yaml-test-suite.
/// </summary>
public class YamlTestCase
{
    /// <summary>
    /// The test case ID (e.g., "229Q").
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// The name of the test case.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// The source of this test (e.g., spec URL).
    /// </summary>
    public string From { get; set; } = "";

    /// <summary>
    /// Tags for categorizing the test.
    /// </summary>
    public string Tags { get; set; } = "";

    /// <summary>
    /// The YAML input to parse.
    /// </summary>
    public string Yaml { get; set; } = "";

    /// <summary>
    /// The expected event tree (parse events in DSL format).
    /// </summary>
    public string Tree { get; set; } = "";

    /// <summary>
    /// The expected JSON equivalent.
    /// </summary>
    public string? Json { get; set; }

    /// <summary>
    /// The expected round-trip output.
    /// </summary>
    public string? Dump { get; set; }

    /// <summary>
    /// Whether this is an error test (should fail to parse).
    /// </summary>
    public bool Fail { get; set; }

    /// <summary>
    /// Whether to skip this test.
    /// </summary>
    public bool Skip { get; set; }

    public override string ToString() => $"{Id}: {Name}";
}

/// <summary>
/// Loader for yaml-test-suite test cases.
/// </summary>
public static class YamlTestSuiteLoader
{
    private static readonly YamlSerializerOptions SerializerOptions = new()
    {
        TypeInfoResolver = new TestSuiteSerializerContext()
    };

    /// <summary>
    /// Gets all test cases from the test suite.
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
    /// Gets test cases by category.
    /// </summary>
    public static IEnumerable<YamlTestCase> GetTestCasesByTag(string tag)
    {
        return GetAllTestCases().Where(tc => tc.Tags.Contains(tag, StringComparison.OrdinalIgnoreCase));
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
        // Look for TestSuiteData directory in output
        var outputDir = AppContext.BaseDirectory;
        var testSuiteDir = Path.Combine(outputDir, "TestSuiteData");
        if (Directory.Exists(testSuiteDir))
        {
            return testSuiteDir;
        }

        // Look in parent directories
        var current = new DirectoryInfo(outputDir);
        while (current != null)
        {
            var yamlTestSuite = Path.Combine(current.FullName, "test", "Yamlify.Tests", "yaml-test-suite", "src");
            if (Directory.Exists(yamlTestSuite))
            {
                return yamlTestSuite;
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
                Yaml = DecodeTestSuiteContent(raw.Yaml ?? ""),
                Tree = raw.Tree ?? "",
                Json = raw.Json,
                Dump = raw.Dump,
                Fail = raw.Fail,
                Skip = raw.Skip
            };
        }
    }

    /// <summary>
    /// Decodes special Unicode characters used by yaml-test-suite to represent
    /// invisible characters (spaces, tabs, newlines) and the end-of-input marker.
    /// </summary>
    /// <remarks>
    /// yaml-test-suite conventions:
    /// - ␣ (U+2423) OPEN BOX → space
    /// - Hard tabs are represented by one of: ———», ——», —», or » (em-dashes + right angle quote)
    ///   - — is U+2014 (EM DASH)
    ///   - » is U+00BB (RIGHT-POINTING DOUBLE ANGLE QUOTATION MARK)
    /// - ↵ (U+21B5) DOWNWARDS ARROW WITH CORNER LEFTWARDS → (stripped, newline already present)
    /// - ∎ (U+220E) END OF PROOF → (stripped, end marker)
    /// - ⇔ (U+21D4) LEFT RIGHT DOUBLE ARROW → (stripped, BOM indicator)
    /// - ← (U+2190) LEFTWARDS ARROW → carriage return
    /// </remarks>
    internal static string DecodeTestSuiteContent(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        var result = new System.Text.StringBuilder(content.Length);
        int i = 0;
        
        while (i < content.Length)
        {
            char c = content[i];
            
            // Check for tab sequences: ———», ——», —», or just »
            // These are em-dash (U+2014) followed by right angle quote (U+00BB)
            if (c == '\u2014') // EM DASH (—)
            {
                // Count consecutive em-dashes
                int dashCount = 0;
                while (i + dashCount < content.Length && content[i + dashCount] == '\u2014')
                {
                    dashCount++;
                }
                
                // Check if followed by right angle quote (»)
                if (i + dashCount < content.Length && content[i + dashCount] == '\u00BB')
                {
                    // This is a tab sequence
                    result.Append('\t');
                    i += dashCount + 1; // Skip all em-dashes and the angle quote
                    continue;
                }
                
                // Not a tab sequence, output the em-dashes
                for (int j = 0; j < dashCount; j++)
                {
                    result.Append('\u2014');
                }
                i += dashCount;
                continue;
            }
            
            if (c == '\u00BB') // » RIGHT-POINTING DOUBLE ANGLE QUOTATION MARK (tab by itself)
            {
                result.Append('\t');
                i++;
                continue;
            }
            
            switch (c)
            {
                case '\u2423': // ␣ OPEN BOX → space
                    result.Append(' ');
                    break;
                case '\u2192': // → RIGHTWARDS ARROW → tab (legacy support)
                    result.Append('\t');
                    break;
                case '\u21B5': // ↵ DOWNWARDS ARROW WITH CORNER LEFTWARDS (strip - newline follows)
                case '\u220E': // ∎ END OF PROOF (strip - end of input marker)
                case '\u21D4': // ⇔ LEFT RIGHT DOUBLE ARROW (strip - BOM indicator)
                    // Skip these marker characters
                    break;
                case '\u2190': // ← LEFTWARDS ARROW → carriage return
                    result.Append('\r');
                    break;
                default:
                    result.Append(c);
                    break;
            }
            i++;
        }
        
        return result.ToString();
    }
}
