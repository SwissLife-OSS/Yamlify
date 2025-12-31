namespace Yamlify.RepresentationModel;

/// <summary>
/// Parser for building YamlDocument instances from YAML input.
/// </summary>
internal sealed class YamlDocumentParser
{
    private readonly Dictionary<string, YamlNode> _anchors = new();

    public IEnumerable<YamlDocument> ParseDocuments(ReadOnlySpan<byte> utf8Yaml)
    {
        var reader = new Core.Utf8YamlReader(utf8Yaml);
        var documents = new List<YamlDocument>();
        
        while (reader.Read())
        {
            if (reader.TokenType == Core.YamlTokenType.DocumentStart ||
                reader.TokenType == Core.YamlTokenType.Scalar ||
                reader.TokenType == Core.YamlTokenType.MappingStart ||
                reader.TokenType == Core.YamlTokenType.SequenceStart)
            {
                var doc = ParseDocument(ref reader);
                documents.Add(doc);
            }
        }
        
        // If no documents were found but there was content, create one document
        if (documents.Count == 0)
        {
            // Reset and try to parse as implicit document
            reader = new Core.Utf8YamlReader(utf8Yaml);
            if (reader.Read())
            {
                var doc = ParseDocument(ref reader);
                documents.Add(doc);
            }
        }
        
        return documents;
    }

    private YamlDocument ParseDocument(ref Core.Utf8YamlReader reader)
    {
        var document = new YamlDocument();
        _anchors.Clear();
        
        if (reader.TokenType == Core.YamlTokenType.DocumentStart)
        {
            document.HasExplicitStart = true;
            if (!reader.Read())
            {
                return document;
            }
        }
        
        document.RootNode = ParseNode(ref reader);
        
        if (reader.TokenType == Core.YamlTokenType.DocumentEnd)
        {
            document.HasExplicitEnd = true;
        }
        
        return document;
    }

    private YamlNode ParseNode(ref Core.Utf8YamlReader reader)
    {
        string? anchor = null;
        string? tag = null;
        
        // Handle anchor and tag
        while (reader.TokenType == Core.YamlTokenType.Anchor || 
               reader.TokenType == Core.YamlTokenType.Tag)
        {
            if (reader.TokenType == Core.YamlTokenType.Anchor)
            {
                anchor = reader.GetString();
            }
            else if (reader.TokenType == Core.YamlTokenType.Tag)
            {
                tag = reader.GetString();
            }
            
            if (!reader.Read())
            {
                break;
            }
        }
        
        YamlNode node = reader.TokenType switch
        {
            Core.YamlTokenType.Scalar => ParseScalar(ref reader),
            Core.YamlTokenType.MappingStart => ParseMapping(ref reader),
            Core.YamlTokenType.SequenceStart => ParseSequence(ref reader),
            Core.YamlTokenType.Alias => ParseAlias(ref reader),
            _ => new YamlScalarNode(null)
        };
        
        node.Anchor = anchor;
        node.Tag = tag;
        
        if (anchor != null)
        {
            _anchors[anchor] = node;
        }
        
        return node;
    }

    private YamlScalarNode ParseScalar(ref Core.Utf8YamlReader reader)
    {
        var node = new YamlScalarNode(reader.GetString())
        {
            Start = reader.TokenStart,
            End = reader.Position
        };
        
        reader.Read(); // Move past scalar
        return node;
    }

    private YamlMappingNode ParseMapping(ref Core.Utf8YamlReader reader)
    {
        var node = new YamlMappingNode
        {
            Start = reader.TokenStart
        };
        
        reader.Read(); // Move past MappingStart
        
        while (reader.TokenType != Core.YamlTokenType.MappingEnd &&
               reader.TokenType != Core.YamlTokenType.None)
        {
            var key = ParseNode(ref reader);
            var value = ParseNode(ref reader);
            node.Add(key, value);
        }
        
        node.End = reader.Position;
        reader.Read(); // Move past MappingEnd
        
        return node;
    }

    private YamlSequenceNode ParseSequence(ref Core.Utf8YamlReader reader)
    {
        var node = new YamlSequenceNode
        {
            Start = reader.TokenStart
        };
        
        reader.Read(); // Move past SequenceStart
        
        while (reader.TokenType != Core.YamlTokenType.SequenceEnd &&
               reader.TokenType != Core.YamlTokenType.None)
        {
            var item = ParseNode(ref reader);
            node.Add(item);
        }
        
        node.End = reader.Position;
        reader.Read(); // Move past SequenceEnd
        
        return node;
    }

    private YamlNode ParseAlias(ref Core.Utf8YamlReader reader)
    {
        var aliasName = reader.GetString() ?? "";
        
        if (_anchors.TryGetValue(aliasName, out var referencedNode))
        {
            reader.Read();
            return referencedNode.DeepClone();
        }
        
        var aliasNode = new YamlAliasNode(aliasName)
        {
            Start = reader.TokenStart,
            End = reader.Position
        };
        
        reader.Read();
        return aliasNode;
    }
}
