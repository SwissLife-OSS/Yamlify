namespace Yamlify.Core;

/// <summary>
/// Specifies the type of YAML token encountered by the <see cref="Utf8YamlReader"/>.
/// </summary>
public enum YamlTokenType : byte
{
    /// <summary>
    /// No token has been read yet.
    /// </summary>
    None = 0,

    /// <summary>
    /// The start of a YAML stream.
    /// </summary>
    StreamStart,

    /// <summary>
    /// The end of a YAML stream.
    /// </summary>
    StreamEnd,

    /// <summary>
    /// The start of a YAML document (--- marker or implicit).
    /// </summary>
    DocumentStart,

    /// <summary>
    /// The end of a YAML document (... marker or implicit).
    /// </summary>
    DocumentEnd,

    /// <summary>
    /// The start of a mapping (block or flow style).
    /// </summary>
    MappingStart,

    /// <summary>
    /// The end of a mapping.
    /// </summary>
    MappingEnd,

    /// <summary>
    /// The start of a sequence (block or flow style).
    /// </summary>
    SequenceStart,

    /// <summary>
    /// The end of a sequence.
    /// </summary>
    SequenceEnd,

    /// <summary>
    /// A scalar value (string, number, boolean, null, etc.).
    /// </summary>
    Scalar,

    /// <summary>
    /// An alias reference (*anchor).
    /// </summary>
    Alias,

    /// <summary>
    /// A tag property (!tag or !!tag).
    /// </summary>
    Tag,

    /// <summary>
    /// An anchor property (&amp;anchor).
    /// </summary>
    Anchor,

    /// <summary>
    /// A comment (presentation detail, not part of content).
    /// </summary>
    Comment,

    /// <summary>
    /// A YAML directive (%YAML 1.2).
    /// </summary>
    VersionDirective,

    /// <summary>
    /// A TAG directive (%TAG !prefix! uri).
    /// </summary>
    TagDirective,

    /// <summary>
    /// A block entry indicator (-).
    /// </summary>
    BlockEntry,

    /// <summary>
    /// A mapping key indicator (?).
    /// </summary>
    Key,

    /// <summary>
    /// A mapping value indicator (:).
    /// </summary>
    Value
}
