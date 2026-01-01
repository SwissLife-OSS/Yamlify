namespace Yamlify.Nodes;

/// <summary>
/// Visitor interface for traversing YAML nodes.
/// </summary>
public interface IYamlVisitor
{
    /// <summary>Visits a scalar node.</summary>
    void Visit(YamlScalarNode scalar);
    
    /// <summary>Visits a sequence node.</summary>
    void Visit(YamlSequenceNode sequence);
    
    /// <summary>Visits a mapping node.</summary>
    void Visit(YamlMappingNode mapping);
    
    /// <summary>Visits an alias node.</summary>
    void Visit(YamlAliasNode alias);
}
