using System.Globalization;
using System.Text.RegularExpressions;

namespace Yamlify.Schema;

/// <summary>
/// The YAML 1.2 Failsafe Schema - the most basic schema with only strings, sequences, and mappings.
/// </summary>
public sealed partial class FailsafeSchema : IYamlSchema
{
    /// <summary>
    /// Gets the singleton instance of the Failsafe Schema.
    /// </summary>
    public static FailsafeSchema Instance { get; } = new();

    /// <inheritdoc/>
    public string Name => "Failsafe";

    private FailsafeSchema() { }

    /// <inheritdoc/>
    public string? ResolveScalarTag(ReadOnlySpan<char> value)
    {
        // Failsafe schema treats all scalars as strings
        return YamlTags.Str;
    }

    /// <inheritdoc/>
    public string ResolveNonPlainScalarTag(ReadOnlySpan<char> value, ScalarStyle style)
    {
        return YamlTags.Str;
    }

    /// <inheritdoc/>
    public string GetCanonicalValue(string value, string tag)
    {
        return value;
    }

    /// <inheritdoc/>
    public bool ValidateValue(string value, string tag)
    {
        return tag == YamlTags.Str || tag == YamlTags.Seq || tag == YamlTags.Map;
    }
}

/// <summary>
/// The YAML 1.2 JSON Schema - compatible with JSON types.
/// </summary>
public sealed partial class JsonSchema : IYamlSchema
{
    /// <summary>
    /// Gets the singleton instance of the JSON Schema.
    /// </summary>
    public static JsonSchema Instance { get; } = new();

    /// <inheritdoc/>
    public string Name => "JSON";

    private JsonSchema() { }

    /// <inheritdoc/>
    public string? ResolveScalarTag(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return YamlTags.Str;

        // null
        if (value.SequenceEqual("null"))
            return YamlTags.Null;

        // bool
        if (value.SequenceEqual("true") || value.SequenceEqual("false"))
            return YamlTags.Bool;

        // int (JSON only supports decimal)
        if (IsJsonInteger(value))
            return YamlTags.Int;

        // float
        if (IsJsonFloat(value))
            return YamlTags.Float;

        return YamlTags.Str;
    }

    /// <inheritdoc/>
    public string ResolveNonPlainScalarTag(ReadOnlySpan<char> value, ScalarStyle style)
    {
        return YamlTags.Str;
    }

    /// <inheritdoc/>
    public string GetCanonicalValue(string value, string tag)
    {
        return tag switch
        {
            YamlTags.Null => "null",
            YamlTags.Bool => value.ToLowerInvariant() == "true" ? "true" : "false",
            _ => value
        };
    }

    /// <inheritdoc/>
    public bool ValidateValue(string value, string tag)
    {
        return tag switch
        {
            YamlTags.Null => value == "null",
            YamlTags.Bool => value is "true" or "false",
            YamlTags.Int => IsJsonInteger(value),
            YamlTags.Float => IsJsonFloat(value),
            YamlTags.Str => true,
            _ => false
        };
    }

    private static bool IsJsonInteger(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty) return false;

        int start = 0;
        if (value[0] == '-')
        {
            if (value.Length == 1) return false;
            start = 1;
        }

        // "0" is valid, "-0" is valid, but "-01" is not
        if (value.Length - start > 1 && value[start] == '0')
            return false;

        for (int i = start; i < value.Length; i++)
        {
            if (!char.IsAsciiDigit(value[i]))
                return false;
        }

        return true;
    }

    private static bool IsJsonFloat(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty) return false;

        // Try to parse as a double in invariant culture
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }
}

/// <summary>
/// The YAML 1.2 Core Schema - the recommended default schema with user-friendly literals.
/// </summary>
public sealed partial class CoreSchema : IYamlSchema
{
    /// <summary>
    /// Gets the singleton instance of the Core Schema.
    /// </summary>
    public static CoreSchema Instance { get; } = new();

    /// <inheritdoc/>
    public string Name => "Core";

    private CoreSchema() { }

    // Regex patterns for Core Schema (compiled for performance)
    [GeneratedRegex(@"^(null|Null|NULL|~)$", RegexOptions.Compiled)]
    private static partial Regex NullPattern();

    [GeneratedRegex(@"^(true|True|TRUE|false|False|FALSE)$", RegexOptions.Compiled)]
    private static partial Regex BoolPattern();

    [GeneratedRegex(@"^[-+]?(0|[1-9][0-9]*)$", RegexOptions.Compiled)]
    private static partial Regex DecimalIntPattern();

    [GeneratedRegex(@"^0o[0-7]+$", RegexOptions.Compiled)]
    private static partial Regex OctalIntPattern();

    [GeneratedRegex(@"^0x[0-9a-fA-F]+$", RegexOptions.Compiled)]
    private static partial Regex HexIntPattern();

    [GeneratedRegex(@"^[-+]?(\.[0-9]+|[0-9]+(\.[0-9]*)?)([eE][-+]?[0-9]+)?$", RegexOptions.Compiled)]
    private static partial Regex FloatPattern();

    [GeneratedRegex(@"^[-+]?(\.inf|\.Inf|\.INF)$", RegexOptions.Compiled)]
    private static partial Regex InfinityPattern();

    [GeneratedRegex(@"^(\.nan|\.NaN|\.NAN)$", RegexOptions.Compiled)]
    private static partial Regex NanPattern();

    /// <inheritdoc/>
    public string? ResolveScalarTag(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return YamlTags.Null;

        var str = value.ToString();

        // null
        if (NullPattern().IsMatch(str))
            return YamlTags.Null;

        // bool
        if (BoolPattern().IsMatch(str))
            return YamlTags.Bool;

        // int (decimal, octal, hex)
        if (DecimalIntPattern().IsMatch(str) || 
            OctalIntPattern().IsMatch(str) || 
            HexIntPattern().IsMatch(str))
            return YamlTags.Int;

        // float (including infinity and NaN)
        if (FloatPattern().IsMatch(str) || 
            InfinityPattern().IsMatch(str) || 
            NanPattern().IsMatch(str))
            return YamlTags.Float;

        return YamlTags.Str;
    }

    /// <inheritdoc/>
    public string ResolveNonPlainScalarTag(ReadOnlySpan<char> value, ScalarStyle style)
    {
        // Non-plain scalars are always strings in Core Schema
        return YamlTags.Str;
    }

    /// <inheritdoc/>
    public string GetCanonicalValue(string value, string tag)
    {
        return tag switch
        {
            YamlTags.Null => "null",
            YamlTags.Bool => value.ToLowerInvariant() switch
            {
                "true" or "yes" or "on" => "true",
                _ => "false"
            },
            YamlTags.Int => GetCanonicalInteger(value),
            YamlTags.Float => GetCanonicalFloat(value),
            _ => value
        };
    }

    /// <inheritdoc/>
    public bool ValidateValue(string value, string tag)
    {
        return tag switch
        {
            YamlTags.Null => NullPattern().IsMatch(value) || string.IsNullOrEmpty(value),
            YamlTags.Bool => BoolPattern().IsMatch(value),
            YamlTags.Int => DecimalIntPattern().IsMatch(value) || 
                           OctalIntPattern().IsMatch(value) || 
                           HexIntPattern().IsMatch(value),
            YamlTags.Float => FloatPattern().IsMatch(value) || 
                             InfinityPattern().IsMatch(value) || 
                             NanPattern().IsMatch(value),
            YamlTags.Str => true,
            _ => false
        };
    }

    private static string GetCanonicalInteger(string value)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return Convert.ToInt64(value[2..], 16).ToString();
        }
        
        if (value.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
        {
            return Convert.ToInt64(value[2..], 8).ToString();
        }
        
        // Remove leading + sign
        if (value.StartsWith('+'))
        {
            return value[1..];
        }
        
        return value;
    }

    private static string GetCanonicalFloat(string value)
    {
        var lower = value.ToLowerInvariant();
        
        if (lower is ".inf" or "+.inf")
            return ".inf";
        if (lower == "-.inf")
            return "-.inf";
        if (lower == ".nan")
            return ".nan";
        
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
        {
            if (double.IsPositiveInfinity(d)) return ".inf";
            if (double.IsNegativeInfinity(d)) return "-.inf";
            if (double.IsNaN(d)) return ".nan";
            return d.ToString("G", CultureInfo.InvariantCulture);
        }
        
        return value;
    }

    /// <summary>
    /// Parses a null value according to Core Schema rules.
    /// </summary>
    public static bool IsNull(ReadOnlySpan<char> value)
    {
        return value.IsEmpty || 
               value.SequenceEqual("~") ||
               value.SequenceEqual("null") ||
               value.SequenceEqual("Null") ||
               value.SequenceEqual("NULL");
    }

    /// <summary>
    /// Parses a boolean value according to Core Schema rules.
    /// </summary>
    public static bool TryGetBoolean(ReadOnlySpan<char> value, out bool result)
    {
        if (value.SequenceEqual("true") || value.SequenceEqual("True") || value.SequenceEqual("TRUE"))
        {
            result = true;
            return true;
        }
        
        if (value.SequenceEqual("false") || value.SequenceEqual("False") || value.SequenceEqual("FALSE"))
        {
            result = false;
            return true;
        }
        
        result = false;
        return false;
    }

    /// <summary>
    /// Parses an integer value according to Core Schema rules.
    /// </summary>
    public static bool TryGetInt64(ReadOnlySpan<char> value, out long result)
    {
        var str = value.ToString();
        
        // Hexadecimal
        if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                result = Convert.ToInt64(str[2..], 16);
                return true;
            }
            catch
            {
                result = 0;
                return false;
            }
        }
        
        // Octal
        if (str.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                result = Convert.ToInt64(str[2..], 8);
                return true;
            }
            catch
            {
                result = 0;
                return false;
            }
        }
        
        // Decimal
        return long.TryParse(value, out result);
    }

    /// <summary>
    /// Parses a floating-point value according to Core Schema rules.
    /// </summary>
    public static bool TryGetDouble(ReadOnlySpan<char> value, out double result)
    {
        var lower = value.ToString().ToLowerInvariant();
        
        if (lower is ".inf" or "+.inf")
        {
            result = double.PositiveInfinity;
            return true;
        }
        
        if (lower == "-.inf")
        {
            result = double.NegativeInfinity;
            return true;
        }
        
        if (lower == ".nan")
        {
            result = double.NaN;
            return true;
        }
        
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}
