namespace Yamlify.Nodes;

/// <summary>
/// Emitter for writing YamlDocument instances to YAML output.
/// </summary>
internal sealed class YamlDocumentEmitter
{
    private readonly Utf8YamlWriter _writer;
    private readonly HashSet<YamlNode> _visitedNodes = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<YamlNode, string> _anchors = new(ReferenceEqualityComparer.Instance);
    private int _anchorCounter;

    public YamlDocumentEmitter(Utf8YamlWriter writer)
    {
        _writer = writer;
    }

    public void Emit(YamlDocument document)
    {
        _visitedNodes.Clear();
        _anchors.Clear();
        _anchorCounter = 0;
        
        // First pass: detect circular references and assign anchors
        if (document.RootNode != null)
        {
            DetectAnchors(document.RootNode);
        }
        
        _writer.WriteDocumentStart();
        
        if (document.RootNode != null)
        {
            EmitNode(document.RootNode);
        }
        
        _writer.WriteDocumentEnd();
    }

    private void DetectAnchors(YamlNode node)
    {
        if (_visitedNodes.Contains(node))
        {
            // Circular reference detected - assign anchor if not already assigned
            if (!_anchors.ContainsKey(node))
            {
                _anchors[node] = $"anchor{++_anchorCounter}";
            }
            return;
        }
        
        _visitedNodes.Add(node);
        
        switch (node)
        {
            case YamlSequenceNode seq:
                foreach (var item in seq)
                {
                    DetectAnchors(item);
                }
                break;
                
            case YamlMappingNode map:
                foreach (var kvp in map)
                {
                    DetectAnchors(kvp.Key);
                    DetectAnchors(kvp.Value);
                }
                break;
        }
    }

    private void EmitNode(YamlNode node)
    {
        // Check if this node needs an alias reference
        if (_anchors.TryGetValue(node, out var anchorName))
        {
            if (_visitedNodes.Contains(node))
            {
                _writer.WriteAlias(anchorName);
                return;
            }
            
            _writer.WriteAnchor(anchorName);
        }
        else if (node.Anchor != null)
        {
            _writer.WriteAnchor(node.Anchor);
        }
        
        if (node.Tag != null)
        {
            _writer.WriteTag(node.Tag);
        }
        
        _visitedNodes.Add(node);
        
        switch (node)
        {
            case YamlScalarNode scalar:
                EmitScalar(scalar);
                break;
                
            case YamlSequenceNode seq:
                EmitSequence(seq);
                break;
                
            case YamlMappingNode map:
                EmitMapping(map);
                break;
                
            case YamlAliasNode alias:
                _writer.WriteAlias(alias.AnchorName);
                break;
        }
    }

    private void EmitScalar(YamlScalarNode scalar)
    {
        if (scalar.IsNull)
        {
            _writer.WriteNull();
        }
        else
        {
            _writer.WriteString(scalar.Value);
        }
    }

    private void EmitSequence(YamlSequenceNode seq)
    {
        _writer.WriteSequenceStart(seq.Style);
        
        foreach (var item in seq)
        {
            EmitNode(item);
        }
        
        _writer.WriteSequenceEnd();
    }

    private void EmitMapping(YamlMappingNode map)
    {
        _writer.WriteMappingStart(map.Style);
        
        foreach (var kvp in map)
        {
            if (kvp.Key is YamlScalarNode key)
            {
                _writer.WritePropertyName(key.Value ?? "");
            }
            else
            {
                // Complex keys - rare but valid in YAML
                _writer.WritePropertyName(kvp.Key.ToString() ?? "");
            }
            
            EmitNode(kvp.Value);
        }
        
        _writer.WriteMappingEnd();
    }
}
