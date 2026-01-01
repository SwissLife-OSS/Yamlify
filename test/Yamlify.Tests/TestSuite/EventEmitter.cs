using System.Text;
using Yamlify;

namespace Yamlify.Tests.TestSuite;

/// <summary>
/// Converts Yamlify parse events to the yaml-test-suite event format.
/// </summary>
public static class EventEmitter
{
    /// <summary>
    /// Parses YAML and returns events in the yaml-test-suite format.
    /// </summary>
    public static string EmitEvents(string yaml)
    {
        var bytes = Encoding.UTF8.GetBytes(yaml);
        return EmitEvents(bytes);
    }

    /// <summary>
    /// Parses YAML and returns events in the yaml-test-suite format.
    /// </summary>
    public static string EmitEvents(ReadOnlySpan<byte> utf8Yaml)
    {
        var sb = new StringBuilder();
        var reader = new Utf8YamlReader(utf8Yaml);
        var depth = 0;

        while (reader.Read())
        {
            var indent = new string(' ', depth);
            
            switch (reader.TokenType)
            {
                case YamlTokenType.StreamStart:
                    sb.AppendLine("+STR");
                    break;

                case YamlTokenType.StreamEnd:
                    sb.AppendLine("-STR");
                    break;

                case YamlTokenType.DocumentStart:
                    sb.AppendLine($"{indent}+DOC");
                    depth++;
                    break;

                case YamlTokenType.DocumentEnd:
                    depth = Math.Max(0, depth - 1);
                    indent = new string(' ', depth);
                    sb.AppendLine($"{indent}-DOC");
                    break;

                case YamlTokenType.MappingStart:
                    sb.AppendLine($"{indent}+MAP");
                    depth++;
                    break;

                case YamlTokenType.MappingEnd:
                    depth = Math.Max(0, depth - 1);
                    indent = new string(' ', depth);
                    sb.AppendLine($"{indent}-MAP");
                    break;

                case YamlTokenType.SequenceStart:
                    sb.AppendLine($"{indent}+SEQ");
                    depth++;
                    break;

                case YamlTokenType.SequenceEnd:
                    depth = Math.Max(0, depth - 1);
                    indent = new string(' ', depth);
                    sb.AppendLine($"{indent}-SEQ");
                    break;

                case YamlTokenType.Scalar:
                    var value = reader.GetString() ?? "";
                    // For plain scalars, normalize line breaks to spaces (YAML line folding)
                    if (reader.ScalarStyle == ScalarStyle.Plain)
                    {
                        value = NormalizePlainScalar(value);
                    }
                    var escapedValue = EscapeValue(value);
                    sb.AppendLine($"{indent}=VAL :{escapedValue}");
                    break;

                case YamlTokenType.Alias:
                    var aliasName = reader.GetString() ?? "";
                    sb.AppendLine($"{indent}=ALI *{aliasName}");
                    break;

                case YamlTokenType.Anchor:
                    // Anchors are typically attached to the next node
                    // For the event format, we might need to combine them
                    break;

                case YamlTokenType.Tag:
                    // Tags are typically attached to the next node
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Normalizes a plain scalar by folding line breaks to spaces.
    /// In YAML, plain scalars use "line folding" where line breaks become spaces.
    /// </summary>
    private static string NormalizePlainScalar(string value)
    {
        if (!value.Contains('\n') && !value.Contains('\r'))
            return value;
        
        var sb = new StringBuilder();
        bool lastWasNewline = false;
        
        foreach (char c in value)
        {
            if (c == '\n' || c == '\r')
            {
                if (!lastWasNewline)
                {
                    sb.Append(' ');
                    lastWasNewline = true;
                }
                // Skip consecutive newlines (they become single space)
            }
            else
            {
                lastWasNewline = false;
                sb.Append(c);
            }
        }
        
        return sb.ToString().Trim();
    }

    /// <summary>
    /// Escapes special characters in values for the event format.
    /// </summary>
    private static string EscapeValue(string value)
    {
        var sb = new StringBuilder();
        foreach (char c in value)
        {
            switch (c)
            {
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}

/// <summary>
/// Compares expected and actual event trees.
/// </summary>
public static class EventComparer
{
    /// <summary>
    /// Compares two event trees for equality.
    /// </summary>
    /// <param name="expected">The expected event tree.</param>
    /// <param name="actual">The actual event tree.</param>
    /// <returns>True if the trees match, false otherwise.</returns>
    public static bool Compare(string expected, string actual)
    {
        var expectedEvents = ParseEvents(expected);
        var actualEvents = ParseEvents(actual);

        if (expectedEvents.Count != actualEvents.Count)
            return false;

        for (int i = 0; i < expectedEvents.Count; i++)
        {
            if (!EventsEqual(expectedEvents[i], actualEvents[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Gets detailed differences between two event trees.
    /// </summary>
    public static List<string> GetDifferences(string expected, string actual)
    {
        var differences = new List<string>();
        var expectedEvents = ParseEvents(expected);
        var actualEvents = ParseEvents(actual);

        var maxCount = Math.Max(expectedEvents.Count, actualEvents.Count);
        for (int i = 0; i < maxCount; i++)
        {
            var exp = i < expectedEvents.Count ? expectedEvents[i] : "(missing)";
            var act = i < actualEvents.Count ? actualEvents[i] : "(missing)";

            if (!EventsEqual(exp, act))
            {
                differences.Add($"Line {i + 1}: Expected '{exp}', got '{act}'");
            }
        }

        return differences;
    }

    private static List<string> ParseEvents(string eventTree)
    {
        return eventTree
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    private static bool EventsEqual(string expected, string actual)
    {
        // Normalize whitespace and compare
        var normalizedExpected = expected.Trim();
        var normalizedActual = actual.Trim();
        return normalizedExpected == normalizedActual;
    }
}
