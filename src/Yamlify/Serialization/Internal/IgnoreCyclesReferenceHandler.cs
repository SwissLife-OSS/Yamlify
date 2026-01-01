namespace Yamlify.Serialization;

internal sealed class IgnoreCyclesReferenceHandler : ReferenceHandler
{
    public override ReferenceResolver CreateResolver() => new IgnoreCyclesResolver();
}
