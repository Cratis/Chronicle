// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks the types that are views over a graph: they are anchored at a node, their relations follow edges
/// to other views, and they are not nodes or edges themselves.
/// </summary>
/// <remarks>
/// Only types carrying an attribute named <c language="csharp">ViewOfAttribute</c> are bound further, so the cost for every other type is
/// one walk over its attribute list.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GraphViewAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor RootRule = new(
        id: DiagnosticIds.GraphViewRootMustBeNode,
        title: "A graph view must be anchored at a node",
        messageFormat: "The view '{0}' is anchored at '{1}', which is not a node",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A view reads a property graph from one node label. The type given to ViewOf<TNode> must be marked with [Node].");

    static readonly DiagnosticDescriptor EdgeRule = new(
        id: DiagnosticIds.GraphViewRelationMustFollowEdge,
        title: "A relation of a graph view must follow an edge",
        messageFormat: "The relation '{0}' of the view '{1}' follows '{2}', which is not an edge",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A relation follows an edge of the graph. The type given to Follows<TEdge> must be marked with [Edge].");

    static readonly DiagnosticDescriptor TargetRule = new(
        id: DiagnosticIds.GraphViewRelationTargetMustBeView,
        title: "A relation of a graph view must lead to a view",
        messageFormat: "The relation '{0}' of the view '{1}' leads to '{2}', which is not a view",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A relation member holds a view, or a collection of a view. Its type, or the item type of the collection, must be marked with ViewOf<TNode>.");

    static readonly DiagnosticDescriptor SchemaRule = new(
        id: DiagnosticIds.GraphViewCannotBeNodeOrEdge,
        title: "A graph view cannot be a node or an edge",
        messageFormat: "The type '{0}' is a view over a graph and is also a node or an edge",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A node or an edge is what a projection writes to a graph; a view only reads it. A type is one or the other.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RootRule, EdgeRule, TargetRule, SchemaRule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        var viewOf = ViewOfOf(type);
        if (viewOf is null)
        {
            return;
        }

        var location = type.Locations.FirstOrDefault() ?? Location.None;
        if (WellKnownTypes.HasAttribute(type, WellKnownTypes.NodeAttributeName) || WellKnownTypes.HasAttribute(type, WellKnownTypes.EdgeAttributeName))
        {
            context.ReportDiagnostic(Diagnostic.Create(SchemaRule, location, type.Name));
        }

        if (viewOf.AttributeClass!.TypeArguments.FirstOrDefault() is { } root && !WellKnownTypes.HasAttribute(root, WellKnownTypes.NodeAttributeName))
        {
            context.ReportDiagnostic(Diagnostic.Create(RootRule, location, type.Name, root.Name));
        }

        foreach (var (symbol, memberType, follows) in Relations(type))
        {
            var memberLocation = symbol.Locations.FirstOrDefault() ?? location;
            if (follows.AttributeClass!.TypeArguments.FirstOrDefault() is { } edge && !WellKnownTypes.HasAttribute(edge, WellKnownTypes.EdgeAttributeName))
            {
                context.ReportDiagnostic(Diagnostic.Create(EdgeRule, memberLocation, symbol.Name, type.Name, edge.Name));
            }

            var target = TargetOf(memberType);
            if (ViewOfOf(target) is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(TargetRule, memberLocation, symbol.Name, type.Name, target.Name));
            }
        }
    }

    static AttributeData? ViewOfOf(ITypeSymbol type) =>
        type.GetAttributes().FirstOrDefault(attribute =>
            attribute.AttributeClass is { } attributeClass &&
            string.Equals(attributeClass.Name, "ViewOfAttribute", System.StringComparison.Ordinal) &&
            attributeClass.OriginalDefinition.ToDisplayString() == WellKnownTypes.ViewOfAttributeName);

    static IEnumerable<(ISymbol Symbol, ITypeSymbol Type, AttributeData Follows)> Relations(INamedTypeSymbol type)
    {
        var primaryConstructor = type.InstanceConstructors.OrderByDescending(constructor => constructor.Parameters.Length).FirstOrDefault();
        var parameters = primaryConstructor?.Parameters ?? [];
        foreach (var parameter in parameters)
        {
            var follows = FollowsOf(parameter) ?? FollowsOf(type.GetMembers(parameter.Name).OfType<IPropertySymbol>().FirstOrDefault());
            if (follows is not null)
            {
                yield return (parameter, parameter.Type, follows);
            }
        }

        foreach (var property in type.GetMembers().OfType<IPropertySymbol>().Where(property => !property.IsStatic && !parameters.Any(parameter => parameter.Name == property.Name)))
        {
            var follows = FollowsOf(property);
            if (follows is not null)
            {
                yield return (property, property.Type, follows);
            }
        }
    }

    static AttributeData? FollowsOf(ISymbol? symbol) =>
        symbol?.GetAttributes().FirstOrDefault(attribute =>
            attribute.AttributeClass is { } attributeClass &&
            string.Equals(attributeClass.Name, "FollowsAttribute", System.StringComparison.Ordinal) &&
            attributeClass.OriginalDefinition.ToDisplayString() == WellKnownTypes.FollowsAttributeName);

    static ITypeSymbol TargetOf(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array)
        {
            return array.ElementType;
        }

        if (type.SpecialType != SpecialType.System_String && type is INamedTypeSymbol named)
        {
            if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                return named.TypeArguments[0];
            }

            var enumerable = named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
                ? named
                : named.AllInterfaces.FirstOrDefault(_ => _.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
            if (enumerable is not null)
            {
                return enumerable.TypeArguments[0];
            }
        }

        return type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
    }
}
