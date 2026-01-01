namespace Yamlify.Serialization;

internal sealed class IgnoreCyclesResolver : ReferenceResolver
{
    private readonly HashSet<object> _visited = new(ReferenceEqualityComparer.Instance);

    public override void AddReference(string referenceId, object value) { }

    public override string GetReference(object value, out bool alreadyExists)
    {
        alreadyExists = !_visited.Add(value);
        return string.Empty;
    }

    public override object ResolveReference(string referenceId) => 
        throw new InvalidOperationException("IgnoreCycles does not support resolving references.");
}
