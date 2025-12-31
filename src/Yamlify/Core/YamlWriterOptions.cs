using System.Buffers;

namespace Yamlify.Core;

/// <summary>
/// Provides options for configuring a <see cref="Utf8YamlWriter"/>.
/// </summary>
public sealed class YamlWriterOptions
{
    /// <summary>
    /// Gets the default writer options.
    /// </summary>
    public static YamlWriterOptions Default { get; } = new();

    /// <summary>
    /// Gets or sets the number of spaces to use for indentation. Default is 2.
    /// </summary>
    public int IndentSize { get; init; } = 2;

    /// <summary>
    /// Gets or sets a value indicating whether to prefer flow style for small collections.
    /// Default is false (prefer block style).
    /// </summary>
    public bool PreferFlowStyle { get; init; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to emit the YAML directive (%YAML 1.2).
    /// Default is false.
    /// </summary>
    public bool EmitYamlDirective { get; init; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to emit document start markers (---).
    /// Default is true for multi-document streams, false for single documents.
    /// </summary>
    public bool EmitDocumentMarkers { get; init; } = false;

    /// <summary>
    /// Gets or sets the maximum line length before wrapping. Default is 80.
    /// </summary>
    public int MaxLineLength { get; init; } = 80;

    /// <summary>
    /// Gets or sets the preferred scalar style for strings.
    /// Default is <see cref="ScalarStyle.Any"/> (auto-detect).
    /// </summary>
    public ScalarStyle DefaultScalarStyle { get; init; } = ScalarStyle.Any;

    /// <summary>
    /// Gets or sets a value indicating whether to skip null values in mappings.
    /// Default is false.
    /// </summary>
    public bool SkipNullValues { get; init; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to indent sequence items relative to their parent key.
    /// When true (default), sequence items are indented:
    /// <code>
    /// resources:
    ///   - name: foo
    /// </code>
    /// When false, sequence items are at the same level as the parent key (compact style):
    /// <code>
    /// resources:
    /// - name: foo
    /// </code>
    /// </summary>
    public bool IndentSequenceItems { get; init; } = true;
}
