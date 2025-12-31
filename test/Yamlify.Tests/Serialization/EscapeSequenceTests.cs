using Yamlify.Core;
using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Tests for escape sequence handling in double-quoted strings.
/// </summary>
public class EscapeSequenceTests
{
    [Fact]
    public void GetString_WithEscapedQuotes_ShouldDecodeCorrectly()
    {
        // YAML: value: "hello \"world\""
        // Expected decoded value: hello "world"
        var yaml = """
            value: "hello \"world\""
            """u8;

        var reader = new Utf8YamlReader(yaml);
        string? scalarValue = null;

        while (reader.Read())
        {
            if (reader.TokenType == YamlTokenType.Scalar)
            {
                var value = reader.GetString();
                if (value != "value")
                {
                    scalarValue = value;
                }
            }
        }

        // This is the YAML spec behavior: \" should become "
        Assert.Equal("hello \"world\"", scalarValue);
    }

    [Fact]
    public void GetString_WithEscapedBackslash_ShouldDecodeCorrectly()
    {
        // YAML: value: "path\\to\\file"
        // Expected decoded value: path\to\file
        var yaml = """
            value: "path\\to\\file"
            """u8;

        var reader = new Utf8YamlReader(yaml);
        string? scalarValue = null;

        while (reader.Read())
        {
            if (reader.TokenType == YamlTokenType.Scalar)
            {
                var value = reader.GetString();
                if (value != "value")
                {
                    scalarValue = value;
                }
            }
        }

        Assert.Equal("path\\to\\file", scalarValue);
    }

    [Fact]
    public void GetString_WithNewlineEscape_ShouldDecodeCorrectly()
    {
        // YAML: value: "line1\nline2"
        // Expected decoded value: line1<newline>line2
        var yaml = """
            value: "line1\nline2"
            """u8;

        var reader = new Utf8YamlReader(yaml);
        string? scalarValue = null;

        while (reader.Read())
        {
            if (reader.TokenType == YamlTokenType.Scalar)
            {
                var value = reader.GetString();
                if (value != "value")
                {
                    scalarValue = value;
                }
            }
        }

        Assert.Equal("line1\nline2", scalarValue);
    }

    [Fact]
    public void GetString_WithTabEscape_ShouldDecodeCorrectly()
    {
        // YAML: value: "col1\tcol2"
        // Expected decoded value: col1<tab>col2
        var yaml = """
            value: "col1\tcol2"
            """u8;

        var reader = new Utf8YamlReader(yaml);
        string? scalarValue = null;

        while (reader.Read())
        {
            if (reader.TokenType == YamlTokenType.Scalar)
            {
                var value = reader.GetString();
                if (value != "value")
                {
                    scalarValue = value;
                }
            }
        }

        Assert.Equal("col1\tcol2", scalarValue);
    }
}
