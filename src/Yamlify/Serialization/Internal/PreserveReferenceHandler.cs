namespace Yamlify.Serialization;

internal sealed class PreserveReferenceHandler : ReferenceHandler
{
    public override ReferenceResolver CreateResolver() => new PreserveResolver();
}
