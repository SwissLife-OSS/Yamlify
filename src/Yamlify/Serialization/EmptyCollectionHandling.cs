namespace Yamlify.Serialization;

/// <summary>
/// Specifies how empty collections (arrays, lists) should be handled during serialization and deserialization.
/// </summary>
public enum EmptyCollectionHandling
{
    /// <summary>
    /// Empty collections are serialized as empty flow-style sequences ([]).
    /// Null values for collection types are deserialized as null.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Empty collections are serialized as empty flow-style sequences ([]).
    /// Null values for collection types are deserialized as empty collections.
    /// This is useful when you always want arrays instead of null values.
    /// </summary>
    /// <remarks>
    /// Per YAML spec, an empty value (key: ) represents null.
    /// With this option, when deserializing into array/list types, null becomes an empty collection.
    /// </remarks>
    PreferEmptyCollection = 1
}
