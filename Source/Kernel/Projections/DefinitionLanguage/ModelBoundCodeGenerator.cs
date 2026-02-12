// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NJsonSchema;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Generates model-bound C# read model code from a <see cref="ProjectionDefinition"/> using Roslyn Syntax Factory.
/// </summary>
public class ModelBoundCodeGenerator
{
    /// <summary>
    /// Generates model-bound read model C# code.
    /// </summary>
    /// <param name="definition">The projection definition to generate code from.</param>
    /// <param name="readModelDefinition">The read model definition the projection targets.</param>
    /// <returns>Generated <see cref="CompilationUnitSyntax"/> for model-bound read model.</returns>
    public CompilationUnitSyntax Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        var schema = readModelDefinition.GetSchemaForLatestGeneration();
        var readModelName = schema.Title!;

        var usings = new[]
        {
            UsingDirective(ParseName("Cratis.Chronicle.Keys")),
            UsingDirective(ParseName("Cratis.Chronicle.Projections.ModelBound"))
        };

        return CompilationUnit()
            .WithUsings(List(usings))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(CreateRecordDeclaration(readModelName, schema, definition)))
            .NormalizeWhitespace();
    }

    static string GetEventPropertyName(string expression)
    {
        if (expression == WellKnownExpressions.EventSourceId) return "Id";
        return expression;
    }

    static string GetCSharpType(JsonSchemaProperty propertySchema)
    {
        return propertySchema.Type switch
        {
            JsonObjectType.String => "string",
            JsonObjectType.Integer => "int",
            JsonObjectType.Number => "decimal",
            JsonObjectType.Boolean => "bool",
            _ => "object"
        };
    }

    static string NormalizeExpression(string expression)
    {
        if (expression.StartsWith("+=", StringComparison.Ordinal))
        {
            return $"{WellKnownExpressions.Add}({expression[2..].Trim()})";
        }

        if (expression.StartsWith("-=", StringComparison.Ordinal))
        {
            return $"{WellKnownExpressions.Subtract}({expression[2..].Trim()})";
        }

        if (expression.Equals("increment", StringComparison.Ordinal))
        {
            return WellKnownExpressions.Increment;
        }

        if (expression.Equals("decrement", StringComparison.Ordinal))
        {
            return WellKnownExpressions.Decrement;
        }

        if (expression.Equals("count", StringComparison.Ordinal))
        {
            return WellKnownExpressions.Count;
        }

        return expression;
    }

    RecordDeclarationSyntax CreateRecordDeclaration(
        string readModelName,
        JsonSchema schema,
        ProjectionDefinition definition)
    {
        var attributes = CreateClassLevelAttributes(definition);
        var parameters = CreateRecordParameters(schema, definition);

        var recordDecl = RecordDeclaration(Token(SyntaxKind.RecordKeyword), readModelName)
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(ParameterList(SeparatedList(parameters)));

        if (attributes.Count > 0)
        {
            recordDecl = recordDecl.WithAttributeLists(List(attributes));
        }

        return recordDecl;
    }

    List<AttributeListSyntax> CreateClassLevelAttributes(ProjectionDefinition definition)
    {
        var attributeLists = new List<AttributeListSyntax>();

        foreach (var from in definition.From)
        {
            var eventTypeName = from.Key.Id.Value;
            var attribute = Attribute(
                GenericName("FromEvent")
                    .WithTypeArgumentList(
                        TypeArgumentList(
                            SingletonSeparatedList<TypeSyntax>(
                                IdentifierName(eventTypeName)))));

            attributeLists.Add(AttributeList(SingletonSeparatedList(attribute)));
        }

        return attributeLists;
    }

    List<ParameterSyntax> CreateRecordParameters(JsonSchema schema, ProjectionDefinition definition)
    {
        var parameters = new List<ParameterSyntax>();

        if (schema.Properties?.Count > 0)
        {
            var propertyInfos = CollectPropertyMappings(definition);
            var isFirst = true;

            foreach (var schemaProp in schema.Properties)
            {
                var propName = schemaProp.Key;
                var propType = GetCSharpType(schemaProp.Value);
                var attributes = new List<AttributeSyntax>();

                if (isFirst)
                {
                    attributes.Add(Attribute(IdentifierName("Key")));
                    isFirst = false;
                }

                if (propertyInfos.TryGetValue(propName, out var propInfo))
                {
                    attributes.AddRange(CreatePropertyAttributes(propInfo, propName));
                }

                var parameter = Parameter(Identifier(propName))
                    .WithType(ParseTypeName(propType));

                if (attributes.Count > 0)
                {
                    parameter = parameter.WithAttributeLists(
                        SingletonList(AttributeList(SeparatedList(attributes))));
                }

                parameters.Add(parameter);
            }
        }

        return parameters;
    }

    Dictionary<string, PropertyInfo> CollectPropertyMappings(ProjectionDefinition definition)
    {
        var propertyInfos = new Dictionary<string, PropertyInfo>();

        foreach (var from in definition.From)
        {
            var eventTypeName = from.Key.Id.Value;
            var fromDef = from.Value;

            foreach (var prop in fromDef.Properties)
            {
                var propertyName = prop.Key.Path;
                var normalizedExpression = NormalizeExpression(prop.Value);

                if (!propertyInfos.TryGetValue(propertyName, out var propInfo))
                {
                    propInfo = new PropertyInfo { PropertyName = propertyName };
                    propertyInfos[propertyName] = propInfo;
                }

                if (normalizedExpression.StartsWith($"{WellKnownExpressions.Add}(", StringComparison.Ordinal) && normalizedExpression.EndsWith(')'))
                {
                    var innerExpr = normalizedExpression[(WellKnownExpressions.Add.Length + 1)..^1];
                    propInfo.AddFroms.Add((eventTypeName, GetEventPropertyName(innerExpr)));
                }
                else if (normalizedExpression.StartsWith($"{WellKnownExpressions.Subtract}(", StringComparison.Ordinal) && normalizedExpression.EndsWith(')'))
                {
                    var innerExpr = normalizedExpression[(WellKnownExpressions.Subtract.Length + 1)..^1];
                    propInfo.SubtractFroms.Add((eventTypeName, GetEventPropertyName(innerExpr)));
                }
                else if (normalizedExpression == WellKnownExpressions.Increment)
                {
                    propInfo.Increments.Add(eventTypeName);
                }
                else if (normalizedExpression == WellKnownExpressions.Decrement)
                {
                    propInfo.Decrements.Add(eventTypeName);
                }
                else if (normalizedExpression == WellKnownExpressions.Count)
                {
                    propInfo.Counts.Add(eventTypeName);
                }
                else
                {
                    propInfo.SetFroms.Add((eventTypeName, GetEventPropertyName(normalizedExpression)));
                }
            }
        }

        return propertyInfos;
    }

    List<AttributeSyntax> CreatePropertyAttributes(PropertyInfo propInfo, string propName)
    {
        var attributes = new List<AttributeSyntax>();

        foreach (var addFrom in propInfo.AddFroms)
        {
            attributes.Add(CreateMappingAttribute("AddFrom", addFrom.EventTypeName, addFrom.EventPropertyName, propName));
        }

        foreach (var subtractFrom in propInfo.SubtractFroms)
        {
            attributes.Add(CreateMappingAttribute("SubtractFrom", subtractFrom.EventTypeName, subtractFrom.EventPropertyName, propName));
        }

        foreach (var incrementFrom in propInfo.Increments)
        {
            attributes.Add(CreateSimpleAttribute("Increment", incrementFrom));
        }

        foreach (var decrementFrom in propInfo.Decrements)
        {
            attributes.Add(CreateSimpleAttribute("Decrement", decrementFrom));
        }

        foreach (var countFrom in propInfo.Counts)
        {
            attributes.Add(CreateSimpleAttribute("Count", countFrom));
        }

        // SetFrom is only needed when the event property name differs from the model property name
        // When they match, AutoMap handles it through the class-level [FromEvent<>] attribute
        foreach (var setFrom in propInfo.SetFroms)
        {
            if (setFrom.EventPropertyName != propName)
            {
                attributes.Add(CreateMappingAttribute("SetFrom", setFrom.EventTypeName, setFrom.EventPropertyName, propName));
            }
        }

        return attributes;
    }

    AttributeSyntax CreateMappingAttribute(string attributeName, string eventTypeName, string eventPropertyName, string propName)
    {
        var attribute = GenericName(attributeName)
            .WithTypeArgumentList(
                TypeArgumentList(
                    SingletonSeparatedList<TypeSyntax>(
                        IdentifierName(eventTypeName))));

        if (eventPropertyName != propName)
        {
            var nameofExpression = InvocationExpression(
                IdentifierName("nameof"))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName(eventTypeName),
                                    IdentifierName(eventPropertyName))))));

            return Attribute(attribute)
                .WithArgumentList(
                    AttributeArgumentList(
                        SingletonSeparatedList(
                            AttributeArgument(nameofExpression))));
        }

        return Attribute(attribute);
    }

    AttributeSyntax CreateSimpleAttribute(string attributeName, string eventTypeName)
    {
        var attribute = GenericName(attributeName)
            .WithTypeArgumentList(
                TypeArgumentList(
                    SingletonSeparatedList<TypeSyntax>(
                        IdentifierName(eventTypeName))));

        return Attribute(attribute);
    }

    sealed class PropertyInfo
    {
        public string PropertyName { get; set; } = string.Empty;
        public List<(string EventTypeName, string EventPropertyName)> SetFroms { get; } = [];
        public List<(string EventTypeName, string EventPropertyName)> AddFroms { get; } = [];
        public List<(string EventTypeName, string EventPropertyName)> SubtractFroms { get; } = [];
        public List<string> Increments { get; } = [];
        public List<string> Decrements { get; } = [];
        public List<string> Counts { get; } = [];
    }
}
