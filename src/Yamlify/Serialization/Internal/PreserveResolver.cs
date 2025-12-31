namespace Yamlify.Serialization;

internal sealed class PreserveResolver : ReferenceResolver
{
    private readonly Dictionary<string, object> _idToObject = new();
    private readonly Dictionary<object, string> _objectToId = new(ReferenceEqualityComparer.Instance);
    private int _nextId = 1;

    public override void AddReference(string referenceId, object value)
    {
        _idToObject[referenceId] = value;
        _objectToId[value] = referenceId;
    }

    public override string GetReference(object value, out bool alreadyExists)
    {
        if (_objectToId.TryGetValue(value, out var existing))
        {
            alreadyExists = true;
            return existing;
        }

        alreadyExists = false;
        var id = $"ref{_nextId++}";
        _objectToId[value] = id;
        _idToObject[id] = value;
        return id;
    }

    public override object ResolveReference(string referenceId)
    {
        if (_idToObject.TryGetValue(referenceId, out var value))
        {
            return value;
        }
        throw new InvalidOperationException($"Reference '{referenceId}' not found.");
    }
}
