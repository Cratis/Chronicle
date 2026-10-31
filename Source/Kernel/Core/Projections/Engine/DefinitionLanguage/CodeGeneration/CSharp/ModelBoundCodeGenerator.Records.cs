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

    static IEnumerable<RecordDeclarationSyntax> CreateRecordDeclarations(RecordScope scope) => CollectUniqueRecordDeclarations(scope);

    static IEnumerable<RecordScope> ChildScopes(RecordScope scope) => scope.Schema.Properties
        .Select(property => ChildScope(scope, property)).OfType<RecordScope>();

    static RecordScope? ChildScope(RecordScope parent, KeyValuePair<string, JsonSchemaProperty> property)
    {
        var actual = property.Value.ActualSchema;
        var collection = actual.Type.HasFlag(JsonObjectType.Array);
        var source = collection ? actual.Item : property.Value;
        var schema = source?.ActualSchema;
        if (schema?.Type.HasFlag(JsonObjectType.Object) != true)
        {
            return null;
        }
        var definition = CSharpDefinitionSupport.ChildDefinition(parent.Definition, property.Key);
        return new RecordScope(parent.Names.Resolve(source!, parent.Name + property.Key), schema, definition, parent.Names)
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
        var fallback = scope.Name + rendering.Target.Path;
        var childSchema = rendering.Schema.ActualSchema.Item ?? rendering.Schema;
        var childType = ParseTypeName(scope.Names.Resolve(childSchema, fallback));
        var attributes = ModelBoundRelationships.PropertyAttributes(scope.Definition, rendering.Target, childType, scope.IsCollection).ToList();
        AddParameterAttributes(rendering, attributes);
        var comments = ModelBoundLimitations.Comments(scope.Definition, rendering.Target.Path, scope.Description)
            .SelectMany(comment => new[] { Comment(comment), ElasticCarriageReturnLineFeed });
        return Parameter(Identifier(rendering.Target.Path))
            .WithType(ParseTypeName(CSharpDefinitionSupport.ModelType(rendering.Schema, fallback, scope.Names)))
            .WithAttributeLists(List(attributes.Select(attribute => ParameterAttribute(attribute, scope.IsNested))))
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

    static AttributeListSyntax ParameterAttribute(AttributeSyntax attribute, bool isNested) => AttributeList(SingletonSeparatedList(attribute))
        .WithTarget(IsPropertyAttribute(attribute, isNested) ? AttributeTargetSpecifier(Token(SyntaxKind.PropertyKeyword)) : null);

    static bool IsPropertyAttribute(AttributeSyntax attribute, bool isNested) => attribute.Name.ToString() == "FromAll"
        || (isNested && attribute.Name is GenericNameSyntax generic && generic.Identifier.Text == "SetFromContext");

    sealed record RecordScope(string Name, JsonSchema Schema, ProjectionDefinition Definition, RecordNames Names)
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
