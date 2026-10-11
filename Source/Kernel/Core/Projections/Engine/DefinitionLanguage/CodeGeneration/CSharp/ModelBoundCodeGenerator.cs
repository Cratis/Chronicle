// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;

/// <summary>
/// Generates model-bound C# read model code from a <see cref="ProjectionDefinition"/> using Roslyn Syntax Factory.
/// </summary>
public partial class ModelBoundCodeGenerator
{
    /// <summary>
    /// How far each record parameter is indented from the record declaration.
    /// </summary>
    const int RecordParameterIndentation = 4;

    /// <summary>
    /// Generates model-bound read model C# code.
    /// </summary>
    /// <param name="definition">The projection definition to generate code from.</param>
    /// <param name="readModelDefinition">The read model definition the projection targets.</param>
    /// <returns>Generated <see cref="CompilationUnitSyntax"/> for model-bound read model.</returns>
    public CompilationUnitSyntax Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        var schema = readModelDefinition.GetSchemaForLatestGeneration();
        var names = new RecordNames();
        var scope = new RecordScope(names.Resolve(schema, schema.Title!), schema, definition, names);
        var usings = new[]
        {
            UsingDirective(ParseName("Cratis.Chronicle.Keys")),
            UsingDirective(ParseName("Cratis.Chronicle.Projections.ModelBound"))
        };
        var compilationUnit = CompilationUnit()
            .WithUsings(List(usings))
            .WithMembers(List<MemberDeclarationSyntax>(CreateRecordDeclarations(scope)))
            .NormalizeWhitespace();

        return (CompilationUnitSyntax)new RecordParameterFormatter(RecordParameterIndentation).Visit(compilationUnit);
    }

    static List<AttributeSyntax> CreatePropertyAttributes(PropertyRendering rendering)
    {
        var property = rendering.Mappings;
        var attributes = new List<AttributeSyntax>();
        AddEveryAttributes(property, rendering.Scope.Definition.SubscribesToAllEvents, attributes);
        attributes.AddRange(property.AddFroms.Select(mapping => CreateMappingAttribute("AddFrom", new(mapping.EventTypeName, mapping.EventPropertyName), rendering.Target)));
        attributes.AddRange(property.SubtractFroms.Select(mapping => CreateMappingAttribute("SubtractFrom", new(mapping.EventTypeName, mapping.EventPropertyName), rendering.Target)));
        attributes.AddRange(property.Increments.Select(eventType => CreateSimpleAttribute("Increment", eventType)));
        attributes.AddRange(property.Decrements.Select(eventType => CreateSimpleAttribute("Decrement", eventType)));
        attributes.AddRange(property.Counts.Select(eventType => CreateSimpleAttribute("Count", eventType)));
        var noAutoMap = rendering.Scope.Definition.AutoMap == AutoMap.Disabled;
        attributes.AddRange(property.SetFroms
            .Where(mapping => mapping.EventPropertyName != rendering.Target.Path || noAutoMap)
            .Select(mapping => CreateMappingAttribute("SetFrom", new(mapping.EventTypeName, mapping.EventPropertyName), rendering.Target)));
        attributes.AddRange(property.SetValues.Select(value => CreateSetValueAttribute(new(value.EventTypeName, value.Value, value.IsText))));
        attributes.AddRange(property.Clears.Select(eventType => CreateSimpleAttribute("ClearWith", eventType)));
        return attributes;
    }

    static void AddEveryAttributes(PropertyInfo property, bool fromAll, List<AttributeSyntax> attributes)
    {
        if (property.FromAllCount) attributes.Add(CreateEveryAttribute(fromAll, "count"));
        if (property.FromAllIncrement) attributes.Add(CreateEveryAttribute(fromAll, "increment"));
        if (property.FromAllDecrement) attributes.Add(CreateEveryAttribute(fromAll, "decrement"));
        if (property.FromAllContextProperty is not null) attributes.Add(CreateEveryAttribute(fromAll, contextProperty: property.FromAllContextProperty));
        if (property.FromAllProperty is not null) attributes.Add(CreateEveryAttribute(fromAll, property.FromAllProperty));
    }

    static AttributeSyntax CreateMappingAttribute(string attributeName, MappingSource mapping, PropertyPath target)
    {
        var source = ProjectionExpressions.ReadValue(mapping.Expression);
        if (attributeName == "SetFrom" && source.Kind is ProjectionValueKind.EventContextProperty or ProjectionValueKind.EventSourceId)
        {
            var contextProperty = source.Kind == ProjectionValueKind.EventSourceId ? "EventSourceId" : CSharpDefinitionSupport.ContextPath(source.Value);
            return CreateSimpleAttribute("SetFromContext", mapping.EventType).WithArgumentList(AttributeArgumentList(
                SingletonSeparatedList(AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(contextProperty))))));
        }
        var attribute = GenericName(attributeName).WithTypeArgumentList(
            TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(mapping.EventType.Value))));
        if (mapping.Expression.Value != target.Path)
        {
            var nameofExpression = InvocationExpression(IdentifierName("nameof")).WithArgumentList(
                ArgumentList(SingletonSeparatedList(Argument(MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression, IdentifierName(mapping.EventType.Value), IdentifierName(mapping.Expression.Value))))));
            return Attribute(attribute).WithArgumentList(AttributeArgumentList(SingletonSeparatedList(AttributeArgument(nameofExpression))));
        }
        return Attribute(attribute);
    }

    static AttributeSyntax CreateSimpleAttribute(string attributeName, EventTypeId eventType) => Attribute(
        GenericName(attributeName).WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(eventType.Value)))));

    static AttributeSyntax CreateEveryAttribute(bool fromAll, PropertyExpression? property = null, PropertyExpression? contextProperty = null)
    {
        var attribute = Attribute(IdentifierName(fromAll ? "FromAll" : "FromEvery"));
        var arguments = new List<AttributeArgumentSyntax>();
        if (contextProperty is not null)
        {
            arguments.Add(AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(contextProperty.Value)))
                .WithNameColon(NameColon(IdentifierName("contextProperty"))));
        }
        if (property is not null)
        {
            arguments.Add(AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(property.Value)))
                .WithNameColon(NameColon(IdentifierName("property"))));
        }
        return arguments.Count == 0 ? attribute : attribute.WithArgumentList(AttributeArgumentList(SeparatedList(arguments)));
    }

    static AttributeSyntax CreateSetValueAttribute(ConstantMapping mapping)
    {
        var attribute = GenericName("SetValue").WithTypeArgumentList(
            TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(mapping.EventType.Value))));
        return Attribute(attribute).WithArgumentList(AttributeArgumentList(SingletonSeparatedList(AttributeArgument(ConstantLiteral(mapping)))));
    }

    static LiteralExpressionSyntax ConstantLiteral(ConstantMapping mapping)
    {
        // Text remains text even when its spelling could be parsed as a number or boolean.
        if (mapping.IsText) return LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(mapping.Value));
        if (bool.TryParse(mapping.Value, out var boolean)) return LiteralExpression(boolean ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
        if (int.TryParse(mapping.Value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var integer)) return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(integer));
        if (long.TryParse(mapping.Value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var longInteger)) return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(longInteger));
        if (double.TryParse(mapping.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number)) return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(number));
        return LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(mapping.Value));
    }

    sealed record PropertyRendering(RecordScope Scope, PropertyPath Target, PropertyInfo Mappings);

    sealed record MappingSource(EventTypeId EventType, PropertyExpression Expression);

    sealed record ConstantMapping(EventTypeId EventType, string Value, bool IsText);

    sealed class PropertyInfo
    {
        public List<(string EventTypeName, string EventPropertyName)> SetFroms { get; } = [];
        public List<(string EventTypeName, string Value, bool IsText)> SetValues { get; } = [];
        public List<string> Clears { get; } = [];
        public List<(string EventTypeName, string EventPropertyName)> AddFroms { get; } = [];
        public List<(string EventTypeName, string EventPropertyName)> SubtractFroms { get; } = [];
        public List<string> Increments { get; } = [];
        public List<string> Decrements { get; } = [];
        public List<string> Counts { get; } = [];
        public bool FromAllCount { get; set; }
        public bool FromAllIncrement { get; set; }
        public bool FromAllDecrement { get; set; }
        public string? FromAllProperty { get; set; }
        public string? FromAllContextProperty { get; set; }
    }
}
