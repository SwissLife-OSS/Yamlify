namespace Yamlify.Serialization;

/// <summary>
/// A disposable scope for managing reference resolver lifetime during serialization.
/// </summary>
internal readonly struct ReferenceResolverScope : IDisposable
{
    public ReferenceResolverScope(ReferenceResolver? resolver)
    {
        // Resolver is already set by BeginSerialize
    }

    public void Dispose()
    {
        YamlSerializerOptions.ClearCurrentResolver();
    }
}
