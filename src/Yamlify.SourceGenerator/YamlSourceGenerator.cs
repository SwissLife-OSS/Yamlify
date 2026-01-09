using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Yamlify.SourceGenerator;

/// <summary>
/// Incremental source generator for AOT-compatible YAML serialization.
/// </summary>
/// <remarks>
/// This generator creates compile-time serialization/deserialization code
/// that doesn't use any reflection, making it fully AOT-compatible.
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class YamlSourceGenerator : IIncrementalGenerator
{
    private const string YamlSerializableAttribute = "Yamlify.Serialization.YamlSerializableAttribute";
    private const string YamlSerializableAttributeGeneric = "Yamlify.Serialization.YamlSerializableAttribute<T>";
    private const string YamlDerivedTypeMappingAttributeGeneric = "Yamlify.Serialization.YamlDerivedTypeMappingAttribute<TBase, TDerived>";
    private const string YamlSerializerContextBase = "Yamlify.Serialization.YamlSerializerContext";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all classes with [YamlSerializable] attributes
        var contextDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateClass(node),
                transform: static (ctx, _) => GetSemanticTarget(ctx))
            .Where(static target => target is not null)
            .Select(static (target, _) => target!);

        // Combine with compilation
        var compilationAndClasses = context.CompilationProvider.Combine(contextDeclarations.Collect());

        // Generate source
        context.RegisterSourceOutput(compilationAndClasses, 
            static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    private static bool IsCandidateClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDecl &&
               classDecl.AttributeLists.Count > 0 &&
               classDecl.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    private static ContextToGenerate? GetSemanticTarget(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
        
        if (classSymbol is null)
        {
            return null;
        }

        // Check if it derives from YamlSerializerContext
        var baseType = classSymbol.BaseType;
        var isSerializerContext = false;
        
        while (baseType is not null)
        {
            if (baseType.ToDisplayString() == YamlSerializerContextBase)
            {
                isSerializerContext = true;
                break;
            }
            baseType = baseType.BaseType;
        }

        if (!isSerializerContext)
        {
            return null;
        }

        // Collect [YamlSerializable] attributes and parse options
        var typesToGenerate = new List<TypeToGenerate>();
        var propertyOrdering = PropertyOrderingMode.DeclarationOrder;
        var indentSequenceItems = true;
        var ignoreNullValues = false;
        var ignoreEmptyObjects = false;
        var discriminatorPosition = DiscriminatorPositionMode.PropertyOrder;
        
        // First pass: collect YamlDerivedTypeMapping attributes
        // Key: base type display string, Value: list of (discriminator, derivedType)
        var derivedTypeMappingsFromAttrs = new Dictionary<string, List<(string Discriminator, INamedTypeSymbol DerivedType)>>();
        
        foreach (var attributeData in classSymbol.GetAttributes())
        {
            var attrOriginalDef = attributeData.AttributeClass?.OriginalDefinition?.ToDisplayString();
            
            if (attrOriginalDef == YamlDerivedTypeMappingAttributeGeneric)
            {
                // [YamlDerivedTypeMapping<TBase, TDerived>("discriminator")]
                if (attributeData.AttributeClass is { IsGenericType: true, TypeArguments.Length: 2 } attrClass &&
                    attrClass.TypeArguments[0] is INamedTypeSymbol mappingBaseType &&
                    attrClass.TypeArguments[1] is INamedTypeSymbol mappingDerivedType)
                {
                    var baseTypeKey = mappingBaseType.ToDisplayString();
                    
                    // Get discriminator from constructor argument (optional)
                    string? discriminator = null;
                    if (attributeData.ConstructorArguments.Length > 0 && 
                        attributeData.ConstructorArguments[0].Value is string discValue)
                    {
                        discriminator = discValue;
                    }
                    discriminator ??= mappingDerivedType.Name;
                    
                    if (!derivedTypeMappingsFromAttrs.TryGetValue(baseTypeKey, out var mappings))
                    {
                        mappings = new List<(string, INamedTypeSymbol)>();
                        derivedTypeMappingsFromAttrs[baseTypeKey] = mappings;
                    }
                    mappings.Add((discriminator, mappingDerivedType));
                }
            }
        }
        
        // Second pass: process YamlSerializable attributes
        foreach (var attributeData in classSymbol.GetAttributes())
        {
            var attrName = attributeData.AttributeClass?.ToDisplayString();
            var attrOriginalDef = attributeData.AttributeClass?.OriginalDefinition?.ToDisplayString();
            
            // Support both [YamlSerializable(typeof(T))] and [YamlSerializable<T>]
            INamedTypeSymbol? typeArg = null;
            var isYamlSerializableAttribute = false;
            
            if (attrName == YamlSerializableAttribute)
            {
                // Non-generic: [YamlSerializable(typeof(T))]
                if (attributeData.ConstructorArguments.Length > 0 &&
                    attributeData.ConstructorArguments[0].Value is INamedTypeSymbol ctorArg)
                {
                    typeArg = ctorArg;
                    isYamlSerializableAttribute = true;
                }
            }
            else if (attrOriginalDef == YamlSerializableAttributeGeneric)
            {
                // Generic: [YamlSerializable<T>]
                if (attributeData.AttributeClass is { IsGenericType: true, TypeArguments.Length: > 0 } attrClass &&
                    attrClass.TypeArguments[0] is INamedTypeSymbol genericArg)
                {
                    typeArg = genericArg;
                    isYamlSerializableAttribute = true;
                }
            }
            
            if (isYamlSerializableAttribute && typeArg is not null)
            {
                // Check for per-type PropertyOrdering override
                PropertyOrderingMode? typeOrdering = null;
                string? typeDiscriminatorPropertyName = null;
                List<INamedTypeSymbol>? derivedTypes = null;
                List<string>? derivedTypeDiscriminators = null;
                
                foreach (var namedArg in attributeData.NamedArguments)
                {
                    if (namedArg.Key == "PropertyOrdering" && namedArg.Value.Value is int orderingValue && orderingValue >= 0)
                    {
                        // Only set if not Inherit (-1)
                        typeOrdering = (PropertyOrderingMode)orderingValue;
                    }
                    else if (namedArg.Key == "TypeDiscriminatorPropertyName" && namedArg.Value.Value is string discPropName)
                    {
                        typeDiscriminatorPropertyName = discPropName;
                    }
                    else if (namedArg.Key == "DerivedTypes" && !namedArg.Value.IsNull)
                    {
                        derivedTypes = namedArg.Value.Values
                            .Where(v => v.Value is INamedTypeSymbol)
                            .Select(v => (INamedTypeSymbol)v.Value!)
                            .ToList();
                    }
                    else if (namedArg.Key == "DerivedTypeDiscriminators" && !namedArg.Value.IsNull)
                    {
                        derivedTypeDiscriminators = namedArg.Value.Values
                            .Where(v => v.Value is string)
                            .Select(v => (string)v.Value!)
                            .ToList();
                    }
                }
                
                // Build PolymorphicInfo if polymorphic configuration is specified
                PolymorphicInfo? polymorphicConfig = null;
                var typeKey = typeArg.ToDisplayString();
                
                // Check for derived type mappings from YamlDerivedTypeMappingAttribute
                if (typeDiscriminatorPropertyName is not null && 
                    derivedTypeMappingsFromAttrs.TryGetValue(typeKey, out var mappingsFromAttr) && 
                    mappingsFromAttr.Count > 0)
                {
                    // Use mappings from YamlDerivedTypeMappingAttribute
                    polymorphicConfig = new PolymorphicInfo(typeDiscriminatorPropertyName, mappingsFromAttr);
                }
                else if (typeDiscriminatorPropertyName is not null && derivedTypes is not null && derivedTypes.Count > 0)
                {
                    // Use inline DerivedTypes/DerivedTypeDiscriminators arrays
                    var derivedTypeMappings = new List<(string Discriminator, INamedTypeSymbol DerivedType)>();
                    for (int i = 0; i < derivedTypes.Count; i++)
                    {
                        var derivedType = derivedTypes[i];
                        // Use explicit discriminator if provided, otherwise use type name
                        var discriminator = (derivedTypeDiscriminators is not null && i < derivedTypeDiscriminators.Count)
                            ? derivedTypeDiscriminators[i]
                            : derivedType.Name;
                        derivedTypeMappings.Add((discriminator, derivedType));
                    }
                    polymorphicConfig = new PolymorphicInfo(typeDiscriminatorPropertyName, derivedTypeMappings);
                }
                
                // Check if type has [YamlConverter] attribute for custom converter support
                INamedTypeSymbol? customConverterType = null;
                foreach (var typeAttr in typeArg.GetAttributes())
                {
                    if (typeAttr.AttributeClass?.ToDisplayString() == "Yamlify.Serialization.YamlConverterAttribute")
                    {
                        if (typeAttr.ConstructorArguments.Length > 0 &&
                            typeAttr.ConstructorArguments[0].Value is INamedTypeSymbol converterType)
                        {
                            customConverterType = converterType;
                        }
                        break;
                    }
                }
                
                typesToGenerate.Add(new TypeToGenerate(typeArg, typeOrdering, polymorphicConfig, customConverterType));
            }
            else if (attrName == "Yamlify.Serialization.YamlSourceGenerationOptionsAttribute")
            {
                // Parse PropertyOrdering and IndentSequenceItems from named arguments
                foreach (var namedArg in attributeData.NamedArguments)
                {
                    if (namedArg.Key == "PropertyOrdering" && namedArg.Value.Value is int orderingValue)
                    {
                        propertyOrdering = (PropertyOrderingMode)orderingValue;
                    }
                    else if (namedArg.Key == "IndentSequenceItems" && namedArg.Value.Value is bool indentSeqValue)
                    {
                        indentSequenceItems = indentSeqValue;
                    }
                    else if (namedArg.Key == "IgnoreNullValues" && namedArg.Value.Value is bool ignoreNullValue)
                    {
                        ignoreNullValues = ignoreNullValue;
                    }
                    else if (namedArg.Key == "IgnoreEmptyObjects" && namedArg.Value.Value is bool ignoreEmptyValue)
                    {
                        ignoreEmptyObjects = ignoreEmptyValue;
                    }
                    else if (namedArg.Key == "DiscriminatorPosition" && namedArg.Value.Value is int discPosValue)
                    {
                        discriminatorPosition = (DiscriminatorPositionMode)discPosValue;
                    }
                }
            }
        }

        if (typesToGenerate.Count == 0)
        {
            return null;
        }

        return new ContextToGenerate(
            classSymbol.Name,
            classSymbol.ContainingNamespace.ToDisplayString(),
            typesToGenerate,
            propertyOrdering,
            indentSequenceItems,
            ignoreNullValues,
            ignoreEmptyObjects,
            discriminatorPosition);
    }

    private static readonly DiagnosticDescriptor AlphabeticalOrderingConflict = new(
        id: "YAML001",
        title: "Property ordering conflict",
        messageFormat: "Property '{0}' in type '{1}' has [YamlPropertyOrder] attribute, but the serializer context uses alphabetical property ordering. Remove the attribute or switch to declaration order.",
        category: "Yamlify",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static void Execute(
        Compilation compilation, 
        ImmutableArray<ContextToGenerate> contexts, 
        SourceProductionContext spc)
    {
        if (contexts.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var ctx in contexts.Distinct())
        {
            // Validate: if alphabetical ordering, no property should have YamlPropertyOrder attribute
            if (ctx.PropertyOrdering == PropertyOrderingMode.Alphabetical)
            {
                foreach (var type in ctx.Types)
                {
                    foreach (var prop in GetAllProperties(type.Symbol))
                    {
                        if (HasPropertyOrderAttribute(prop))
                        {
                            var diagnostic = Diagnostic.Create(
                                AlphabeticalOrderingConflict,
                                prop.Locations.FirstOrDefault(),
                                prop.Name,
                                type.Symbol.Name);
                            spc.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }

            var source = GenerateContextSource(ctx, compilation);
            spc.AddSource($"{ctx.ClassName}.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GenerateContextSource(ContextToGenerate ctx, Compilation compilation)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// This file was generated by Yamlify.SourceGenerator.");
        sb.AppendLine("// It is AOT-compatible and uses no reflection at runtime.");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Yamlify;");
        sb.AppendLine("using Yamlify.Serialization;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(ctx.Namespace))
        {
            sb.AppendLine($"namespace {ctx.Namespace};");
            sb.AppendLine();
        }

        sb.AppendLine($"partial class {ctx.ClassName}");
        sb.AppendLine("{");
        
        // Generate constructor that configures options if any non-default settings
        var hasNonDefaultOptions = !ctx.IndentSequenceItems || ctx.IgnoreNullValues || ctx.IgnoreEmptyObjects;
        if (hasNonDefaultOptions)
        {
            var optionsList = new List<string>();
            if (!ctx.IndentSequenceItems)
            {
                optionsList.Add("IndentSequenceItems = false");
            }
            if (ctx.IgnoreNullValues)
            {
                optionsList.Add("IgnoreNullValues = true");
            }
            if (ctx.IgnoreEmptyObjects)
            {
                optionsList.Add("IgnoreEmptyObjects = true");
            }
            var optionsStr = string.Join(", ", optionsList);
            sb.AppendLine($"    public {ctx.ClassName}() : base(new YamlSerializerOptions {{ {optionsStr} }})");
            sb.AppendLine("    {");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        
        // Generate static Default singleton
        sb.AppendLine($"    private static {ctx.ClassName}? _default;");
        sb.AppendLine($"    public static {ctx.ClassName} Default => _default ??= new {ctx.ClassName}();");
        sb.AppendLine();

        // Build a map of property names to handle collisions
        var propertyNameMap = BuildPropertyNameMap(ctx.Types);

        // Generate type info properties
        foreach (var type in ctx.Types)
        {
            var propertyName = propertyNameMap[type.Symbol.ToDisplayString()];
            var fullTypeName = type.Symbol.ToDisplayString();
            
            sb.AppendLine($"    private YamlTypeInfo<{fullTypeName}>? _{propertyName.ToLowerInvariant()};");
            sb.AppendLine($"    public YamlTypeInfo<{fullTypeName}> {propertyName} => _{propertyName.ToLowerInvariant()} ??= Create{propertyName}TypeInfo();");
            sb.AppendLine();
        }

        // Generate GetTypeInfo override
        sb.AppendLine("    public override YamlTypeInfo? GetTypeInfo(Type type, YamlSerializerOptions options)");
        sb.AppendLine("    {");
        
        foreach (var type in ctx.Types)
        {
            var fullTypeName = type.Symbol.ToDisplayString();
            var propertyName = propertyNameMap[fullTypeName];
            sb.AppendLine($"        if (type == typeof({fullTypeName})) return {propertyName};");
        }
        
        sb.AppendLine("        return null;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate type info creation methods
        foreach (var type in ctx.Types)
        {
            GenerateTypeInfoMethod(sb, type, propertyNameMap, compilation);
        }

        // Generate converter classes
        foreach (var type in ctx.Types)
        {
            // Use per-type ordering if specified, otherwise fall back to context-level ordering
            var effectiveOrdering = type.PropertyOrdering ?? ctx.PropertyOrdering;
            GenerateConverterClass(sb, type, ctx.Types, compilation, effectiveOrdering, ctx.DiscriminatorPosition, ctx.IgnoreEmptyObjects);
        }

        // Generate IsEmpty helper methods for IgnoreEmptyObjects support
        if (ctx.IgnoreEmptyObjects)
        {
            sb.AppendLine("    // IsEmpty helper methods for IgnoreEmptyObjects support");
            foreach (var type in ctx.Types)
            {
                GenerateIsEmptyMethod(sb, type);
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateTypeInfoMethod(StringBuilder sb, TypeToGenerate type, Dictionary<string, string> propertyNameMap, Compilation compilation)
    {
        var propertyName = propertyNameMap[type.Symbol.ToDisplayString()];
        var fullTypeName = type.Symbol.ToDisplayString();
        var converterName = GetConverterName(type.Symbol);
        var hasCustomConverter = type.CustomConverterType is not null;
        
        sb.AppendLine($"    private YamlTypeInfo<{fullTypeName}> Create{propertyName}TypeInfo()");
        sb.AppendLine("    {");
        
        if (hasCustomConverter)
        {
            // Use the custom converter with GeneratedRead/GeneratedWrite delegates set via object initializer
            var customConverterTypeName = type.CustomConverterType!.ToDisplayString();
            sb.AppendLine($"        var converter = new {customConverterTypeName}");
            sb.AppendLine("        {");
            sb.AppendLine($"            GeneratedRead = {converterName}.ReadCore,");
            sb.AppendLine($"            GeneratedWrite = {converterName}.WriteCore");
            sb.AppendLine("        };");
        }
        else
        {
            sb.AppendLine($"        var converter = new {converterName}();");
        }
        
        sb.AppendLine($"        var properties = new List<YamlPropertyInfo>();");
        sb.AppendLine();

        // Add property metadata (including inherited properties)
        foreach (var prop in GetAllProperties(type.Symbol))
        {
            var propName = prop.Name;
            var yamlName = ToKebabCase(propName);
            var propTypeName = prop.Type.ToDisplayString();

            sb.AppendLine($"        properties.Add(new YamlPropertyInfo<{fullTypeName}, {propTypeName}>(");
            sb.AppendLine($"            name: \"{propName}\",");
            sb.AppendLine($"            serializedName: \"{yamlName}\",");
            
            if (prop.GetMethod is not null)
            {
                sb.AppendLine($"            getter: static obj => obj.{propName},");
            }
            else
            {
                sb.AppendLine($"            getter: null,");
            }
            
            // Init-only properties cannot have setters (they must be set in object initializer/constructor)
            if (prop.SetMethod is not null 
                && prop.SetMethod.DeclaredAccessibility == Accessibility.Public
                && !prop.SetMethod.IsInitOnly)
            {
                sb.AppendLine($"            setter: static (obj, value) => obj.{propName} = value));");
            }
            else
            {
                sb.AppendLine($"            setter: null));");
            }
            sb.AppendLine();
        }

        // Check if type has parameterless constructor and no required members
        var hasParameterlessConstructor = type.Symbol.Constructors
            .Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);
        
        // Check for required members including inherited (cannot use default CreateInstance)
        var hasRequiredMembers = GetAllProperties(type.Symbol)
            .Any(p => p.IsRequired);

        sb.AppendLine($"        return new YamlTypeInfo<{fullTypeName}>(converter, properties, Options)");
        sb.AppendLine("        {");
        
        // Only generate CreateInstance if no required members
        if (hasParameterlessConstructor && !hasRequiredMembers)
        {
            sb.AppendLine($"            CreateInstance = static () => new {fullTypeName}(),");
        }
        
        // For custom converters, use the custom converter's methods (which may delegate to generated code)
        if (hasCustomConverter)
        {
            sb.AppendLine($"            SerializeAction = (writer, value, options) => converter.Write(writer, value, options),");
            sb.AppendLine($"            DeserializeFunc = (ref Utf8YamlReader reader, YamlSerializerOptions options) => converter.Read(ref reader, options)");
        }
        else
        {
            sb.AppendLine($"            SerializeAction = static (writer, value, options) => new {converterName}().Write(writer, value, options),");
            sb.AppendLine($"            DeserializeFunc = static (ref Utf8YamlReader reader, YamlSerializerOptions options) => new {converterName}().Read(ref reader, options)");
        }
        
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateConverterClass(StringBuilder sb, TypeToGenerate type, IReadOnlyList<TypeToGenerate> allTypes, Compilation compilation, PropertyOrderingMode propertyOrdering, DiscriminatorPositionMode discriminatorPosition, bool ignoreEmptyObjects)
    {
        var converterName = GetConverterName(type.Symbol);
        var fullTypeName = type.Symbol.ToDisplayString();

        // Check if this type has a custom converter - if so, generate static methods instead
        if (type.CustomConverterType is not null)
        {
            GenerateStaticConverterMethods(sb, type, allTypes, compilation, propertyOrdering, discriminatorPosition, ignoreEmptyObjects);
            return;
        }

        sb.AppendLine($"    private sealed class {converterName} : YamlConverter<{fullTypeName}>");
        sb.AppendLine("    {");
        
        // Check if this is a polymorphic base type (from [YamlSerializable] or [YamlPolymorphic] attributes)
        var polyInfo = GetPolymorphicInfoForType(type);
        var isPolymorphicBase = polyInfo is not null && polyInfo.DerivedTypes.Count > 0;
        
        // Read method
        GenerateReadMethod(sb, type, allTypes, compilation);
        sb.AppendLine();

        // Write method - use polymorphic dispatch for types with [YamlPolymorphic] and derived types
        if (isPolymorphicBase)
        {
            GeneratePolymorphicWriteMethod(sb, type, polyInfo!, allTypes, compilation);
        }
        else
        {
            GenerateWriteMethod(sb, type, allTypes, compilation, propertyOrdering, discriminatorPosition, ignoreEmptyObjects);
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates static ReadCore and WriteCore methods for types with custom converters.
    /// These methods are wired up to the GeneratedRead/GeneratedWrite delegates on the custom converter.
    /// We generate a full converter class internally and wrap calls to it via static methods.
    /// </summary>
    private static void GenerateStaticConverterMethods(StringBuilder sb, TypeToGenerate type, IReadOnlyList<TypeToGenerate> allTypes, Compilation compilation, PropertyOrderingMode propertyOrdering, DiscriminatorPositionMode discriminatorPosition, bool ignoreEmptyObjects)
    {
        var converterName = GetConverterName(type.Symbol);
        var fullTypeName = type.Symbol.ToDisplayString();
        var isValueType = type.Symbol.IsValueType;
        var nullableAnnotation = isValueType ? "" : "?";
        
        // Check if this is a polymorphic base type
        var polyInfo = GetPolymorphicInfoForType(type);
        var isPolymorphicBase = polyInfo is not null && polyInfo.DerivedTypes.Count > 0;

        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Generated converter for {type.Symbol.Name}. Since a custom converter exists ({type.CustomConverterType!.ToDisplayString()}),");
        sb.AppendLine($"    /// static ReadCore/WriteCore methods are provided for delegation from the custom converter.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    private sealed class {converterName} : YamlConverter<{fullTypeName}>");
        sb.AppendLine("    {");
        
        // Instance field for lazy initialization
        sb.AppendLine($"        private static {converterName}? _instance;");
        sb.AppendLine($"        private static {converterName} Instance => _instance ??= new {converterName}();");
        sb.AppendLine();
        
        // Generate static ReadCore method that delegates to instance Read
        sb.AppendLine($"        public static {fullTypeName}{nullableAnnotation} ReadCore(ref Utf8YamlReader reader, YamlSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            return Instance.Read(ref reader, options);");
        sb.AppendLine("        }");
        sb.AppendLine();
        
        // Generate static WriteCore method that delegates to instance Write
        sb.AppendLine($"        public static void WriteCore(Utf8YamlWriter writer, {fullTypeName} value, YamlSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            Instance.Write(writer, value, options);");
        sb.AppendLine("        }");
        sb.AppendLine();
        
        // Read method
        GenerateReadMethod(sb, type, allTypes, compilation);
        sb.AppendLine();

        // Write method - use polymorphic dispatch for types with [YamlPolymorphic] and derived types
        if (isPolymorphicBase)
        {
            GeneratePolymorphicWriteMethod(sb, type, polyInfo!, allTypes, compilation);
        }
        else
        {
            GenerateWriteMethod(sb, type, allTypes, compilation, propertyOrdering, discriminatorPosition, ignoreEmptyObjects);
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateReadMethod(StringBuilder sb, TypeToGenerate type, IReadOnlyList<TypeToGenerate> allTypes, Compilation compilation)
    {
        var typeName = type.Symbol.Name;
        var fullTypeName = type.Symbol.ToDisplayString();
        
        // For value types (structs), the return type must not include ? because:
        // - The base class has T? which for value types means Nullable<T>
        // - When overriding, we must match the signature exactly
        // For reference types, T? is just an annotation and we can use it
        var isValueType = type.Symbol.IsValueType;
        var nullableAnnotation = isValueType ? "" : "?";

        sb.AppendLine($"        public override {fullTypeName}{nullableAnnotation} Read(ref Utf8YamlReader reader, YamlSerializerOptions options)");
        sb.AppendLine("        {");
        
        // Special handling for enums - they are scalar values, not mappings
        if (type.Symbol.TypeKind == TypeKind.Enum)
        {
            sb.AppendLine($"            var stringValue = reader.GetString();");
            sb.AppendLine("            reader.Read();");
            sb.AppendLine($"            if (System.Enum.TryParse<{fullTypeName}>(stringValue, true, out var enumValue))");
            sb.AppendLine("            {");
            sb.AppendLine("                return enumValue;");
            sb.AppendLine("            }");
            sb.AppendLine("            return default;");
            sb.AppendLine("        }");
            return;
        }
        
        // Special handling for collection types (List<T>, T[], etc.) at root level
        if (IsListOrArray(type.Symbol, out var elementType, out var isHashSet))
        {
            GenerateRootCollectionRead(sb, type.Symbol, elementType!, isHashSet, allTypes);
            return;
        }
        
        // Special handling for dictionary types at root level
        if (IsDictionary(type.Symbol, out var keyType, out var valueType))
        {
            GenerateRootDictionaryRead(sb, type.Symbol, keyType!, valueType!, allTypes);
            return;
        }
        
        sb.AppendLine("            if (reader.TokenType != YamlTokenType.MappingStart)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Skip unexpected token to prevent infinite loops when reading collections");
        sb.AppendLine("                reader.Skip();");
        sb.AppendLine("                return default;");
        sb.AppendLine("            }");
        sb.AppendLine();
        
        // Check if this type is polymorphic (from [YamlSerializable] or [YamlPolymorphic] attributes)
        var polyInfo = GetPolymorphicInfoForType(type);
        if (polyInfo is { DerivedTypes.Count: > 0 } && (type.Symbol.IsAbstract || type.Symbol.TypeKind == TypeKind.Interface))
        {
            // Generate polymorphic read - dispatch to correct derived type based on discriminator
            GeneratePolymorphicRead(sb, type, polyInfo, allTypes, fullTypeName);
            sb.AppendLine("        }");
            return;
        }
        else if (polyInfo is { DerivedTypes.Count: > 0 })
        {
            // Non-abstract base type with derived types - still needs polymorphic dispatch
            // but must also handle the case where the base type itself is serialized
            GeneratePolymorphicReadWithBaseType(sb, type, polyInfo, allTypes, fullTypeName);
            sb.AppendLine("        }");
            return;
        }
        
        // Collect all public readable properties including inherited (for reading values from YAML)
        // Exclude properties with YamlIgnore attribute
        var allReadableProperties = GetAllProperties(type.Symbol)
            .Where(p => p.GetMethod is not null && !ShouldIgnoreProperty(p))
            .ToList();
        
        // Collect settable properties (for object initializer syntax)
        // Include both regular setters and init-only setters
        var settableProperties = allReadableProperties
            .Where(p => p.SetMethod is not null 
                     && p.SetMethod.DeclaredAccessibility == Accessibility.Public)
            .ToList();
        
        // Init-only properties must be set in object initializer, not via assignment
        var initOnlyProperties = settableProperties
            .Where(p => p.SetMethod!.IsInitOnly)
            .ToList();
        
        // Regular settable properties can be set after construction
        var regularSettableProperties = settableProperties
            .Where(p => !p.SetMethod!.IsInitOnly)
            .ToList();

        // Find the constructor we'll use (to extract default parameter values)
        var constructors = type.Symbol.Constructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
            .OrderByDescending(c => c.Parameters.Length)
            .ToList();
        
        var parameterlessConstructor = constructors.FirstOrDefault(c => c.Parameters.Length == 0);
        var primaryConstructor = parameterlessConstructor is null ? constructors.FirstOrDefault() : null;
        
        // Check if any properties have the 'required' modifier - these must always be set
        var hasRequiredProperties = settableProperties.Any(p => p.IsRequired);
        
        // Only use the "preserve defaults" optimization if we have a parameterless constructor
        // AND no required properties (can't create defaults object with required properties)
        var canPreserveDefaults = parameterlessConstructor is not null && !hasRequiredProperties;
        
        // Build a dictionary of property name -> default value from constructor parameters
        var constructorDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (primaryConstructor is not null)
        {
            foreach (var param in primaryConstructor.Parameters)
            {
                if (param.HasExplicitDefaultValue)
                {
                    var defaultValueStr = GetExplicitDefaultValueString(param);
                    constructorDefaults[param.Name] = defaultValueStr;
                }
            }
        }

        // Declare variables for ALL readable properties (we need to capture values for constructor params too)
        foreach (var prop in allReadableProperties)
        {
            var propName = prop.Name;
            var propTypeName = prop.Type.ToDisplayString();
            
            // Use constructor default if available, otherwise use type default
            var defaultValue = constructorDefaults.TryGetValue(propName, out var ctorDefault) 
                ? ctorDefault 
                : GetDefaultValue(prop.Type);
            
            sb.AppendLine($"            {propTypeName} _{propName.ToLowerInvariant()} = {defaultValue};");
        }
        
        // For types where we can preserve defaults, track which settable properties were actually set from YAML
        // This allows the object's property initializers/constructor defaults to be preserved for properties not in YAML
        if (canPreserveDefaults)
        {
            foreach (var prop in settableProperties)
            {
                sb.AppendLine($"            bool _has{prop.Name} = false;");
            }
        }
        sb.AppendLine();

        sb.AppendLine("            reader.Read(); // Move past MappingStart");
        sb.AppendLine();
        sb.AppendLine("            while (reader.TokenType != YamlTokenType.MappingEnd && reader.TokenType != YamlTokenType.None)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (reader.TokenType != YamlTokenType.Scalar)");
        sb.AppendLine("                {");
        sb.AppendLine("                    reader.Read();");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                var propertyName = reader.GetString();");
        sb.AppendLine("                reader.Read();");
        sb.AppendLine();
        sb.AppendLine("                switch (propertyName)");
        sb.AppendLine("                {");

        // Generate cases for ALL readable properties (not just settable)
        foreach (var prop in allReadableProperties)
        {
            var propName = prop.Name;
            var yamlName = GetYamlPropertyName(prop);

            sb.AppendLine($"                    case \"{yamlName}\":");
            // Also accept the original property name and kebab-case as fallbacks
            var kebabName = ToKebabCase(propName);
            if (yamlName != propName && yamlName != propName.ToLowerInvariant())
            {
                sb.AppendLine($"                    case \"{propName}\":");
            }
            if (yamlName != kebabName && kebabName != propName.ToLowerInvariant())
            {
                sb.AppendLine($"                    case \"{kebabName}\":");
            }
            
            // Check for sibling discriminator - for simple types, the discriminator applies to the property itself
            // For dictionaries, the discriminator applies to the value type (polymorphic dictionary values)
            SiblingDiscriminatorInfo? siblingInfo = null;
            if (!IsListOrArray(prop.Type, out _, out _))
            {
                siblingInfo = GetSiblingDiscriminatorInfo(prop);
            }
            GeneratePropertyRead(sb, propName, prop.Type, allTypes, siblingInfo);
            
            // For types where we can preserve defaults, mark that this settable property was actually set from YAML
            // For reference types, only mark as "has value" if the deserialized value is not null
            // This ensures that `property:` (empty/null in YAML) preserves the class default value
            if (canPreserveDefaults && settableProperties.Contains(prop))
            {
                var varName = $"_{propName.ToLowerInvariant()}";
                if (prop.Type.IsValueType)
                {
                    // Value types are always "set"
                    sb.AppendLine($"                        _has{propName} = true;");
                }
                else
                {
                    // Reference types - only mark as set if not null
                    sb.AppendLine($"                        _has{propName} = {varName} is not null;");
                }
            }
            
            sb.AppendLine("                        break;");
        }

        sb.AppendLine("                    default:");
        sb.AppendLine("                        reader.Skip();");
        sb.AppendLine("                        break;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            reader.Read(); // Move past MappingEnd");
        sb.AppendLine();

        // Create instance - reuse constructor info from earlier
        if (canPreserveDefaults)
        {
            // Use parameterless constructor with "preserve defaults" optimization
            // Both init-only and regular settable properties: only set if they were actually found in YAML
            // This preserves the class's default property values (from constructor or property initializers)
            
            if (initOnlyProperties.Count > 0)
            {
                sb.AppendLine($"            // Create temporary object to capture default values from parameterless constructor");
                sb.AppendLine($"            var _defaults = new {fullTypeName}();");
                
                sb.AppendLine($"            var result = new {fullTypeName}");
                sb.AppendLine("            {");
                for (int i = 0; i < initOnlyProperties.Count; i++)
                {
                    var prop = initOnlyProperties[i];
                    var comma = i < initOnlyProperties.Count - 1 ? "," : "";
                    // Use ternary to pick between YAML value and constructor default
                    sb.AppendLine($"                {prop.Name} = _has{prop.Name} ? _{prop.Name.ToLowerInvariant()} : _defaults.{prop.Name}{comma}");
                }
                sb.AppendLine("            };");
            }
            else
            {
                sb.AppendLine($"            var result = new {fullTypeName}();");
            }
            
            // Regular properties - only set if present in YAML to preserve class default values
            foreach (var prop in regularSettableProperties)
            {
                sb.AppendLine($"            if (_has{prop.Name})");
                sb.AppendLine("            {");
                sb.AppendLine($"                result.{prop.Name} = _{prop.Name.ToLowerInvariant()};");
                sb.AppendLine("            }");
            }
            sb.AppendLine("            return result;");
        }
        else if (parameterlessConstructor is not null)
        {
            // Use parameterless constructor but can't preserve defaults (has required properties)
            // Must set all properties in object initializer
            if (settableProperties.Count > 0)
            {
                sb.AppendLine($"            return new {fullTypeName}");
                sb.AppendLine("            {");
                for (int i = 0; i < settableProperties.Count; i++)
                {
                    var prop = settableProperties[i];
                    var comma = i < settableProperties.Count - 1 ? "," : "";
                    sb.AppendLine($"                {prop.Name} = _{prop.Name.ToLowerInvariant()}{comma}");
                }
                sb.AppendLine("            };");
            }
            else
            {
                sb.AppendLine($"            return new {fullTypeName}();");
            }
        }
        else
        {
            // Use primary constructor with matching parameters
            if (primaryConstructor is not null)
            {
                var args = new List<string>();
                var usedPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                foreach (var param in primaryConstructor.Parameters)
                {
                    // Find matching property from ALL readable properties (not just settable)
                    var matchingProp = allReadableProperties
                        .FirstOrDefault(p => p.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingProp is not null)
                    {
                        args.Add($"_{matchingProp.Name.ToLowerInvariant()}");
                        usedPropertyNames.Add(matchingProp.Name);
                    }
                    else
                    {
                        args.Add(GetDefaultValue(param.Type));
                    }
                }
                
                // Find settable properties that aren't covered by constructor parameters
                var additionalSettableProps = settableProperties
                    .Where(p => !usedPropertyNames.Contains(p.Name))
                    .ToList();
                
                if (additionalSettableProps.Count > 0)
                {
                    // Use object initializer syntax after constructor call
                    sb.AppendLine($"            return new {fullTypeName}({string.Join(", ", args)})");
                    sb.AppendLine("            {");
                    for (int i = 0; i < additionalSettableProps.Count; i++)
                    {
                        var prop = additionalSettableProps[i];
                        var comma = i < additionalSettableProps.Count - 1 ? "," : "";
                        sb.AppendLine($"                {prop.Name} = _{prop.Name.ToLowerInvariant()}{comma}");
                    }
                    sb.AppendLine("            };");
                }
                else
                {
                    sb.AppendLine($"            return new {fullTypeName}({string.Join(", ", args)});");
                }
            }
            else
            {
                sb.AppendLine($"            return default;");
            }
        }
        
        sb.AppendLine("        }");
    }

    /// <summary>
    /// Generates polymorphic read code using the combined converter approach.
    /// This reads ALL properties from ALL derived types in a single pass,
    /// then constructs the correct concrete type based on the discriminator value.
    /// </summary>
    private static void GeneratePolymorphicRead(
        StringBuilder sb, 
        TypeToGenerate type, 
        PolymorphicInfo polyInfo, 
        IReadOnlyList<TypeToGenerate> allTypes,
        string fullTypeName)
    {
        // Collect all properties from all derived types
        // Key: PropertyName -> (VariableName, TypeSymbol, List of YamlNames)
        var allProperties = new Dictionary<string, (string VarName, ITypeSymbol PropType, HashSet<string> YamlNames)>(StringComparer.OrdinalIgnoreCase);
        var discriminatorPropertyName = polyInfo.TypeDiscriminatorPropertyName;
        
        // Get properties from the base type first
        foreach (var prop in GetAllProperties(type.Symbol).Where(p => p.GetMethod is not null && !ShouldIgnoreProperty(p)))
        {
            var varName = $"_{prop.Name.ToLowerInvariant()}";
            var yamlNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                GetYamlPropertyName(prop),
                prop.Name,
                ToKebabCase(prop.Name)
            };
            allProperties[prop.Name] = (varName, prop.Type, yamlNames);
        }
        
        // Collect properties from all derived types
        foreach (var (_, derivedTypeSymbol) in polyInfo.DerivedTypes)
        {
            foreach (var prop in GetAllProperties(derivedTypeSymbol).Where(p => p.GetMethod is not null && !ShouldIgnoreProperty(p)))
            {
                if (!allProperties.ContainsKey(prop.Name))
                {
                    var varName = $"_{prop.Name.ToLowerInvariant()}";
                    var yamlNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        GetYamlPropertyName(prop),
                        prop.Name,
                        ToKebabCase(prop.Name)
                    };
                    allProperties[prop.Name] = (varName, prop.Type, yamlNames);
                }
                else
                {
                    // Merge yaml names
                    var existing = allProperties[prop.Name];
                    existing.YamlNames.Add(GetYamlPropertyName(prop));
                    existing.YamlNames.Add(prop.Name);
                    existing.YamlNames.Add(ToKebabCase(prop.Name));
                }
            }
        }
        
        // Declare the discriminator variable
        sb.AppendLine($"            string? _discriminator = null;");
        
        // Declare variables for all properties with tracking flags for default preservation
        foreach (var kvp in allProperties)
        {
            var propInfo = kvp.Value;
            var propTypeName = propInfo.PropType.ToDisplayString();
            var defaultValue = GetDefaultValue(propInfo.PropType);
            sb.AppendLine($"            {propTypeName} {propInfo.VarName} = {defaultValue};");
            sb.AppendLine($"            bool _has{kvp.Key} = false;");
        }
        sb.AppendLine();

        sb.AppendLine("            reader.Read(); // Move past MappingStart");
        sb.AppendLine();
        sb.AppendLine("            while (reader.TokenType != YamlTokenType.MappingEnd && reader.TokenType != YamlTokenType.None)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (reader.TokenType != YamlTokenType.Scalar)");
        sb.AppendLine("                {");
        sb.AppendLine("                    reader.Read();");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                var propertyName = reader.GetString();");
        sb.AppendLine("                reader.Read();");
        sb.AppendLine();
        
        // Handle discriminator first
        sb.AppendLine($"                if (propertyName == \"{discriminatorPropertyName}\")");
        sb.AppendLine("                {");
        sb.AppendLine("                    _discriminator = reader.GetString();");
        sb.AppendLine("                    reader.Read();");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine();
        
        sb.AppendLine("                switch (propertyName)");
        sb.AppendLine("                {");

        // Generate cases for ALL properties from all derived types (with has-tracking)
        foreach (var kvp in allProperties)
        {
            var propName = kvp.Key;
            var propInfo = kvp.Value;
            
            foreach (var yamlName in propInfo.YamlNames.Distinct())
            {
                sb.AppendLine($"                    case \"{yamlName}\":");
            }
            
            GeneratePropertyRead(sb, propName, propInfo.PropType, allTypes);
            sb.AppendLine($"                        _has{propName} = true;");
            
            sb.AppendLine("                        break;");
        }

        sb.AppendLine("                    default:");
        sb.AppendLine("                        reader.Skip();");
        sb.AppendLine("                        break;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            reader.Read(); // Move past MappingEnd");
        sb.AppendLine();
        
        // Create defaults instances for derived types with parameterless constructors
        // to preserve default property values from the type definition
        foreach (var (discriminator, derivedTypeSymbol) in polyInfo.DerivedTypes)
        {
            var constructors = derivedTypeSymbol.Constructors
                .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
                .ToList();
            
            var hasParameterlessConstructor = constructors.Any(c => c.Parameters.Length == 0);
            if (hasParameterlessConstructor)
            {
                var derivedFullTypeName = derivedTypeSymbol.ToDisplayString();
                var safeTypeName = derivedTypeSymbol.Name.Replace(".", "_");
                sb.AppendLine($"            var _defaults{safeTypeName} = new {derivedFullTypeName}();");
            }
        }
        sb.AppendLine();
        
        // Generate if/else chain on discriminator to construct the correct concrete type
        // Using if/else with StringComparison.OrdinalIgnoreCase for case-insensitive matching
        var isFirst = true;
        foreach (var (discriminator, derivedTypeSymbol) in polyInfo.DerivedTypes)
        {
            var derivedFullTypeName = derivedTypeSymbol.ToDisplayString();
            var safeTypeName = derivedTypeSymbol.Name.Replace(".", "_");
            
            // Get all settable properties for this derived type
            var derivedProperties = GetAllProperties(derivedTypeSymbol)
                .Where(p => p.GetMethod is not null && p.SetMethod is not null 
                         && p.SetMethod.DeclaredAccessibility == Accessibility.Public
                         && !ShouldIgnoreProperty(p))
                .ToList();
            
            // Get constructor info for this derived type
            var constructors = derivedTypeSymbol.Constructors
                .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
                .OrderByDescending(c => c.Parameters.Length)
                .ToList();
            
            var parameterlessConstructor = constructors.FirstOrDefault(c => c.Parameters.Length == 0);
            var primaryConstructor = parameterlessConstructor is null ? constructors.FirstOrDefault() : null;
            
            var ifKeyword = isFirst ? "if" : "else if";
            sb.AppendLine($"            {ifKeyword} (string.Equals(_discriminator, \"{discriminator}\", System.StringComparison.OrdinalIgnoreCase))");
            sb.AppendLine("            {");
            
            if (parameterlessConstructor is not null)
            {
                // Use object initializer with conditional default fallback
                sb.Append($"                return new {derivedFullTypeName} {{ ");
                var propsToSet = derivedProperties.Select(p => 
                    $"{p.Name} = _has{p.Name} ? _{p.Name.ToLowerInvariant()} : _defaults{safeTypeName}.{p.Name}");
                sb.Append(string.Join(", ", propsToSet));
                sb.AppendLine(" };");
            }
            else if (primaryConstructor is not null)
            {
                // Use primary constructor
                var args = primaryConstructor.Parameters
                    .Select(param =>
                    {
                        var matchingProp = derivedProperties
                            .FirstOrDefault(p => p.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase));
                        return matchingProp is not null 
                            ? $"_{matchingProp.Name.ToLowerInvariant()}" 
                            : GetDefaultValue(param.Type);
                    });
                
                // Find additional properties not covered by constructor
                var usedNames = new HashSet<string>(primaryConstructor.Parameters.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
                var additionalProps = derivedProperties.Where(p => !usedNames.Contains(p.Name)).ToList();
                
                if (additionalProps.Count > 0)
                {
                    sb.Append($"                return new {derivedFullTypeName}({string.Join(", ", args)}) {{ ");
                    var propsToSet = additionalProps.Select(p => $"{p.Name} = _{p.Name.ToLowerInvariant()}");
                    sb.Append(string.Join(", ", propsToSet));
                    sb.AppendLine(" };");
                }
                else
                {
                    sb.AppendLine($"                return new {derivedFullTypeName}({string.Join(", ", args)});");
                }
            }
            else
            {
                // No suitable constructor, return default
                sb.AppendLine($"                return default;");
            }
            
            sb.AppendLine("            }");
            isFirst = false;
        }
        
        // For abstract types/interfaces, throw exception for unknown or missing discriminator
        // since we can't instantiate the base type
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine($"                if (_discriminator is null)");
        sb.AppendLine("                {");
        sb.AppendLine($"                    throw new System.InvalidOperationException(\"Missing extension type discriminator '{discriminatorPropertyName}' for polymorphic deserialization of {fullTypeName}\");");
        sb.AppendLine("                }");
        sb.AppendLine($"                throw new System.InvalidOperationException($\"Unknown extension type: {{_discriminator}}\");");
        sb.AppendLine("            }");
    }

    /// <summary>
    /// Generates polymorphic read code for non-abstract base types.
    /// Similar to GeneratePolymorphicRead but includes a fallback case for the base type itself
    /// when no derived type discriminator matches or when the discriminator indicates the base type.
    /// </summary>
    private static void GeneratePolymorphicReadWithBaseType(
        StringBuilder sb, 
        TypeToGenerate type, 
        PolymorphicInfo polyInfo, 
        IReadOnlyList<TypeToGenerate> allTypes,
        string fullTypeName)
    {
        // Collect all properties from all derived types
        var allProperties = new Dictionary<string, (string VarName, ITypeSymbol PropType, HashSet<string> YamlNames)>(StringComparer.OrdinalIgnoreCase);
        var discriminatorPropertyName = polyInfo.TypeDiscriminatorPropertyName;
        
        // Get properties from the base type first
        foreach (var prop in GetAllProperties(type.Symbol).Where(p => p.GetMethod is not null && !ShouldIgnoreProperty(p)))
        {
            var varName = $"_{prop.Name.ToLowerInvariant()}";
            var yamlNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                GetYamlPropertyName(prop),
                prop.Name,
                ToKebabCase(prop.Name)
            };
            allProperties[prop.Name] = (varName, prop.Type, yamlNames);
        }
        
        // Collect properties from all derived types
        foreach (var (_, derivedTypeSymbol) in polyInfo.DerivedTypes)
        {
            foreach (var prop in GetAllProperties(derivedTypeSymbol).Where(p => p.GetMethod is not null && !ShouldIgnoreProperty(p)))
            {
                if (!allProperties.ContainsKey(prop.Name))
                {
                    var varName = $"_{prop.Name.ToLowerInvariant()}";
                    var yamlNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        GetYamlPropertyName(prop),
                        prop.Name,
                        ToKebabCase(prop.Name)
                    };
                    allProperties[prop.Name] = (varName, prop.Type, yamlNames);
                }
                else
                {
                    // Merge yaml names
                    var existing = allProperties[prop.Name];
                    existing.YamlNames.Add(GetYamlPropertyName(prop));
                    existing.YamlNames.Add(prop.Name);
                    existing.YamlNames.Add(ToKebabCase(prop.Name));
                }
            }
        }
        
        // Declare the discriminator variable
        sb.AppendLine($"            string? _discriminator = null;");
        
        // Declare variables for all properties with tracking flags for default preservation
        foreach (var kvp in allProperties)
        {
            var propInfo = kvp.Value;
            var propTypeName = propInfo.PropType.ToDisplayString();
            var defaultValue = GetDefaultValue(propInfo.PropType);
            sb.AppendLine($"            {propTypeName} {propInfo.VarName} = {defaultValue};");
            sb.AppendLine($"            bool _has{kvp.Key} = false;");
        }
        sb.AppendLine();

        sb.AppendLine("            reader.Read(); // Move past MappingStart");
        sb.AppendLine();
        sb.AppendLine("            while (reader.TokenType != YamlTokenType.MappingEnd && reader.TokenType != YamlTokenType.None)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (reader.TokenType != YamlTokenType.Scalar)");
        sb.AppendLine("                {");
        sb.AppendLine("                    reader.Read();");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                var propertyName = reader.GetString();");
        sb.AppendLine("                reader.Read();");
        sb.AppendLine();
        
        // Handle discriminator first
        sb.AppendLine($"                if (propertyName == \"{discriminatorPropertyName}\")");
        sb.AppendLine("                {");
        sb.AppendLine("                    _discriminator = reader.GetString();");
        sb.AppendLine("                    reader.Read();");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine();
        
        sb.AppendLine("                switch (propertyName)");
        sb.AppendLine("                {");

        // Generate cases for ALL properties from all derived types (with has-tracking)
        foreach (var kvp in allProperties)
        {
            var propName = kvp.Key;
            var propInfo = kvp.Value;
            
            foreach (var yamlName in propInfo.YamlNames.Distinct())
            {
                sb.AppendLine($"                    case \"{yamlName}\":");
            }
            
            GeneratePropertyRead(sb, propName, propInfo.PropType, allTypes);
            sb.AppendLine($"                        _has{propName} = true;");
            
            sb.AppendLine("                        break;");
        }

        sb.AppendLine("                    default:");
        sb.AppendLine("                        reader.Skip();");
        sb.AppendLine("                        break;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            reader.Read(); // Move past MappingEnd");
        sb.AppendLine();
        
        // Create defaults instances for derived types with parameterless constructors
        foreach (var (discriminator, derivedTypeSymbol) in polyInfo.DerivedTypes)
        {
            var constructors = derivedTypeSymbol.Constructors
                .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
                .ToList();
            
            var hasParameterlessConstructor = constructors.Any(c => c.Parameters.Length == 0);
            if (hasParameterlessConstructor)
            {
                var derivedFullTypeName = derivedTypeSymbol.ToDisplayString();
                var safeTypeName = derivedTypeSymbol.Name.Replace(".", "_");
                sb.AppendLine($"            var _defaults{safeTypeName} = new {derivedFullTypeName}();");
            }
        }
        
        // Also create a defaults instance for the base type if it has a parameterless constructor
        var baseHasParameterlessConstructor = type.Symbol.Constructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
            .Any(c => c.Parameters.Length == 0);
        if (baseHasParameterlessConstructor)
        {
            var safeBaseTypeName = type.Symbol.Name.Replace(".", "_");
            sb.AppendLine($"            var _defaults{safeBaseTypeName} = new {fullTypeName}();");
        }
        sb.AppendLine();
        
        // Generate if/else chain on discriminator to construct the correct concrete type
        // Using if/else with StringComparison.OrdinalIgnoreCase for case-insensitive matching
        var isFirst = true;
        foreach (var (discriminator, derivedTypeSymbol) in polyInfo.DerivedTypes)
        {
            var derivedFullTypeName = derivedTypeSymbol.ToDisplayString();
            var safeTypeName = derivedTypeSymbol.Name.Replace(".", "_");
            
            // Get all settable properties for this derived type
            var derivedProperties = GetAllProperties(derivedTypeSymbol)
                .Where(p => p.GetMethod is not null && p.SetMethod is not null 
                         && p.SetMethod.DeclaredAccessibility == Accessibility.Public
                         && !ShouldIgnoreProperty(p))
                .ToList();
            
            // Get constructor info for this derived type
            var constructors = derivedTypeSymbol.Constructors
                .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
                .OrderByDescending(c => c.Parameters.Length)
                .ToList();
            
            var parameterlessConstructor = constructors.FirstOrDefault(c => c.Parameters.Length == 0);
            var primaryConstructor = parameterlessConstructor is null ? constructors.FirstOrDefault() : null;
            
            var ifKeyword = isFirst ? "if" : "else if";
            sb.AppendLine($"            {ifKeyword} (string.Equals(_discriminator, \"{discriminator}\", System.StringComparison.OrdinalIgnoreCase))");
            sb.AppendLine("            {");
            
            if (parameterlessConstructor is not null)
            {
                // Use object initializer with conditional default fallback
                sb.Append($"                return new {derivedFullTypeName} {{ ");
                var propsToSet = derivedProperties.Select(p => 
                    $"{p.Name} = _has{p.Name} ? _{p.Name.ToLowerInvariant()} : _defaults{safeTypeName}.{p.Name}");
                sb.Append(string.Join(", ", propsToSet));
                sb.AppendLine(" };");
            }
            else if (primaryConstructor is not null)
            {
                // Use primary constructor
                var args = primaryConstructor.Parameters
                    .Select(param =>
                    {
                        var matchingProp = derivedProperties
                            .FirstOrDefault(p => p.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase));
                        return matchingProp is not null 
                            ? $"_{matchingProp.Name.ToLowerInvariant()}" 
                            : GetDefaultValue(param.Type);
                    });
                
                // Find additional properties not covered by constructor
                var usedNames = new HashSet<string>(primaryConstructor.Parameters.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
                var additionalProps = derivedProperties.Where(p => !usedNames.Contains(p.Name)).ToList();
                
                if (additionalProps.Count > 0)
                {
                    sb.Append($"                return new {derivedFullTypeName}({string.Join(", ", args)}) {{ ");
                    var propsToSet = additionalProps.Select(p => $"{p.Name} = _{p.Name.ToLowerInvariant()}");
                    sb.Append(string.Join(", ", propsToSet));
                    sb.AppendLine(" };");
                }
                else
                {
                    sb.AppendLine($"                return new {derivedFullTypeName}({string.Join(", ", args)});");
                }
            }
            else
            {
                // No suitable constructor, return default
                sb.AppendLine($"                return default;");
            }
            
            sb.AppendLine("            }");
            isFirst = false;
        }
        
        // For non-abstract base types, return an instance of the base type for unknown/missing discriminator
        // Get constructor info for the base type
        var baseConstructors = type.Symbol.Constructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
            .OrderByDescending(c => c.Parameters.Length)
            .ToList();
        
        var baseParamlessConstructor = baseConstructors.FirstOrDefault(c => c.Parameters.Length == 0);
        var basePrimaryConstructor = baseParamlessConstructor is null ? baseConstructors.FirstOrDefault() : null;
        
        // Get settable properties for the base type
        var baseProperties = GetAllProperties(type.Symbol)
            .Where(p => p.GetMethod is not null && p.SetMethod is not null 
                     && p.SetMethod.DeclaredAccessibility == Accessibility.Public
                     && !ShouldIgnoreProperty(p))
            .ToList();
        
        var safeBaseTypeName2 = type.Symbol.Name.Replace(".", "_");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        if (baseParamlessConstructor is not null)
        {
            // Use object initializer for base type with conditional default fallback
            sb.Append($"                return new {fullTypeName} {{ ");
            var propsToSet = baseProperties.Select(p => 
                $"{p.Name} = _has{p.Name} ? _{p.Name.ToLowerInvariant()} : _defaults{safeBaseTypeName2}.{p.Name}");
            sb.Append(string.Join(", ", propsToSet));
            sb.AppendLine(" };");
        }
        else if (basePrimaryConstructor is not null)
        {
            // Use primary constructor for base type
            var args = basePrimaryConstructor.Parameters
                .Select(param =>
                {
                    var matchingProp = baseProperties
                        .FirstOrDefault(p => p.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase));
                    return matchingProp is not null 
                        ? $"_{matchingProp.Name.ToLowerInvariant()}" 
                        : GetDefaultValue(param.Type);
                });
            
            // Find additional properties not covered by constructor
            var usedNames = new HashSet<string>(basePrimaryConstructor.Parameters.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
            var additionalProps = baseProperties.Where(p => !usedNames.Contains(p.Name)).ToList();
            
            if (additionalProps.Count > 0)
            {
                sb.Append($"                return new {fullTypeName}({string.Join(", ", args)}) {{ ");
                var propsToSet = additionalProps.Select(p => $"{p.Name} = _{p.Name.ToLowerInvariant()}");
                sb.Append(string.Join(", ", propsToSet));
                sb.AppendLine(" };");
            }
            else
            {
                sb.AppendLine($"                return new {fullTypeName}({string.Join(", ", args)});");
            }
        }
        else
        {
            sb.AppendLine($"                return default;");
        }
        sb.AppendLine("            }");
    }

    /// <summary>
    /// Generates a Write method for polymorphic base types that dispatches to derived type converters.
    /// </summary>
    private static void GeneratePolymorphicWriteMethod(
        StringBuilder sb, 
        TypeToGenerate type, 
        PolymorphicInfo polyInfo, 
        IReadOnlyList<TypeToGenerate> allTypes,
        Compilation compilation)
    {
        var typeName = type.Symbol.Name;
        var fullTypeName = type.Symbol.ToDisplayString();
        var isAbstractOrInterface = type.Symbol.IsAbstract || type.Symbol.TypeKind == TypeKind.Interface;

        sb.AppendLine($"        public override void Write(Utf8YamlWriter writer, {fullTypeName} value, YamlSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (value is null)");
        sb.AppendLine("            {");
        sb.AppendLine("                writer.WriteNull();");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            // Dispatch to derived type converter based on runtime type");
        sb.AppendLine("            // (ordered from most specific to least specific to ensure correct type matching)");
        
        // Sort derived types by inheritance depth (most specific first) to ensure correct pattern matching
        var sortedDerivedTypes = polyInfo.DerivedTypes
            .Select(dt => (dt.Discriminator, dt.DerivedType, Depth: GetInheritanceDepth(dt.DerivedType, type.Symbol)))
            .OrderByDescending(x => x.Depth)
            .ToList();
        
        bool first = true;
        foreach (var (discriminator, derivedType, _) in sortedDerivedTypes)
        {
            var derivedTypeInfo = allTypes.FirstOrDefault(t => 
                SymbolEqualityComparer.Default.Equals(t.Symbol, derivedType) ||
                t.Symbol.ToDisplayString() == derivedType.ToDisplayString());
            
            if (derivedTypeInfo is not null)
            {
                var derivedTypeName = derivedTypeInfo.Symbol.Name;
                var derivedConverterName = GetConverterName(derivedTypeInfo.Symbol);
                var derivedFullTypeName = derivedTypeInfo.Symbol.ToDisplayString();
                var keyword = first ? "if" : "else if";
                first = false;
                
                sb.AppendLine($"            {keyword} (value is {derivedFullTypeName} {derivedTypeName.ToLowerInvariant()}Value)");
                sb.AppendLine("            {");
                sb.AppendLine($"                new {derivedConverterName}().Write(writer, {derivedTypeName.ToLowerInvariant()}Value, options);");
                sb.AppendLine("            }");
            }
        }
        
        if (!first)
        {
            sb.AppendLine("            else");
            sb.AppendLine("            {");
            if (isAbstractOrInterface)
            {
                sb.AppendLine("                // Unknown derived type - write as null");
                sb.AppendLine("                writer.WriteNull();");
            }
            else
            {
                // For non-abstract base types, write the base type's own properties inline
                GeneratePolymorphicBaseTypeInlineWrite(sb, type, allTypes);
            }
            sb.AppendLine("            }");
        }
        else
        {
            // No derived types registered
            sb.AppendLine("            // No derived types registered - write as null");
            sb.AppendLine("            writer.WriteNull();");
        }
        
        sb.AppendLine("        }");
    }
    
    /// <summary>
    /// Generates inline property writes for the base type in polymorphic fallback.
    /// </summary>
    private static void GeneratePolymorphicBaseTypeInlineWrite(
        StringBuilder sb,
        TypeToGenerate type,
        IReadOnlyList<TypeToGenerate> allTypes)
    {
        var typeName = type.Symbol.Name;
        
        // Collect all public readable properties
        var allProperties = GetAllProperties(type.Symbol)
            .Where(p => p.GetMethod is not null && !ShouldIgnoreProperty(p))
            .ToList();
            
        // Order properties with YamlPropertyOrder first
        var orderedProperties = allProperties
            .Select(p => (Property: p, Order: GetPropertyOrder(p)))
            .OrderBy(x => x.Order)
            .ThenBy(x => GetSerializedPropertyName(x.Property))
            .Select(x => x.Property)
            .ToList();
        
        sb.AppendLine($"                // Base type {typeName} - write own properties");
        sb.AppendLine("                writer.WriteMappingStart();");
        
        foreach (var prop in orderedProperties)
        {
            var propName = GetSerializedPropertyName(prop);
            GeneratePropertyWrite(sb, prop.Name, prop.Type, allTypes, "                ");
        }
        
        sb.AppendLine("                writer.WriteMappingEnd();");
    }

    private static void GenerateWriteMethod(StringBuilder sb, TypeToGenerate type, IReadOnlyList<TypeToGenerate> allTypes, Compilation compilation, PropertyOrderingMode propertyOrdering, DiscriminatorPositionMode discriminatorPosition, bool ignoreEmptyObjects)
    {
        var typeName = type.Symbol.Name;
        var fullTypeName = type.Symbol.ToDisplayString();

        sb.AppendLine($"        public override void Write(Utf8YamlWriter writer, {fullTypeName} value, YamlSerializerOptions options)");
        sb.AppendLine("        {");
        
        // Special handling for enums - they are scalar values, not mappings
        if (type.Symbol.TypeKind == TypeKind.Enum)
        {
            sb.AppendLine("            writer.WriteString(value.ToString());");
            sb.AppendLine("        }");
            return;
        }
        
        // Special handling for collection types (List<T>, T[], etc.) at root level
        if (IsListOrArray(type.Symbol, out var elementType, out var isHashSet))
        {
            GenerateRootCollectionWrite(sb, type.Symbol, elementType!, allTypes);
            return;
        }
        
        // Special handling for dictionary types at root level
        if (IsDictionary(type.Symbol, out var keyType, out var valueType))
        {
            GenerateRootDictionaryWrite(sb, type.Symbol, keyType!, valueType!, allTypes);
            return;
        }
        
        sb.AppendLine("            writer.WriteMappingStart();");
        sb.AppendLine();

        // Check if this type has a polymorphic base that requires a discriminator
        // Pass allTypes to also check context-based polymorphic configurations
        var polyInfo = GetPolymorphicInfoWithInheritance(type.Symbol, allTypes);
        string? discriminatorPropertyName = null;
        string? discriminatorValue = null;
        bool discriminatorHasMatchingProperty = false;
        
        if (polyInfo is not null)
        {
            discriminatorValue = GetDiscriminatorForDerivedType(type.Symbol, polyInfo);
            if (discriminatorValue is not null)
            {
                discriminatorPropertyName = polyInfo.TypeDiscriminatorPropertyName;
            }
        }

        // Collect all properties
        var allProperties = GetAllProperties(type.Symbol)
            .Where(p => p.GetMethod is not null && !ShouldIgnoreProperty(p))
            .ToList();
        
        // Check if there's a property that matches the discriminator name
        if (discriminatorPropertyName is not null)
        {
            discriminatorHasMatchingProperty = allProperties.Any(p =>
            {
                var serializedName = GetExplicitYamlPropertyName(p) ?? ToKebabCase(p.Name);
                return string.Equals(serializedName, discriminatorPropertyName, StringComparison.OrdinalIgnoreCase);
            });
        }
        
        // Write discriminator at the beginning if:
        // 1. DiscriminatorPosition.First is configured, OR
        // 2. DiscriminatorPosition.PropertyOrder is configured but there's no matching property to attach the order to
        if (discriminatorPropertyName is not null && discriminatorValue is not null)
        {
            var shouldWriteDiscriminatorFirst = discriminatorPosition == DiscriminatorPositionMode.First 
                || (discriminatorPosition == DiscriminatorPositionMode.PropertyOrder && !discriminatorHasMatchingProperty);
            
            if (shouldWriteDiscriminatorFirst)
            {
                sb.AppendLine($"            writer.WritePropertyName(\"{discriminatorPropertyName}\");");
                sb.AppendLine($"            writer.WriteString(\"{discriminatorValue}\");");
                sb.AppendLine();
            }
        }
        
        // Find all discriminator property names (properties referenced by YamlSiblingDiscriminator)
        var discriminatorPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in allProperties)
        {
            var siblingInfo = GetSiblingDiscriminatorInfo(prop);
            if (siblingInfo is not null)
            {
                discriminatorPropertyNames.Add(siblingInfo.DiscriminatorPropertyName);
            }
        }
        
        // Order properties based on the configured ordering mode
        IEnumerable<IPropertySymbol> orderedProperties;
        
        if (propertyOrdering == PropertyOrderingMode.Alphabetical)
        {
            // For alphabetical ordering, sort by the serialized name (kebab-case by default)
            orderedProperties = allProperties
                .OrderBy(x => discriminatorPropertyNames.Contains(x.Name) ? "" : GetSerializedPropertyName(x)) // discriminators first
                .ThenBy(x => x.Name); // Secondary sort by C# property name for stability
        }
        else if (propertyOrdering == PropertyOrderingMode.OrderedThenAlphabetical)
        {
            // OrderedThenAlphabetical: properties with YamlPropertyOrder come first (sorted by order),
            // then remaining properties sorted alphabetically by serialized name
            orderedProperties = allProperties
                .OrderBy(x => discriminatorPropertyNames.Contains(x.Name) ? -1000 : (HasPropertyOrderAttribute(x) ? GetPropertyOrder(x) : int.MaxValue))
                .ThenBy(x => HasPropertyOrderAttribute(x) ? "" : GetSerializedPropertyName(x)) // alphabetical for unordered
                .ThenBy(x => x.Name); // Secondary sort by C# property name for stability
        }
        else
        {
            // Declaration order: discriminators first (order -1000), then by YamlPropertyOrder, 
            // then by declaration order (index in the list) for stability and to match source order
            orderedProperties = allProperties
                .Select((p, index) => (Property: p, Index: index))
                .OrderBy(x => discriminatorPropertyNames.Contains(x.Property.Name) ? -1000 : GetPropertyOrder(x.Property))
                .ThenBy(x => GetPropertyOrder(x.Property) == int.MaxValue ? x.Index : 0) // Preserve declaration order for unordered properties
                .Select(x => x.Property);
        }
        
        foreach (var prop in orderedProperties)
        {
            var propName = prop.Name;
            var explicitName = GetExplicitYamlPropertyName(prop);
            var isNullable = IsNullableType(prop.Type);
            
            // Calculate the serialized property name (for comparing with discriminator)
            var serializedName = explicitName ?? ToKebabCase(propName);
            
            // Check if this property is the discriminator property
            var isDiscriminatorProperty = discriminatorPropertyName is not null && 
                string.Equals(serializedName, discriminatorPropertyName, StringComparison.OrdinalIgnoreCase);
            
            if (isDiscriminatorProperty)
            {
                // When DiscriminatorPosition.First (or no matching property), discriminator was already written - skip
                // When DiscriminatorPosition.PropertyOrder AND there IS a matching property, write the discriminator at this position
                if (discriminatorPosition == DiscriminatorPositionMode.First || !discriminatorHasMatchingProperty)
                {
                    continue;
                }
                else
                {
                    // Write the discriminator value (not the property value) at this position
                    sb.AppendLine($"            writer.WritePropertyName(\"{discriminatorPropertyName}\");");
                    sb.AppendLine($"            writer.WriteString(\"{discriminatorValue}\");");
                    sb.AppendLine();
                    continue;
                }
            }
            
            // Check for sibling discriminator
            // For dictionaries, we want sibling discriminator to apply to the dictionary values
            // For regular lists/arrays, sibling discriminator doesn't make sense (each element should determine its own type)
            SiblingDiscriminatorInfo? siblingInfo = null;
            if (!IsListOrArray(prop.Type, out _, out _))
            {
                siblingInfo = GetSiblingDiscriminatorInfo(prop);
            }
            
            // Generate property name code
            string propertyNameCode;
            if (explicitName is not null)
            {
                propertyNameCode = $"\"{explicitName}\"";
            }
            else
            {
                propertyNameCode = $"options.PropertyNamingPolicy?.ConvertName(\"{propName}\") ?? \"{ToKebabCase(propName)}\"";
            }
            
            // Check if property type is a registered nested type (for IgnoreEmptyObjects support)
            var underlyingPropType = prop.Type;
            if (prop.Type is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } nullableType)
            {
                underlyingPropType = nullableType.TypeArguments[0];
            }
            var nestedTypeInfo = allTypes.FirstOrDefault(t => 
                SymbolEqualityComparer.Default.Equals(t.Symbol, underlyingPropType) ||
                t.Symbol.ToDisplayString() == underlyingPropType.ToDisplayString());
            
            // Check if the nested type is polymorphic (has derived types) or abstract
            // We can't use IsEmpty for polymorphic/abstract base types because we don't know the derived type's properties at compile time
            // UNLESS we have sibling discrimination info, which tells us all possible derived types
            var isPolymorphicBaseType = nestedTypeInfo is not null && GetPolymorphicInfoForType(nestedTypeInfo) is { DerivedTypes.Count: > 0 };
            var isAbstractType = nestedTypeInfo is not null && nestedTypeInfo.Symbol.IsAbstract;
            var hasSiblingDiscrimination = siblingInfo is not null && siblingInfo.Mappings.Count > 0;
            
            // Skip IsEmpty for types with custom converters - the custom converter controls all serialization logic
            var hasCustomConverter = nestedTypeInfo?.CustomConverterType is not null;
            
            var isNestedObjectType = nestedTypeInfo is not null 
                && nestedTypeInfo.Symbol.TypeKind != TypeKind.Enum
                && !IsListOrArray(nestedTypeInfo.Symbol, out _, out _) 
                && !IsDictionary(nestedTypeInfo.Symbol, out _, out _)
                && (!isPolymorphicBaseType || hasSiblingDiscrimination)  // Allow IsEmpty if we have sibling discrimination
                && (!isAbstractType || hasSiblingDiscrimination)         // Allow IsEmpty if we have sibling discrimination
                && !hasCustomConverter;                                  // Never use IsEmpty for types with custom converters
            
            if (isNullable)
            {
                // Build the condition for whether to write this property
                var conditions = new List<string>();
                conditions.Add($"value.{propName} is not null");
                
                if (ignoreEmptyObjects && isNestedObjectType)
                {
                    if (hasSiblingDiscrimination)
                    {
                        // For sibling-discriminated types, generate inline pattern matching IsEmpty check
                        var isEmptyChecks = new List<string>();
                        foreach (var (_, concreteType) in siblingInfo!.Mappings)
                        {
                            var derivedSafeName = concreteType.Name.Replace(".", "_");
                            var derivedFullTypeName = concreteType.ToDisplayString();
                            isEmptyChecks.Add($"(value.{propName} is {derivedFullTypeName} {derivedSafeName.ToLowerInvariant()} && IsEmpty{derivedSafeName}({derivedSafeName.ToLowerInvariant()}))");
                        }
                        conditions.Add($"!({string.Join(" || ", isEmptyChecks)})");
                    }
                    else
                    {
                        var safeName = nestedTypeInfo!.Symbol.Name.Replace(".", "_");
                        conditions.Add($"!IsEmpty{safeName}(value.{propName})");
                    }
                }
                
                // Wrap nullable properties with IgnoreNullValues and IgnoreEmptyObjects checks
                sb.AppendLine($"            if (!options.IgnoreNullValues || ({string.Join(" && ", conditions)}))");
                sb.AppendLine("            {");
                sb.AppendLine($"                writer.WritePropertyName({propertyNameCode});");
                GeneratePropertyWrite(sb, propName, prop.Type, allTypes, "    ", siblingInfo);
                sb.AppendLine("            }");
            }
            else
            {
                // Non-nullable properties are always written
                sb.AppendLine($"            writer.WritePropertyName({propertyNameCode});");
                GeneratePropertyWrite(sb, propName, prop.Type, allTypes, "", siblingInfo);
            }
            sb.AppendLine();
        }

        sb.AppendLine("            writer.WriteMappingEnd();");
        sb.AppendLine("        }");
    }

    /// <summary>
    /// Generates a static IsEmpty method for a type that returns true if all nullable properties are null.
    /// This is used with IgnoreEmptyObjects to skip writing properties that would serialize to empty mappings.
    /// </summary>
    private static void GenerateIsEmptyMethod(StringBuilder sb, TypeToGenerate type)
    {
        var fullTypeName = type.Symbol.ToDisplayString();
        var safeName = type.Symbol.Name.Replace(".", "_");
        
        // Skip enums, collections, and primitives - they don't have properties
        if (type.Symbol.TypeKind == TypeKind.Enum)
        {
            return;
        }
        
        if (IsListOrArray(type.Symbol, out _, out _) || IsDictionary(type.Symbol, out _, out _))
        {
            return;
        }
        
        // Get all nullable properties that would be written
        var nullableProperties = GetAllProperties(type.Symbol)
            .Where(p => p.GetMethod is not null && !ShouldIgnoreProperty(p) && IsNullableType(p.Type))
            .ToList();
        
        // If no nullable properties, the object is never considered "empty"
        if (nullableProperties.Count == 0)
        {
            sb.AppendLine($"    private static bool IsEmpty{safeName}({fullTypeName}? obj) => obj is null;");
            sb.AppendLine();
            return;
        }
        
        sb.AppendLine($"    private static bool IsEmpty{safeName}({fullTypeName}? obj)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (obj is null) return true;");
        
        // Generate the check: all nullable properties must be null
        var checks = nullableProperties.Select(p => $"obj.{p.Name} is null");
        sb.AppendLine($"        return {string.Join(" && ", checks)};");
        
        sb.AppendLine("    }");
        sb.AppendLine();
    }
    
    private static bool IsNullableType(ITypeSymbol type)
    {
        // Check for nullable value types
        if (type is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T })
        {
            return true;
        }
        
        // Check for nullable reference types or reference types that can be null
        if (!type.IsValueType)
        {
            return true;
        }
        
        return false;
    }

    private static void GeneratePropertyRead(StringBuilder sb, string propName, ITypeSymbol propType, IReadOnlyList<TypeToGenerate> allTypes, SiblingDiscriminatorInfo? siblingInfo = null)
    {
        var typeStr = propType.ToDisplayString();
        var varName = $"_{propName.ToLowerInvariant()}";
        
        // Handle sibling discriminator - use switch on discriminator value to determine concrete type
        // But for dictionaries, the sibling discriminator applies to the VALUE type, not the dictionary itself
        if (siblingInfo is not null && !IsDictionary(propType, out _, out _))
        {
            var discriminatorVarName = $"_{siblingInfo.DiscriminatorPropertyName.ToLowerInvariant()}";
            sb.AppendLine($"                        switch ({discriminatorVarName}.ToString())");
            sb.AppendLine("                        {");
            
            foreach (var (discValue, concreteType) in siblingInfo.Mappings)
            {
                var concreteConverterName = GetConverterName(concreteType);
                sb.AppendLine($"                            case \"{discValue}\":");
                sb.AppendLine($"                                {varName} = new {concreteConverterName}().Read(ref reader, options);");
                sb.AppendLine("                                break;");
            }
            
            sb.AppendLine("                            default:");
            sb.AppendLine("                                reader.Skip();");
            sb.AppendLine($"                                {varName} = null;");
            sb.AppendLine("                                break;");
            sb.AppendLine("                        }");
            return;
        }
        
        // Handle nullable value types - get the underlying type
        var underlyingType = propType;
        var isNullableValueType = false;
        if (propType is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } namedType)
        {
            underlyingType = namedType.TypeArguments[0];
            isNullableValueType = true;
        }
        
        var underlyingTypeStr = underlyingType.ToDisplayString();
        
        // Check for collections first
        if (IsListOrArray(propType, out var elementType, out var isHashSet) && elementType is not null)
        {
            GenerateCollectionRead(sb, varName, propType, elementType, isHashSet, allTypes);
            return;
        }
        
        if (IsDictionary(propType, out var keyType, out var valueType) && keyType is not null && valueType is not null)
        {
            GenerateDictionaryRead(sb, varName, keyType, valueType, allTypes, siblingInfo);
            return;
        }
        
        // Check if the property type is a registered nested type
        var nestedType = allTypes.FirstOrDefault(t => 
            SymbolEqualityComparer.Default.Equals(t.Symbol, underlyingType) ||
            t.Symbol.ToDisplayString() == underlyingType.ToDisplayString());
        
        if (nestedType is not null)
        {
            // Use nested converter
            var nestedConverterName = GetConverterName(nestedType.Symbol);
            sb.AppendLine($"                        {varName} = new {nestedConverterName}().Read(ref reader, options);");
        }
        else if (underlyingTypeStr == "string" || typeStr == "string?")
        {
            // Check for YAML null value before getting string
            sb.AppendLine($"                        {varName} = reader.IsNull() ? null : reader.GetString();");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "int" || underlyingTypeStr == "System.Int32")
        {
            sb.AppendLine($"                        {varName} = reader.TryGetInt32(out var val{propName}) ? val{propName} : {(isNullableValueType ? "null" : "0")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "long" || underlyingTypeStr == "System.Int64")
        {
            sb.AppendLine($"                        {varName} = reader.TryGetInt64(out var val{propName}) ? val{propName} : {(isNullableValueType ? "null" : "0L")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "short" || underlyingTypeStr == "System.Int16")
        {
            sb.AppendLine($"                        {varName} = reader.TryGetInt32(out var val{propName}) ? (short)val{propName} : {(isNullableValueType ? "null" : "(short)0")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "byte" || underlyingTypeStr == "System.Byte")
        {
            sb.AppendLine($"                        {varName} = reader.TryGetInt32(out var val{propName}) ? (byte)val{propName} : {(isNullableValueType ? "null" : "(byte)0")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "sbyte" || underlyingTypeStr == "System.SByte")
        {
            sb.AppendLine($"                        {varName} = reader.TryGetInt32(out var val{propName}) ? (sbyte)val{propName} : {(isNullableValueType ? "null" : "(sbyte)0")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "ushort" || underlyingTypeStr == "System.UInt16")
        {
            sb.AppendLine($"                        {varName} = reader.TryGetInt32(out var val{propName}) ? (ushort)val{propName} : {(isNullableValueType ? "null" : "(ushort)0")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "uint" || underlyingTypeStr == "System.UInt32")
        {
            sb.AppendLine($"                        {varName} = reader.TryGetInt64(out var val{propName}) ? (uint)val{propName} : {(isNullableValueType ? "null" : "0u")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "ulong" || underlyingTypeStr == "System.UInt64")
        {
            sb.AppendLine($"                        {varName} = ulong.TryParse(reader.GetString(), out var val{propName}) ? val{propName} : {(isNullableValueType ? "null" : "0ul")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "char" || underlyingTypeStr == "System.Char")
        {
            sb.AppendLine($"                        var str{propName} = reader.GetString();");
            sb.AppendLine($"                        {varName} = !string.IsNullOrEmpty(str{propName}) ? str{propName}[0] : {(isNullableValueType ? "null" : "'\\0'")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "double" || underlyingTypeStr == "System.Double")
        {
            sb.AppendLine($"                        {varName} = reader.TryGetDouble(out var val{propName}) ? val{propName} : {(isNullableValueType ? "null" : "0.0")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "float" || underlyingTypeStr == "System.Single")
        {
            sb.AppendLine($"                        {varName} = reader.TryGetDouble(out var val{propName}) ? (float)val{propName} : {(isNullableValueType ? "null" : "0f")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "bool" || underlyingTypeStr == "System.Boolean")
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"                        {varName} = reader.TryGetBoolean(out var val{propName}) ? val{propName} : null;");
            }
            else
            {
                sb.AppendLine($"                        {varName} = reader.TryGetBoolean(out var val{propName}) && val{propName};");
            }
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "decimal" || underlyingTypeStr == "System.Decimal")
        {
            sb.AppendLine($"                        {varName} = decimal.TryParse(reader.GetString(), out var val{propName}) ? val{propName} : {(isNullableValueType ? "null" : "0m")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "System.Guid" || underlyingTypeStr == "Guid")
        {
            sb.AppendLine($"                        {varName} = System.Guid.TryParse(reader.GetString(), out var val{propName}) ? val{propName} : {(isNullableValueType ? "null" : "System.Guid.Empty")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "System.DateTime" || underlyingTypeStr == "DateTime")
        {
            sb.AppendLine($"                        {varName} = System.DateTime.TryParse(reader.GetString(), out var val{propName}) ? val{propName} : {(isNullableValueType ? "null" : "default")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "System.DateTimeOffset" || underlyingTypeStr == "DateTimeOffset")
        {
            sb.AppendLine($"                        {varName} = System.DateTimeOffset.TryParse(reader.GetString(), out var val{propName}) ? val{propName} : {(isNullableValueType ? "null" : "default")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "System.TimeSpan" || underlyingTypeStr == "TimeSpan")
        {
            sb.AppendLine($"                        {varName} = System.TimeSpan.TryParse(reader.GetString(), out var val{propName}) ? val{propName} : {(isNullableValueType ? "null" : "default")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "System.DateOnly" || underlyingTypeStr == "DateOnly")
        {
            sb.AppendLine($"                        {varName} = System.DateOnly.TryParse(reader.GetString(), out var val{propName}) ? val{propName} : {(isNullableValueType ? "null" : "default")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingTypeStr == "System.TimeOnly" || underlyingTypeStr == "TimeOnly")
        {
            sb.AppendLine($"                        {varName} = System.TimeOnly.TryParse(reader.GetString(), out var val{propName}) ? val{propName} : {(isNullableValueType ? "null" : "default")};");
            sb.AppendLine("                        reader.Read();");
        }
        else if (typeStr == "System.Uri" || typeStr == "System.Uri?" || typeStr == "Uri" || typeStr == "Uri?")
        {
            sb.AppendLine($"                        {varName} = System.Uri.TryCreate(reader.GetString(), System.UriKind.RelativeOrAbsolute, out var val{propName}) ? val{propName} : null;");
            sb.AppendLine("                        reader.Read();");
        }
        else if (underlyingType.TypeKind == TypeKind.Enum)
        {
            sb.AppendLine($"                        {varName} = System.Enum.TryParse<{underlyingTypeStr}>(reader.GetString(), true, out var val{propName}) ? val{propName} : {(isNullableValueType ? "null" : "default")};");
            sb.AppendLine("                        reader.Read();");
        }
        else
        {
            // Skip complex types that aren't registered
            sb.AppendLine("                        reader.Skip();");
        }
    }

    private static void GeneratePropertyWrite(StringBuilder sb, string propName, ITypeSymbol propType, IReadOnlyList<TypeToGenerate> allTypes, string extraIndent = "", SiblingDiscriminatorInfo? siblingInfo = null)
    {
        var typeStr = propType.ToDisplayString();
        var indent = $"            {extraIndent}";
        var indent2 = $"                {extraIndent}";
        
        // Handle sibling discriminator - dispatch to concrete type converter based on runtime type
        // For dictionaries, sibling discriminator applies to the values, not the dictionary itself, so skip here
        if (siblingInfo is not null && siblingInfo.Mappings.Count > 0 && !IsDictionary(propType, out _, out _))
        {
            sb.AppendLine($"{indent}if (value.{propName} is null)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent2}writer.WriteNull();");
            sb.AppendLine($"{indent}}}");
            
            bool first = true;
            foreach (var (discValue, concreteType) in siblingInfo.Mappings)
            {
                var concreteConverterName = GetConverterName(concreteType);
                var concreteTypeName = concreteType.ToDisplayString();
                var varName = concreteType.Name.ToLowerInvariant() + "Value";
                var keyword = first ? "else if" : "else if";
                first = false;
                
                sb.AppendLine($"{indent}{keyword} (value.{propName} is {concreteTypeName} {varName})");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent2}new {concreteConverterName}().Write(writer, {varName}, options);");
                sb.AppendLine($"{indent}}}");
            }
            
            sb.AppendLine($"{indent}else");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent2}// Unknown derived type - write as null");
            sb.AppendLine($"{indent2}writer.WriteNull();");
            sb.AppendLine($"{indent}}}");
            return;
        }
        
        // Handle nullable value types - get the underlying type
        var underlyingType = propType;
        var isNullableValueType = false;
        if (propType is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } namedType)
        {
            underlyingType = namedType.TypeArguments[0];
            isNullableValueType = true;
        }
        
        var underlyingTypeStr = underlyingType.ToDisplayString();
        
        // Check for collections first
        if (IsListOrArray(propType, out var elementType, out _) && elementType is not null)
        {
            GenerateCollectionWrite(sb, propName, elementType, allTypes, extraIndent);
            return;
        }
        
        if (IsDictionary(propType, out var keyType, out var valueType) && keyType is not null && valueType is not null)
        {
            GenerateDictionaryWrite(sb, propName, keyType, valueType, allTypes, extraIndent, siblingInfo);
            return;
        }
        
        // Check if the property type is a registered nested type
        var nestedType = allTypes.FirstOrDefault(t => 
            SymbolEqualityComparer.Default.Equals(t.Symbol, underlyingType) ||
            t.Symbol.ToDisplayString() == underlyingType.ToDisplayString());
        
        if (nestedType is not null)
        {
            // Use nested converter
            var nestedConverterName = GetConverterName(nestedType.Symbol);
            if (propType.IsValueType && !isNullableValueType)
            {
                // Non-nullable value type - no null check needed
                sb.AppendLine($"{indent}new {nestedConverterName}().Write(writer, value.{propName}, options);");
            }
            else if (isNullableValueType)
            {
                // Nullable value type - need null check and .Value access
                sb.AppendLine($"{indent}if (value.{propName}.HasValue)");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent2}new {nestedConverterName}().Write(writer, value.{propName}.Value, options);");
                sb.AppendLine($"{indent}}}");
                sb.AppendLine($"{indent}else");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent2}writer.WriteNull();");
                sb.AppendLine($"{indent}}}");
            }
            else
            {
                // Reference type - need null check and circular reference detection
                sb.AppendLine($"{indent}if (value.{propName} is null)");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent2}writer.WriteNull();");
                sb.AppendLine($"{indent}}}");
                sb.AppendLine($"{indent}else if (YamlSerializerOptions.CurrentResolver?.IsCycleReference(value.{propName}) == true)");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent2}// Circular reference detected - write null to break the cycle");
                sb.AppendLine($"{indent2}writer.WriteNull();");
                sb.AppendLine($"{indent}}}");
                sb.AppendLine($"{indent}else");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent2}new {nestedConverterName}().Write(writer, value.{propName}, options);");
                sb.AppendLine($"{indent}}}");
            }
        }
        else if (underlyingTypeStr == "string" || typeStr == "string?")
        {
            sb.AppendLine($"{indent}writer.WriteString(value.{propName});");
        }
        else if (underlyingTypeStr == "int" || underlyingTypeStr == "System.Int32")
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteNumber(value.{propName}.Value); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteNumber(value.{propName});");
            }
        }
        else if (underlyingTypeStr == "long" || underlyingTypeStr == "System.Int64")
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteNumber(value.{propName}.Value); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteNumber(value.{propName});");
            }
        }
        else if (underlyingTypeStr == "short" || underlyingTypeStr == "System.Int16" ||
                 underlyingTypeStr == "byte" || underlyingTypeStr == "System.Byte" ||
                 underlyingTypeStr == "sbyte" || underlyingTypeStr == "System.SByte" ||
                 underlyingTypeStr == "ushort" || underlyingTypeStr == "System.UInt16" ||
                 underlyingTypeStr == "uint" || underlyingTypeStr == "System.UInt32")
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteNumber(value.{propName}.Value); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteNumber(value.{propName});");
            }
        }
        else if (underlyingTypeStr == "ulong" || underlyingTypeStr == "System.UInt64")
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteString(value.{propName}.Value.ToString()); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteString(value.{propName}.ToString());");
            }
        }
        else if (underlyingTypeStr == "char" || underlyingTypeStr == "System.Char")
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteString(value.{propName}.Value.ToString()); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteString(value.{propName}.ToString());");
            }
        }
        else if (underlyingTypeStr == "double" || underlyingTypeStr == "System.Double")
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteNumber(value.{propName}.Value); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteNumber(value.{propName});");
            }
        }
        else if (underlyingTypeStr == "float" || underlyingTypeStr == "System.Single")
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteNumber(value.{propName}.Value); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteNumber(value.{propName});");
            }
        }
        else if (underlyingTypeStr == "decimal" || underlyingTypeStr == "System.Decimal")
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteNumber((double)value.{propName}.Value); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteNumber((double)value.{propName});");
            }
        }
        else if (underlyingTypeStr == "bool" || underlyingTypeStr == "System.Boolean")
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteBoolean(value.{propName}.Value); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteBoolean(value.{propName});");
            }
        }
        else if (underlyingType.TypeKind == TypeKind.Enum)
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteString(value.{propName}.Value.ToString()); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteString(value.{propName}.ToString());");
            }
        }
        else if (underlyingTypeStr == "System.Guid" || underlyingTypeStr == "Guid")
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteString(value.{propName}.Value.ToString()); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteString(value.{propName}.ToString());");
            }
        }
        else if (underlyingTypeStr == "System.DateTime" || underlyingTypeStr == "DateTime")
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteString(value.{propName}.Value.ToString(\"O\")); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteString(value.{propName}.ToString(\"O\"));");
            }
        }
        else if (underlyingTypeStr == "System.DateTimeOffset" || underlyingTypeStr == "DateTimeOffset")
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteString(value.{propName}.Value.ToString(\"O\")); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteString(value.{propName}.ToString(\"O\"));");
            }
        }
        else if (underlyingTypeStr == "System.TimeSpan" || underlyingTypeStr == "TimeSpan")
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteString(value.{propName}.Value.ToString(\"c\")); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteString(value.{propName}.ToString(\"c\"));");
            }
        }
        else if (underlyingTypeStr == "System.DateOnly" || underlyingTypeStr == "DateOnly")
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteString(value.{propName}.Value.ToString(\"O\")); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteString(value.{propName}.ToString(\"O\"));");
            }
        }
        else if (underlyingTypeStr == "System.TimeOnly" || underlyingTypeStr == "TimeOnly")
        {
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteString(value.{propName}.Value.ToString(\"O\")); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteString(value.{propName}.ToString(\"O\"));");
            }
        }
        else if (typeStr == "System.Uri" || typeStr == "Uri" || typeStr == "System.Uri?" || typeStr == "Uri?")
        {
            sb.AppendLine($"{indent}if (value.{propName} is not null) writer.WriteString(value.{propName}.OriginalString); else writer.WriteNull();");
        }
        else if (propType.IsValueType)
        {
            // For value types that aren't handled, use ToString
            if (isNullableValueType)
            {
                sb.AppendLine($"{indent}if (value.{propName}.HasValue) writer.WriteString(value.{propName}.Value.ToString()); else writer.WriteNull();");
            }
            else
            {
                sb.AppendLine($"{indent}writer.WriteString(value.{propName}.ToString());");
            }
        }
        else
        {
            // For reference types, use null-conditional
            sb.AppendLine($"{indent}writer.WriteString(value.{propName}?.ToString());");
        }
    }

    private static string GetDefaultValue(ITypeSymbol type)
    {
        var typeStr = type.ToDisplayString();
        
        // Handle arrays - use empty array instead of null
        if (type is IArrayTypeSymbol arrayType)
        {
            return $"System.Array.Empty<{arrayType.ElementType.ToDisplayString()}>()";
        }
        
        // Handle generic collection types - use empty array/list instead of null
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var constructedFrom = namedType.ConstructedFrom.ToDisplayString();
            var elementType = namedType.TypeArguments.Length > 0 
                ? namedType.TypeArguments[0].ToDisplayString() 
                : "object";
                
            // List<T>, IList<T>, IReadOnlyList<T>, ICollection<T>, IEnumerable<T>
            if (constructedFrom.StartsWith("System.Collections.Generic.List<") ||
                constructedFrom.StartsWith("System.Collections.Generic.IList<") ||
                constructedFrom.StartsWith("System.Collections.Generic.IReadOnlyList<") ||
                constructedFrom.StartsWith("System.Collections.Generic.ICollection<") ||
                constructedFrom.StartsWith("System.Collections.Generic.IEnumerable<"))
            {
                return $"new System.Collections.Generic.List<{elementType}>()";
            }
            
            // HashSet<T>
            if (constructedFrom.StartsWith("System.Collections.Generic.HashSet<"))
            {
                return $"new System.Collections.Generic.HashSet<{elementType}>()";
            }
            
            // Dictionary types
            if (namedType.TypeArguments.Length >= 2)
            {
                var keyType = namedType.TypeArguments[0].ToDisplayString();
                var valueType = namedType.TypeArguments[1].ToDisplayString();
                
                if (constructedFrom.StartsWith("System.Collections.Generic.Dictionary<") ||
                    constructedFrom.StartsWith("System.Collections.Generic.IDictionary<") ||
                    constructedFrom.StartsWith("System.Collections.Generic.IReadOnlyDictionary<"))
                {
                    return $"new System.Collections.Generic.Dictionary<{keyType}, {valueType}>()";
                }
            }
        }
        
        if (type.NullableAnnotation == NullableAnnotation.Annotated || !type.IsValueType)
        {
            return "default!";
        }
        
        return typeStr switch
        {
            "string" => "\"\"",
            "int" or "System.Int32" => "0",
            "long" or "System.Int64" => "0L",
            "short" or "System.Int16" => "(short)0",
            "byte" or "System.Byte" => "(byte)0",
            "sbyte" or "System.SByte" => "(sbyte)0",
            "ushort" or "System.UInt16" => "(ushort)0",
            "uint" or "System.UInt32" => "0u",
            "ulong" or "System.UInt64" => "0ul",
            "double" or "System.Double" => "0.0",
            "float" or "System.Single" => "0f",
            "decimal" or "System.Decimal" => "0m",
            "bool" or "System.Boolean" => "false",
            "char" or "System.Char" => "'\\0'",
            _ => "default!"
        };
    }

    private static string GetExplicitDefaultValueString(IParameterSymbol param)
    {
        var value = param.ExplicitDefaultValue;
        
        if (value is null)
        {
            return "default!";
        }
        
        return value switch
        {
            string s => $"\"{s.Replace("\"", "\\\"")}\"",
            bool b => b ? "true" : "false",
            char c => $"'{c}'",
            int i => i.ToString(),
            long l => $"{l}L",
            float f => $"{f}f",
            double d => $"{d}d",
            decimal m => $"{m}m",
            _ => value.ToString() ?? "default!"
        };
    }

    private static string GetYamlPropertyName(IPropertySymbol property)
    {
        // Check for YamlPropertyName attribute
        var explicitName = GetExplicitYamlPropertyName(property);
        if (explicitName is not null)
        {
            return explicitName;
        }
        
        // Default to kebab-case
        return ToKebabCase(property.Name);
    }

    private static string? GetExplicitYamlPropertyName(IPropertySymbol property)
    {
        // Check for YamlPropertyName attribute
        foreach (var attr in property.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "YamlPropertyNameAttribute" ||
                attr.AttributeClass?.ToDisplayString() == "Yamlify.Serialization.YamlPropertyNameAttribute")
            {
                if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string name)
                {
                    return name;
                }
            }
        }
        return null;
    }

    private static bool ShouldIgnoreProperty(IPropertySymbol property)
    {
        // Check for YamlIgnore attribute
        foreach (var attr in property.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "YamlIgnoreAttribute" ||
                attr.AttributeClass?.ToDisplayString() == "Yamlify.Serialization.YamlIgnoreAttribute")
            {
                return true;
            }
        }
        return false;
    }

    private static int GetPropertyOrder(IPropertySymbol property)
    {
        // Check for YamlPropertyOrder attribute
        foreach (var attr in property.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "YamlPropertyOrderAttribute" ||
                attr.AttributeClass?.ToDisplayString() == "Yamlify.Serialization.YamlPropertyOrderAttribute")
            {
                if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int order)
                {
                    return order;
                }
            }
        }
        // Default order is 0, properties without attribute come after those with order
        return int.MaxValue;
    }

    /// <summary>
    /// Gets the inheritance depth from a derived type to a base type.
    /// Higher values mean the type is more specific (further down the inheritance chain).
    /// </summary>
    private static int GetInheritanceDepth(INamedTypeSymbol derivedType, INamedTypeSymbol baseType)
    {
        int depth = 0;
        var current = derivedType;
        
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return depth;
            }
            current = current.BaseType;
            depth++;
        }
        
        // If not found in class hierarchy, check interfaces
        if (baseType.TypeKind == TypeKind.Interface)
        {
            depth = 0;
            current = derivedType;
            while (current is not null)
            {
                if (current.Interfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, baseType)))
                {
                    return depth;
                }
                current = current.BaseType;
                depth++;
            }
        }
        
        return depth;
    }

    private static bool HasPropertyOrderAttribute(IPropertySymbol property)
    {
        foreach (var attr in property.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "YamlPropertyOrderAttribute" ||
                attr.AttributeClass?.ToDisplayString() == "Yamlify.Serialization.YamlPropertyOrderAttribute")
            {
                return true;
            }
        }
        return false;
    }

    private static string GetSerializedPropertyName(IPropertySymbol property)
    {
        // Check for explicit name first
        var explicitName = GetExplicitYamlPropertyName(property);
        if (explicitName is not null)
        {
            return explicitName;
        }
        // Otherwise use kebab-case
        return ToKebabCase(property.Name);
    }

    private static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    sb.Append('-');
                }
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Gets polymorphic type information from [YamlPolymorphic] and [YamlDerivedType] attributes.
    /// </summary>
    private static PolymorphicInfo? GetPolymorphicInfo(INamedTypeSymbol typeSymbol)
    {
        string? typeDiscriminatorPropertyName = null;
        var derivedTypes = new List<(string Discriminator, INamedTypeSymbol DerivedType)>();

        foreach (var attr in typeSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            
            // Check for [YamlPolymorphic] attribute
            if (attrName == "YamlPolymorphicAttribute")
            {
                // Default discriminator is "$type"
                typeDiscriminatorPropertyName = "$type";
                
                // Check for custom TypeDiscriminatorPropertyName
                foreach (var namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "TypeDiscriminatorPropertyName" && namedArg.Value.Value is string propName)
                    {
                        typeDiscriminatorPropertyName = propName;
                    }
                }
            }
            
            // Check for [YamlDerivedType] attributes
            if (attrName == "YamlDerivedTypeAttribute")
            {
                if (attr.ConstructorArguments.Length >= 1 &&
                    attr.ConstructorArguments[0].Value is INamedTypeSymbol derivedType)
                {
                    // Get discriminator value (second constructor arg or derived type name)
                    string discriminator = derivedType.Name;
                    if (attr.ConstructorArguments.Length >= 2 && 
                        attr.ConstructorArguments[1].Value is string discValue)
                    {
                        discriminator = discValue;
                    }
                    
                    derivedTypes.Add((discriminator, derivedType));
                }
            }
        }

        if (typeDiscriminatorPropertyName is not null && derivedTypes.Count > 0)
        {
            return new PolymorphicInfo(typeDiscriminatorPropertyName, derivedTypes);
        }

        return null;
    }

    /// <summary>
    /// Gets polymorphic info for a TypeToGenerate, checking:
    /// 1. First, the PolymorphicConfig from the [YamlSerializable] attribute (takes precedence)
    /// 2. Then, the [YamlPolymorphic]/[YamlDerivedType] attributes on the type itself
    /// </summary>
    private static PolymorphicInfo? GetPolymorphicInfoForType(TypeToGenerate type)
    {
        // PolymorphicConfig from [YamlSerializable] takes precedence
        if (type.PolymorphicConfig is not null)
        {
            return type.PolymorphicConfig;
        }
        
        // Fall back to type-level attributes
        return GetPolymorphicInfo(type.Symbol);
    }

    /// <summary>
    /// Gets polymorphic info for a type, checking the type itself and its base types/interfaces.
    /// </summary>
    private static PolymorphicInfo? GetPolymorphicInfoWithInheritance(INamedTypeSymbol typeSymbol)
    {
        return GetPolymorphicInfoWithInheritance(typeSymbol, null);
    }
    
    /// <summary>
    /// Gets polymorphic info for a type, checking:
    /// 1. The type itself (from attributes)
    /// 2. The type's base types/interfaces (from attributes)
    /// 3. Context-based polymorphic configurations from allTypes where this type is a derived type
    /// </summary>
    private static PolymorphicInfo? GetPolymorphicInfoWithInheritance(INamedTypeSymbol typeSymbol, IReadOnlyList<TypeToGenerate>? allTypes)
    {
        // First check the type itself (from attributes)
        var info = GetPolymorphicInfo(typeSymbol);
        if (info is not null)
        {
            return info;
        }

        // Check base types (from attributes)
        var baseType = typeSymbol.BaseType;
        while (baseType is not null && baseType.SpecialType != SpecialType.System_Object)
        {
            info = GetPolymorphicInfo(baseType);
            if (info is not null)
            {
                return info;
            }
            baseType = baseType.BaseType;
        }

        // Check interfaces (from attributes)
        foreach (var iface in typeSymbol.AllInterfaces)
        {
            info = GetPolymorphicInfo(iface);
            if (info is not null)
            {
                return info;
            }
        }
        
        // Check context-based polymorphic configurations
        // Look for any TypeToGenerate in allTypes that has a PolymorphicConfig where this type is listed as a derived type
        if (allTypes is not null)
        {
            foreach (var contextType in allTypes)
            {
                if (contextType.PolymorphicConfig is null)
                {
                    continue;
                }
                
                // Check if this typeSymbol is listed as a derived type in the PolymorphicConfig
                foreach (var (_, derivedType) in contextType.PolymorphicConfig.DerivedTypes)
                {
                    if (SymbolEqualityComparer.Default.Equals(typeSymbol, derivedType))
                    {
                        return contextType.PolymorphicConfig;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets sibling discriminator information for a property that uses [YamlSiblingDiscriminator].
    /// </summary>
    private static SiblingDiscriminatorInfo? GetSiblingDiscriminatorInfo(IPropertySymbol property)
    {
        string? discriminatorPropertyName = null;
        var mappings = new List<(string DiscriminatorValue, INamedTypeSymbol ConcreteType)>();

        foreach (var attr in property.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            
            // Check for [YamlSiblingDiscriminator] attribute
            if (attrName == "YamlSiblingDiscriminatorAttribute")
            {
                if (attr.ConstructorArguments.Length > 0 && 
                    attr.ConstructorArguments[0].Value is string propName)
                {
                    discriminatorPropertyName = propName;
                }
            }
            
            // Check for [YamlDiscriminatorMapping] attributes
            if (attrName == "YamlDiscriminatorMappingAttribute")
            {
                if (attr.ConstructorArguments.Length >= 2 &&
                    attr.ConstructorArguments[0].Value is string discValue)
                {
                    // Type arguments are represented as ITypeSymbol, not INamedTypeSymbol directly
                    var typeArg = attr.ConstructorArguments[1].Value;
                    INamedTypeSymbol? concreteType = typeArg switch
                    {
                        INamedTypeSymbol nts => nts,
                        ITypeSymbol ts when ts is INamedTypeSymbol nts2 => nts2,
                        _ => null
                    };
                    
                    if (concreteType is not null)
                    {
                        mappings.Add((discValue, concreteType));
                    }
                }
            }
        }

        if (discriminatorPropertyName is not null && mappings.Count > 0)
        {
            return new SiblingDiscriminatorInfo(discriminatorPropertyName, mappings);
        }

        return null;
    }

    /// <summary>
    /// Finds the discriminator value for a derived type in a polymorphic hierarchy.
    /// </summary>
    private static string? GetDiscriminatorForDerivedType(INamedTypeSymbol derivedType, PolymorphicInfo polyInfo)
    {
        foreach (var (discriminator, type) in polyInfo.DerivedTypes)
        {
            if (SymbolEqualityComparer.Default.Equals(type, derivedType) ||
                type.ToDisplayString() == derivedType.ToDisplayString())
            {
                return discriminator;
            }
        }
        return null;
    }

    private static bool IsListOrArray(ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? elementType, out bool isHashSet)
    {
        elementType = null;
        isHashSet = false;
        
        // Check for arrays
        if (type is IArrayTypeSymbol arrayType)
        {
            elementType = arrayType.ElementType;
            return true;
        }
        
        // Check for List<T>, IList<T>, IReadOnlyList<T>, IEnumerable<T>, ICollection<T>, HashSet<T>
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeName = namedType.ConstructedFrom.ToDisplayString();
            if (typeName.StartsWith("System.Collections.Generic.HashSet<"))
            {
                elementType = namedType.TypeArguments[0];
                isHashSet = true;
                return true;
            }
            if (typeName.StartsWith("System.Collections.Generic.List<") ||
                typeName.StartsWith("System.Collections.Generic.IList<") ||
                typeName.StartsWith("System.Collections.Generic.IReadOnlyList<") ||
                typeName.StartsWith("System.Collections.Generic.ICollection<") ||
                typeName.StartsWith("System.Collections.Generic.IEnumerable<"))
            {
                elementType = namedType.TypeArguments[0];
                return true;
            }
        }
        
        return false;
    }
    
    private static bool IsDictionary(ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? keyType, [NotNullWhen(true)] out ITypeSymbol? valueType)
    {
        keyType = null;
        valueType = null;
        
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeName = namedType.ConstructedFrom.ToDisplayString();
            if (typeName.StartsWith("System.Collections.Generic.Dictionary<") ||
                typeName.StartsWith("System.Collections.Generic.IDictionary<") ||
                typeName.StartsWith("System.Collections.Generic.IReadOnlyDictionary<"))
            {
                keyType = namedType.TypeArguments[0];
                valueType = namedType.TypeArguments[1];
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Generates read method for a collection type (List&lt;T&gt;, T[], etc.) when it's the root type.
    /// </summary>
    private static void GenerateRootCollectionRead(StringBuilder sb, ITypeSymbol collectionType, ITypeSymbol elementType, bool isHashSet, IReadOnlyList<TypeToGenerate> allTypes)
    {
        var elementTypeStr = elementType.ToDisplayString();
        var isArray = collectionType is IArrayTypeSymbol;
        var fullTypeName = collectionType.ToDisplayString();
        
        sb.AppendLine("            if (reader.TokenType != YamlTokenType.SequenceStart)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Skip unexpected token to prevent infinite loops when reading collections");
        sb.AppendLine("                reader.Skip();");
        sb.AppendLine("                return default;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine($"            var list = new System.Collections.Generic.List<{elementTypeStr}>();");
        sb.AppendLine("            reader.Read(); // Move past SequenceStart");
        sb.AppendLine("            while (reader.TokenType != YamlTokenType.SequenceEnd && reader.TokenType != YamlTokenType.None)");
        sb.AppendLine("            {");
        
        // Generate element reading based on element type
        GenerateElementRead(sb, "item", elementType, allTypes, "                ");
        sb.AppendLine("                list.Add(item);");
        
        sb.AppendLine("            }");
        sb.AppendLine("            reader.Read(); // Move past SequenceEnd");
        
        if (isArray)
        {
            sb.AppendLine("            return list.ToArray();");
        }
        else if (isHashSet)
        {
            sb.AppendLine($"            return new System.Collections.Generic.HashSet<{elementTypeStr}>(list);");
        }
        else
        {
            sb.AppendLine("            return list;");
        }
        
        sb.AppendLine("        }");
    }
    
    /// <summary>
    /// Generates read method for a dictionary type when it's the root type.
    /// </summary>
    private static void GenerateRootDictionaryRead(StringBuilder sb, ITypeSymbol dictType, ITypeSymbol keyType, ITypeSymbol valueType, IReadOnlyList<TypeToGenerate> allTypes)
    {
        var keyTypeStr = keyType.ToDisplayString();
        var valueTypeStr = valueType.ToDisplayString();
        
        sb.AppendLine("            if (reader.TokenType != YamlTokenType.MappingStart)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Skip unexpected token to prevent infinite loops when reading collections");
        sb.AppendLine("                reader.Skip();");
        sb.AppendLine("                return default;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine($"            var dict = new System.Collections.Generic.Dictionary<{keyTypeStr}, {valueTypeStr}>();");
        sb.AppendLine("            reader.Read(); // Move past MappingStart");
        sb.AppendLine("            while (reader.TokenType != YamlTokenType.MappingEnd && reader.TokenType != YamlTokenType.None)");
        sb.AppendLine("            {");
        
        // Read key
        if (keyType.TypeKind == TypeKind.Enum)
        {
            sb.AppendLine("                var keyString = reader.GetString();");
            sb.AppendLine("                reader.Read();");
            sb.AppendLine($"                if (!System.Enum.TryParse<{keyTypeStr}>(keyString, true, out var key))");
            sb.AppendLine("                {");
            sb.AppendLine("                    reader.Skip();");
            sb.AppendLine("                    continue;");
            sb.AppendLine("                }");
        }
        else
        {
            sb.AppendLine("                var key = reader.GetString() ?? string.Empty;");
            sb.AppendLine("                reader.Read();");
        }
        
        // Read value
        GenerateElementRead(sb, "value", valueType, allTypes, "                ");
        
        sb.AppendLine("                dict[key] = value;");
        sb.AppendLine("            }");
        sb.AppendLine("            reader.Read(); // Move past MappingEnd");
        sb.AppendLine("            return dict;");
        sb.AppendLine("        }");
    }
    
    /// <summary>
    /// Generates write method for a collection type (List&lt;T&gt;, T[], etc.) when it's the root type.
    /// </summary>
    private static void GenerateRootCollectionWrite(StringBuilder sb, ITypeSymbol collectionType, ITypeSymbol elementType, IReadOnlyList<TypeToGenerate> allTypes)
    {
        // Use flow style for empty collections to output [] instead of nothing.
        // For IEnumerable<T> that doesn't implement ICollection, we use TryGetNonEnumeratedCount or enumerate.
        // Since we need to enumerate anyway for writing, we just check ICollection first for efficiency.
        sb.AppendLine("            var hasItems = false;");
        sb.AppendLine("            if (value is System.Collections.ICollection collection)");
        sb.AppendLine("            {");
        sb.AppendLine("                hasItems = collection.Count > 0;");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                // For pure IEnumerable, we need to start iterating to know if it's empty");
        sb.AppendLine("                using var enumerator = value.GetEnumerator();");
        sb.AppendLine("                hasItems = enumerator.MoveNext();");
        sb.AppendLine("                if (hasItems)");
        sb.AppendLine("                {");
        sb.AppendLine("                    // Non-empty IEnumerable - write sequence with first item, then rest");
        sb.AppendLine("                    writer.WriteSequenceStart();");
        sb.AppendLine("                    do");
        sb.AppendLine("                    {");
        GenerateElementWrite(sb, "enumerator.Current", elementType, allTypes, "                        ");
        sb.AppendLine("                    } while (enumerator.MoveNext());");
        sb.AppendLine("                    writer.WriteSequenceEnd();");
        sb.AppendLine("                    return;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            if (!hasItems)");
        sb.AppendLine("            {");
        sb.AppendLine("                writer.WriteSequenceStart(Yamlify.CollectionStyle.Flow);");
        sb.AppendLine("                writer.WriteSequenceEnd();");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                writer.WriteSequenceStart();");
        sb.AppendLine("                foreach (var item in value)");
        sb.AppendLine("                {");
        
        GenerateElementWrite(sb, "item", elementType, allTypes, "                    ");
        
        sb.AppendLine("                }");
        sb.AppendLine("                writer.WriteSequenceEnd();");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
    }
    
    /// <summary>
    /// Generates write method for a dictionary type when it's the root type.
    /// </summary>
    private static void GenerateRootDictionaryWrite(StringBuilder sb, ITypeSymbol dictType, ITypeSymbol keyType, ITypeSymbol valueType, IReadOnlyList<TypeToGenerate> allTypes)
    {
        sb.AppendLine("            writer.WriteMappingStart();");
        sb.AppendLine("            foreach (var kvp in value)");
        sb.AppendLine("            {");
        
        // Write key
        if (keyType.TypeKind == TypeKind.Enum)
        {
            sb.AppendLine("                writer.WritePropertyName(kvp.Key.ToString());");
        }
        else
        {
            sb.AppendLine("                writer.WritePropertyName(kvp.Key);");
        }
        
        // Write value
        GenerateElementWrite(sb, "kvp.Value", valueType, allTypes, "                ");
        
        sb.AppendLine("            }");
        sb.AppendLine("            writer.WriteMappingEnd();");
        sb.AppendLine("        }");
    }
    
    private static void GenerateCollectionRead(StringBuilder sb, string varName, ITypeSymbol propType, ITypeSymbol elementType, bool isHashSet, IReadOnlyList<TypeToGenerate> allTypes)
    {
        var elementTypeStr = elementType.ToDisplayString();
        var isArray = propType is IArrayTypeSymbol;
        
        sb.AppendLine($"                        if (reader.TokenType == YamlTokenType.SequenceStart)");
        sb.AppendLine("                        {");
        sb.AppendLine($"                            var list = new System.Collections.Generic.List<{elementTypeStr}>();");
        sb.AppendLine("                            reader.Read(); // Move past SequenceStart");
        sb.AppendLine("                            while (reader.TokenType != YamlTokenType.SequenceEnd && reader.TokenType != YamlTokenType.None)");
        sb.AppendLine("                            {");
        
        // Generate element reading based on element type
        GenerateElementRead(sb, "item", elementType, allTypes, "                                ");
        sb.AppendLine("                                list.Add(item);");
        
        sb.AppendLine("                            }");
        sb.AppendLine("                            reader.Read(); // Move past SequenceEnd");
        
        if (isArray)
        {
            sb.AppendLine($"                            {varName} = list.ToArray();");
        }
        else if (isHashSet)
        {
            sb.AppendLine($"                            {varName} = new System.Collections.Generic.HashSet<{elementTypeStr}>(list);");
        }
        else
        {
            sb.AppendLine($"                            {varName} = list;");
        }
        
        sb.AppendLine("                        }");
        sb.AppendLine("                        else");
        sb.AppendLine("                        {");
        sb.AppendLine("                            reader.Skip();");
        sb.AppendLine("                        }");
    }
    
    private static void GenerateElementRead(StringBuilder sb, string varName, ITypeSymbol elementType, IReadOnlyList<TypeToGenerate> allTypes, string indent)
    {
        var typeStr = elementType.ToDisplayString();
        
        // Check if element type is itself a collection (nested collections)
        if (IsListOrArray(elementType, out var nestedElementType, out var isNestedHashSet))
        {
            var nestedElementTypeStr = nestedElementType.ToDisplayString();
            sb.AppendLine($"{indent}System.Collections.Generic.List<{nestedElementTypeStr}>? {varName} = null;");
            sb.AppendLine($"{indent}if (reader.TokenType == YamlTokenType.SequenceStart)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    var nestedList = new System.Collections.Generic.List<{nestedElementTypeStr}>();");
            sb.AppendLine($"{indent}    reader.Read(); // Move past SequenceStart");
            sb.AppendLine($"{indent}    while (reader.TokenType != YamlTokenType.SequenceEnd && reader.TokenType != YamlTokenType.None)");
            sb.AppendLine($"{indent}    {{");
            GenerateElementRead(sb, "nestedElement", nestedElementType, allTypes, indent + "        ");
            sb.AppendLine($"{indent}        nestedList.Add(nestedElement);");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}    reader.Read(); // Move past SequenceEnd");
            sb.AppendLine($"{indent}    {varName} = nestedList;");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine($"{indent}else");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    reader.Skip();");
            sb.AppendLine($"{indent}}}");
            return;
        }
        
        // Check if element type is a dictionary
        if (IsDictionary(elementType, out var nestedKeyType, out var nestedValueType))
        {
            var nestedKeyTypeStr = nestedKeyType.ToDisplayString();
            var nestedValueTypeStr = nestedValueType.ToDisplayString();
            sb.AppendLine($"{indent}System.Collections.Generic.Dictionary<{nestedKeyTypeStr}, {nestedValueTypeStr}>? {varName} = null;");
            sb.AppendLine($"{indent}if (reader.TokenType == YamlTokenType.MappingStart)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    var nestedDict = new System.Collections.Generic.Dictionary<{nestedKeyTypeStr}, {nestedValueTypeStr}>();");
            sb.AppendLine($"{indent}    reader.Read(); // Move past MappingStart");
            sb.AppendLine($"{indent}    while (reader.TokenType != YamlTokenType.MappingEnd && reader.TokenType != YamlTokenType.None)");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        var nestedKey = reader.GetString() ?? \"\";");
            sb.AppendLine($"{indent}        reader.Read();");
            GenerateElementRead(sb, "nestedValue", nestedValueType, allTypes, indent + "        ");
            sb.AppendLine($"{indent}        nestedDict[nestedKey] = nestedValue;");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}    reader.Read(); // Move past MappingEnd");
            sb.AppendLine($"{indent}    {varName} = nestedDict;");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine($"{indent}else");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    reader.Skip();");
            sb.AppendLine($"{indent}}}");
            return;
        }
        
        // Check if it's a registered nested type
        var nestedType = allTypes.FirstOrDefault(t => 
            SymbolEqualityComparer.Default.Equals(t.Symbol, elementType) ||
            t.Symbol.ToDisplayString() == typeStr);
        
        if (nestedType is not null)
        {
            var nestedConverterName = GetConverterName(nestedType.Symbol);
            sb.AppendLine($"{indent}var {varName} = new {nestedConverterName}().Read(ref reader, options);");
        }
        else if (typeStr == "string" || typeStr == "string?")
        {
            // Check for YAML null value before getting string
            sb.AppendLine($"{indent}var {varName} = reader.IsNull() ? null : reader.GetString();");
            sb.AppendLine($"{indent}reader.Read();");
        }
        else if (typeStr == "int" || typeStr == "System.Int32")
        {
            sb.AppendLine($"{indent}var {varName} = reader.TryGetInt32(out var v) ? v : 0;");
            sb.AppendLine($"{indent}reader.Read();");
        }
        else if (typeStr == "long" || typeStr == "System.Int64")
        {
            sb.AppendLine($"{indent}var {varName} = reader.TryGetInt64(out var v) ? v : 0L;");
            sb.AppendLine($"{indent}reader.Read();");
        }
        else if (typeStr == "double" || typeStr == "System.Double")
        {
            sb.AppendLine($"{indent}var {varName} = reader.TryGetDouble(out var v) ? v : 0.0;");
            sb.AppendLine($"{indent}reader.Read();");
        }
        else if (typeStr == "float" || typeStr == "System.Single")
        {
            sb.AppendLine($"{indent}var {varName} = reader.TryGetDouble(out var v) ? (float)v : 0f;");
            sb.AppendLine($"{indent}reader.Read();");
        }
        else if (typeStr == "bool" || typeStr == "System.Boolean")
        {
            sb.AppendLine($"{indent}var {varName} = reader.TryGetBoolean(out var v) && v;");
            sb.AppendLine($"{indent}reader.Read();");
        }
        else
        {
            sb.AppendLine($"{indent}{typeStr} {varName} = default!;");
            sb.AppendLine($"{indent}reader.Skip();");
        }
    }
    
    private static void GenerateDictionaryRead(StringBuilder sb, string varName, ITypeSymbol keyType, ITypeSymbol valueType, IReadOnlyList<TypeToGenerate> allTypes, SiblingDiscriminatorInfo? siblingInfo = null)
    {
        var keyTypeStr = keyType.ToDisplayString();
        var valueTypeStr = valueType.ToDisplayString();
        
        sb.AppendLine($"                        if (reader.TokenType == YamlTokenType.MappingStart)");
        sb.AppendLine("                        {");
        sb.AppendLine($"                            var dict = new System.Collections.Generic.Dictionary<{keyTypeStr}, {valueTypeStr}>();");
        sb.AppendLine("                            reader.Read(); // Move past MappingStart");
        sb.AppendLine("                            while (reader.TokenType != YamlTokenType.MappingEnd && reader.TokenType != YamlTokenType.None)");
        sb.AppendLine("                            {");
        sb.AppendLine("                                var keyStr = reader.GetString() ?? \"\";");
        
        // Handle enum keys
        if (keyType.TypeKind == TypeKind.Enum)
        {
            sb.AppendLine($"                                var key = System.Enum.TryParse<{keyTypeStr}>(keyStr, true, out var keyVal) ? keyVal : default;");
        }
        else if (keyTypeStr == "string")
        {
            sb.AppendLine("                                var key = keyStr;");
        }
        else
        {
            // Default to string for unknown key types
            sb.AppendLine($"                                var key = keyStr;");
        }
        
        sb.AppendLine("                                reader.Read();");
        
        // Generate value reading - use sibling discriminator if provided for polymorphic value types
        if (siblingInfo is not null)
        {
            // Use sibling discriminator to determine concrete type for dictionary values
            var discriminatorVarName = $"_{siblingInfo.DiscriminatorPropertyName.ToLowerInvariant()}";
            sb.AppendLine($"                                {valueTypeStr} value;");
            sb.AppendLine($"                                switch ({discriminatorVarName}.ToString())");
            sb.AppendLine("                                {");
            
            foreach (var (discValue, concreteType) in siblingInfo.Mappings)
            {
                var concreteConverterName = GetConverterName(concreteType);
                sb.AppendLine($"                                    case \"{discValue}\":");
                sb.AppendLine($"                                        value = new {concreteConverterName}().Read(ref reader, options);");
                sb.AppendLine("                                        break;");
            }
            
            sb.AppendLine("                                    default:");
            sb.AppendLine("                                        reader.Skip();");
            sb.AppendLine("                                        value = null;");
            sb.AppendLine("                                        break;");
            sb.AppendLine("                                }");
        }
        else
        {
            GenerateElementRead(sb, "value", valueType, allTypes, "                                ");
        }
        
        // Handle array value types - GenerateElementRead creates List<T> for collections
        if (valueType is IArrayTypeSymbol arrayType)
        {
            sb.AppendLine("                                dict[key] = value?.ToArray() ?? [];");
        }
        else
        {
            sb.AppendLine("                                dict[key] = value;");
        }
        
        sb.AppendLine("                            }");
        sb.AppendLine("                            reader.Read(); // Move past MappingEnd");
        sb.AppendLine($"                            {varName} = dict;");
        sb.AppendLine("                        }");
        sb.AppendLine("                        else");
        sb.AppendLine("                        {");
        sb.AppendLine("                            reader.Skip();");
        sb.AppendLine("                        }");
    }
    
    private static void GenerateCollectionWrite(StringBuilder sb, string propName, ITypeSymbol elementType, IReadOnlyList<TypeToGenerate> allTypes, string extraIndent = "")
    {
        var elementTypeStr = elementType.ToDisplayString();
        
        sb.AppendLine($"            if (value.{propName} is not null)");
        sb.AppendLine("            {");
        // Use flow style for empty collections to output [] instead of nothing
        sb.AppendLine($"                if (value.{propName} is System.Collections.ICollection {{ Count: 0 }})");
        sb.AppendLine("                {");
        sb.AppendLine("                    writer.WriteSequenceStart(Yamlify.CollectionStyle.Flow);");
        sb.AppendLine("                    writer.WriteSequenceEnd();");
        sb.AppendLine("                }");
        sb.AppendLine("                else");
        sb.AppendLine("                {");
        sb.AppendLine("                    writer.WriteSequenceStart();");
        sb.AppendLine($"                    foreach (var item in value.{propName})");
        sb.AppendLine("                    {");
        
        // Generate element writing based on element type
        GenerateElementWrite(sb, "item", elementType, allTypes, "                        ");
        
        sb.AppendLine("                    }");
        sb.AppendLine("                    writer.WriteSequenceEnd();");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                writer.WriteNull();");
        sb.AppendLine("            }");
    }
    
    private static void GenerateElementWrite(StringBuilder sb, string varName, ITypeSymbol elementType, IReadOnlyList<TypeToGenerate> allTypes, string indent, SiblingDiscriminatorInfo? siblingInfo = null)
    {
        var typeStr = elementType.ToDisplayString();
        
        // Handle sibling discriminator - dispatch to concrete type converter based on runtime type
        if (siblingInfo is not null && siblingInfo.Mappings.Count > 0)
        {
            sb.AppendLine($"{indent}if ({varName} is null)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    writer.WriteNull();");
            sb.AppendLine($"{indent}}}");
            
            bool first = true;
            foreach (var (discValue, concreteType) in siblingInfo.Mappings)
            {
                var concreteConverterName = GetConverterName(concreteType);
                var concreteTypeName = concreteType.ToDisplayString();
                var localVarName = concreteType.Name.ToLowerInvariant() + "Value";
                var keyword = first ? "else if" : "else if";
                first = false;
                
                sb.AppendLine($"{indent}{keyword} ({varName} is {concreteTypeName} {localVarName})");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent}    new {concreteConverterName}().Write(writer, {localVarName}, options);");
                sb.AppendLine($"{indent}}}");
            }
            
            sb.AppendLine($"{indent}else");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    // Unknown derived type - write as null");
            sb.AppendLine($"{indent}    writer.WriteNull();");
            sb.AppendLine($"{indent}}}");
            return;
        }
        
        // Check if element type is itself a collection (nested collections)
        if (IsListOrArray(elementType, out var nestedElementType, out _))
        {
            sb.AppendLine($"{indent}if ({varName} is not null)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    writer.WriteSequenceStart();");
            sb.AppendLine($"{indent}    foreach (var nestedItem in {varName})");
            sb.AppendLine($"{indent}    {{");
            GenerateElementWrite(sb, "nestedItem", nestedElementType, allTypes, indent + "        ");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}    writer.WriteSequenceEnd();");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine($"{indent}else");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    writer.WriteNull();");
            sb.AppendLine($"{indent}}}");
            return;
        }
        
        // Check if element type is a dictionary
        if (IsDictionary(elementType, out var nestedKeyType, out var nestedValueType))
        {
            sb.AppendLine($"{indent}if ({varName} is not null)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    writer.WriteMappingStart();");
            sb.AppendLine($"{indent}    foreach (var nestedKvp in {varName})");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        writer.WritePropertyName(nestedKvp.Key?.ToString() ?? \"\");");
            GenerateElementWrite(sb, "nestedKvp.Value", nestedValueType, allTypes, indent + "        ");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}    writer.WriteMappingEnd();");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine($"{indent}else");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    writer.WriteNull();");
            sb.AppendLine($"{indent}}}");
            return;
        }
        
        // Check if it's a registered nested type
        var nestedType = allTypes.FirstOrDefault(t => 
            SymbolEqualityComparer.Default.Equals(t.Symbol, elementType) ||
            t.Symbol.ToDisplayString() == typeStr);
        
        if (nestedType is not null)
        {
            var nestedConverterName = GetConverterName(nestedType.Symbol);
            // Value types (enums, structs) can't be null
            if (elementType.IsValueType)
            {
                sb.AppendLine($"{indent}new {nestedConverterName}().Write(writer, {varName}, options);");
            }
            else
            {
                sb.AppendLine($"{indent}if ({varName} is null)");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent}    writer.WriteNull();");
                sb.AppendLine($"{indent}}}");
                sb.AppendLine($"{indent}else if (YamlSerializerOptions.CurrentResolver?.IsCycleReference({varName}) == true)");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent}    // Circular reference detected - write null to break the cycle");
                sb.AppendLine($"{indent}    writer.WriteNull();");
                sb.AppendLine($"{indent}}}");
                sb.AppendLine($"{indent}else");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent}    new {nestedConverterName}().Write(writer, {varName}, options);");
                sb.AppendLine($"{indent}}}");
            }
        }
        else if (typeStr == "string" || typeStr == "string?")
        {
            sb.AppendLine($"{indent}writer.WriteString({varName});");
        }
        else if (typeStr == "int" || typeStr == "System.Int32" ||
                 typeStr == "long" || typeStr == "System.Int64" ||
                 typeStr == "double" || typeStr == "System.Double" ||
                 typeStr == "float" || typeStr == "System.Single")
        {
            sb.AppendLine($"{indent}writer.WriteNumber({varName});");
        }
        else if (typeStr == "bool" || typeStr == "System.Boolean")
        {
            sb.AppendLine($"{indent}writer.WriteBoolean({varName});");
        }
        else
        {
            sb.AppendLine($"{indent}writer.WriteString({varName}?.ToString());");
        }
    }
    
    private static void GenerateDictionaryWrite(StringBuilder sb, string propName, ITypeSymbol keyType, ITypeSymbol valueType, IReadOnlyList<TypeToGenerate> allTypes, string extraIndent = "", SiblingDiscriminatorInfo? siblingInfo = null)
    {
        sb.AppendLine($"            if (value.{propName} is not null)");
        sb.AppendLine("            {");
        sb.AppendLine("                writer.WriteMappingStart();");
        sb.AppendLine($"                foreach (var kvp in value.{propName})");
        sb.AppendLine("                {");
        
        // Handle enum keys and value type keys (can't use ?. on value types)
        if (keyType.IsValueType)
        {
            sb.AppendLine("                    writer.WritePropertyName(kvp.Key.ToString() ?? \"\");");
        }
        else
        {
            sb.AppendLine("                    writer.WritePropertyName(kvp.Key?.ToString() ?? \"\");");
        }
        
        // Generate value writing - pass siblingInfo for polymorphic dictionary values
        GenerateElementWrite(sb, "kvp.Value", valueType, allTypes, "                    ", siblingInfo);
        
        sb.AppendLine("                }");
        sb.AppendLine("                writer.WriteMappingEnd();");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                writer.WriteNull();");
        sb.AppendLine("            }");
    }
    
    /// <summary>
    /// Gets all public instance properties including inherited ones.
    /// Properties are returned in declaration order: base class properties first, then derived class.
    /// This ensures proper property ordering when DeclarationOrder mode is used.
    /// </summary>
    private static IEnumerable<IPropertySymbol> GetAllProperties(INamedTypeSymbol type)
    {
        // First, collect the type hierarchy (from base to derived)
        var typeHierarchy = new List<INamedTypeSymbol>();
        var currentType = type;
        
        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            typeHierarchy.Add(currentType);
            currentType = currentType.BaseType;
        }
        
        // Reverse to process from base class to derived class
        typeHierarchy.Reverse();
        
        // Track which properties have been seen to handle overrides
        // For overrides, we use the derived class's property symbol but keep base class position
        var seenProperties = new Dictionary<string, IPropertySymbol>();
        var propertyOrder = new List<string>();
        
        foreach (var typeInHierarchy in typeHierarchy)
        {
            foreach (var member in typeInHierarchy.GetMembers())
            {
                if (member is IPropertySymbol prop 
                    && prop.DeclaredAccessibility == Accessibility.Public
                    && !prop.IsStatic 
                    && !prop.IsIndexer)
                {
                    if (seenProperties.ContainsKey(prop.Name))
                    {
                        // Override: replace with derived property but keep original position
                        seenProperties[prop.Name] = prop;
                    }
                    else
                    {
                        // New property: add to order and track it
                        seenProperties[prop.Name] = prop;
                        propertyOrder.Add(prop.Name);
                    }
                }
            }
        }
        
        // Return properties in declaration order (base class first)
        foreach (var propName in propertyOrder)
        {
            yield return seenProperties[propName];
        }
    }
    
    /// <summary>
    /// Gets a unique identifier name for a type to avoid collisions
    /// when multiple types have the same simple name in different namespaces.
    /// </summary>
    private static string GetUniqueTypeName(ITypeSymbol type)
    {
        // Use the full type name with dots replaced by underscores
        // e.g., "SwissLife.Lukla.Workloads.NetworkPolicies" -> "SwissLife_Lukla_Workloads_NetworkPolicies"
        var fullName = type.ToDisplayString();
        return fullName.Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(", ", "_");
    }
    
    /// <summary>
    /// Gets a unique converter name for a type.
    /// </summary>
    private static string GetConverterName(ITypeSymbol type)
    {
        return $"{GetUniqueTypeName(type)}Converter";
    }
    
    /// <summary>
    /// Gets a unique converter name from the full type display string.
    /// </summary>
    private static string GetConverterNameFromFullTypeName(string fullTypeName)
    {
        var sanitizedName = fullTypeName.Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(", ", "_");
        return $"{sanitizedName}Converter";
    }
    
    /// <summary>
    /// Builds a map from full type names to property names.
    /// Uses simple names when there's no collision, and qualified names when there are collisions.
    /// </summary>
    private static Dictionary<string, string> BuildPropertyNameMap(List<TypeToGenerate> types)
    {
        var result = new Dictionary<string, string>();
        
        // Group types by simple name to detect collisions
        var groupedBySimpleName = types
            .GroupBy(t => GetSimplePropertyName(t.Symbol))
            .ToList();
        
        foreach (var group in groupedBySimpleName)
        {
            var typesList = group.ToList();
            if (typesList.Count == 1)
            {
                // No collision, use simple name
                var type = typesList[0];
                result[type.Symbol.ToDisplayString()] = GetSimplePropertyName(type.Symbol);
            }
            else
            {
                // Collision detected, use unique names
                foreach (var type in typesList)
                {
                    var uniqueName = GetUniqueTypeName(type.Symbol);
                    result[type.Symbol.ToDisplayString()] = uniqueName;
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets a simple property name for a type. For generic types, includes the type arguments.
    /// E.g., List&lt;TemplateTransformDefinition&gt; becomes ListTemplateTransformDefinition
    /// For arrays, uses ElementTypeArray format, e.g., AppGroupDefinition[] becomes AppGroupDefinitionArray
    /// </summary>
    private static string GetSimplePropertyName(ITypeSymbol type)
    {
        // Handle arrays: AppGroupDefinition[] -> AppGroupDefinitionArray
        if (type is IArrayTypeSymbol arrayType)
        {
            var elementName = GetSimplePropertyName(arrayType.ElementType);
            return elementName + "Array";
        }
        
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            // For generic types, combine the base name with type arguments
            // e.g., List<TemplateTransformDefinition> -> ListTemplateTransformDefinition
            var baseName = namedType.Name;
            var typeArgs = string.Join("", namedType.TypeArguments.Select(GetSimplePropertyName));
            return baseName + typeArgs;
        }
        
        return type.Name;
    }
}

internal sealed class ContextToGenerate : IEquatable<ContextToGenerate>
{
    public string ClassName { get; }
    public string Namespace { get; }
    public List<TypeToGenerate> Types { get; }
    public PropertyOrderingMode PropertyOrdering { get; }
    public bool IndentSequenceItems { get; }
    public bool IgnoreNullValues { get; }
    public bool IgnoreEmptyObjects { get; }
    public DiscriminatorPositionMode DiscriminatorPosition { get; }

    public ContextToGenerate(
        string className, 
        string ns, 
        List<TypeToGenerate> types, 
        PropertyOrderingMode propertyOrdering = PropertyOrderingMode.DeclarationOrder,
        bool indentSequenceItems = true,
        bool ignoreNullValues = false,
        bool ignoreEmptyObjects = false,
        DiscriminatorPositionMode discriminatorPosition = DiscriminatorPositionMode.PropertyOrder)
    {
        ClassName = className;
        Namespace = ns;
        Types = types;
        PropertyOrdering = propertyOrdering;
        IndentSequenceItems = indentSequenceItems;
        IgnoreNullValues = ignoreNullValues;
        IgnoreEmptyObjects = ignoreEmptyObjects;
        DiscriminatorPosition = discriminatorPosition;
    }

    public bool Equals(ContextToGenerate? other)
    {
        if (other is null)
        {
            return false;
        }
        return ClassName == other.ClassName && Namespace == other.Namespace;
    }

    public override bool Equals(object? obj) => Equals(obj as ContextToGenerate);
    
    public override int GetHashCode() => (ClassName, Namespace).GetHashCode();
}

/// <summary>
/// Specifies the ordering strategy for properties during serialization.
/// </summary>
internal enum PropertyOrderingMode
{
    /// <summary>
    /// Properties are ordered by their declaration order in the source code.
    /// </summary>
    DeclarationOrder = 0,

    /// <summary>
    /// Properties are ordered alphabetically by their serialized name.
    /// </summary>
    Alphabetical = 1,

    /// <summary>
    /// Properties with YamlPropertyOrder come first (sorted by order), then remaining alphabetically.
    /// </summary>
    OrderedThenAlphabetical = 2
}

/// <summary>
/// Specifies where the type discriminator property should be written during serialization.
/// </summary>
internal enum DiscriminatorPositionMode
{
    /// <summary>
    /// The discriminator property is written according to its YamlPropertyOrder or declaration order.
    /// </summary>
    PropertyOrder = 0,

    /// <summary>
    /// The discriminator property is always written first.
    /// </summary>
    First = 1
}

internal sealed class TypeToGenerate
{
    public INamedTypeSymbol Symbol { get; }
    
    /// <summary>
    /// Per-type property ordering override. Null means inherit from context.
    /// </summary>
    public PropertyOrderingMode? PropertyOrdering { get; }

    /// <summary>
    /// Polymorphic configuration from [YamlSerializable] attribute on the context.
    /// Takes precedence over [YamlPolymorphic]/[YamlDerivedType] on the type itself.
    /// </summary>
    public PolymorphicInfo? PolymorphicConfig { get; }

    /// <summary>
    /// The custom converter type specified via [YamlConverter] attribute on the type.
    /// When set, the generator creates static ReadCore/WriteCore methods and wires up
    /// the GeneratedRead/GeneratedWrite delegates on the custom converter.
    /// </summary>
    public INamedTypeSymbol? CustomConverterType { get; }

    public TypeToGenerate(
        INamedTypeSymbol symbol, 
        PropertyOrderingMode? propertyOrdering = null, 
        PolymorphicInfo? polymorphicConfig = null,
        INamedTypeSymbol? customConverterType = null)
    {
        Symbol = symbol;
        PropertyOrdering = propertyOrdering;
        PolymorphicConfig = polymorphicConfig;
        CustomConverterType = customConverterType;
    }
}

/// <summary>
/// Represents polymorphic type information from [YamlPolymorphic] and [YamlDerivedType] attributes.
/// </summary>
internal sealed class PolymorphicInfo
{
    /// <summary>
    /// The property name used as type discriminator (e.g., "$type" or "kind").
    /// </summary>
    public string TypeDiscriminatorPropertyName { get; }
    
    /// <summary>
    /// Mappings from discriminator value to derived type symbol.
    /// </summary>
    public List<(string Discriminator, INamedTypeSymbol DerivedType)> DerivedTypes { get; }

    public PolymorphicInfo(string typeDiscriminatorPropertyName, List<(string, INamedTypeSymbol)> derivedTypes)
    {
        TypeDiscriminatorPropertyName = typeDiscriminatorPropertyName;
        DerivedTypes = derivedTypes;
    }
}

/// <summary>
/// Represents sibling discriminator information from [YamlSiblingDiscriminator] and [YamlDiscriminatorMapping] attributes.
/// </summary>
internal sealed class SiblingDiscriminatorInfo
{
    /// <summary>
    /// The name of the sibling property that contains the discriminator value.
    /// </summary>
    public string DiscriminatorPropertyName { get; }
    
    /// <summary>
    /// Mappings from discriminator value to concrete type symbol.
    /// </summary>
    public List<(string DiscriminatorValue, INamedTypeSymbol ConcreteType)> Mappings { get; }

    public SiblingDiscriminatorInfo(string discriminatorPropertyName, List<(string, INamedTypeSymbol)> mappings)
    {
        DiscriminatorPropertyName = discriminatorPropertyName;
        Mappings = mappings;
    }
}
