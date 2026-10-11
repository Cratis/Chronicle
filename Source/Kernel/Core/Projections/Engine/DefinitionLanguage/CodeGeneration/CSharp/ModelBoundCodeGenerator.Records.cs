// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;

/// <summary>
/// Renders record declarations and their child scopes.
/// </summary>
public partial class ModelBoundCodeGenerator
{
    static RecordDeclarationSyntax CreateRecordDeclaration(RecordScope scope)
    {
        var attributes = ModelBoundRelationships.ClassAttributes(scope.Definition, scope.IsCollection)
            .Select(attribute => AttributeList(SingletonSeparatedList(attribute)));
        return RecordDeclaration(Token(SyntaxKind.RecordKeyword), scope.Name)
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(ParameterList(SeparatedList(CreateRecordParameters(scope))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            .WithAttributeLists(List(attributes));
    }

    static IEnumerable<RecordDeclarationSyntax> CreateRecordDeclarations(RecordScope scope)
    {
        yield return CreateRecordDeclaration(scope);
        foreach (var child in ChildScopes(scope))
        {
            foreach (var record in CreateRecordDeclarations(child))
            {
                yield return record;
            }
        }
    }

    static IEnumerable<RecordScope> ChildScopes(RecordScope scope) => scope.Schema.Properties
        .Select(property => ChildScope(scope, property)).OfType<RecordScope>();

    static RecordScope? ChildScope(RecordScope parent, KeyValuePair<string, JsonSchemaProperty> property)
    {
        var collection = property.Value.ActualSchema.Type == JsonObjectType.Array;
        var schema = collection ? property.Value.Item?.ActualSchema : property.Value.ActualSchema;
        if (schema?.Type != JsonObjectType.Object)
        {
            return null;
        }
        var definition = CSharpDefinitionSupport.ChildDefinition(parent.Definition, property.Key);
        return new RecordScope(CSharpDefinitionSupport.RecordName(schema, parent.Name + property.Key), schema, definition)
        {
            IsCollection = collection,
            IsNested = !collection,
            KeyProperty = collection ? definition.IdentifiedBy.Path : null,
            Description = $"{(collection ? "children" : "nested")} '{property.Key}'"
        };
    }

    static IEnumerable<ParameterSyntax> CreateRecordParameters(RecordScope scope)
    {
        if (scope.Schema.Properties.Count == 0)
        {
            return [];
        }
        var mappings = CollectPropertyMappings(scope.Definition);
        var keyProperty = scope.ResolveKey();
        return scope.Schema.Properties.Select(property => CreateRecordParameter(
            new ParameterRendering(scope, new PropertyPath(property.Key), property.Value, mappings.GetValueOrDefault(property.Key)) { IsKey = property.Key == keyProperty }));
    }

    static ParameterSyntax CreateRecordParameter(ParameterRendering rendering)
    {
        var scope = rendering.Scope;
        var fallback = scope.Schema.Title + rendering.Target.Path;
        var childSchema = rendering.Schema.Item?.ActualSchema ?? rendering.Schema.ActualSchema;
        var childType = ParseTypeName(CSharpDefinitionSupport.RecordName(childSchema, fallback));
        var attributes = ModelBoundRelationships.PropertyAttributes(scope.Definition, rendering.Target, childType, scope.IsCollection).ToList();
        AddParameterAttributes(rendering, attributes);
        var comments = ModelBoundLimitations.Comments(scope.Definition, rendering.Target.Path, scope.Description)
            .SelectMany(comment => new[] { Comment(comment), ElasticCarriageReturnLineFeed });
        return Parameter(Identifier(rendering.Target.Path))
            .WithType(ParseTypeName(CSharpDefinitionSupport.ModelType(rendering.Schema, fallback)))
            .WithAttributeLists(List(attributes.Select(ParameterAttribute)))
            .WithLeadingTrivia(comments);
    }

    static void AddParameterAttributes(ParameterRendering rendering, List<AttributeSyntax> attributes)
    {
        if (rendering.IsKey) attributes.Add(Attribute(IdentifierName("Key")));
        if (rendering.Mappings is not null)
        {
            attributes.AddRange(CreatePropertyAttributes(new(rendering.Scope, rendering.Target, rendering.Mappings)));
        }
    }

    static AttributeListSyntax ParameterAttribute(AttributeSyntax attribute) => AttributeList(SingletonSeparatedList(attribute))
        .WithTarget(attribute.Name.ToString() == "FromAll" ? AttributeTargetSpecifier(Token(SyntaxKind.PropertyKeyword)) : null);

    sealed record RecordScope(string Name, JsonSchema Schema, ProjectionDefinition Definition)
    {
        public bool IsCollection { get; init; }
        public bool IsNested { get; init; }
        public string? KeyProperty { get; init; }
        public string Description { get; init; } = $"projection '{Name}'";

        public string? ResolveKey() => IsNested || KeyProperty is not null
            ? KeyProperty
            : Schema.Properties.Keys.FirstOrDefault(property => property.Equals("Id", StringComparison.OrdinalIgnoreCase)) ?? Schema.Properties.Keys.FirstOrDefault();
    }

    sealed record ParameterRendering(RecordScope Scope, PropertyPath Target, JsonSchemaProperty Schema, PropertyInfo? Mappings)
    {
        public bool IsKey { get; init; }
    }
}
