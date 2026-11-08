// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks the types that are nodes and edges in a graph projection: they have only primitive properties,
/// exactly one key (a node) or at most one (an edge), and are not also read models.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GraphElementAnalyzer : DiagnosticAnalyzer
{
    static readonly ImmutableHashSet<string> WellKnownValueTypes = ImmutableHashSet.Create("System.Guid", "System.DateTimeOffset", "System.DateOnly", "System.TimeOnly", "System.TimeSpan");

    static readonly DiagnosticDescriptor NestedObjectRule = new(
        id: DiagnosticIds.GraphElementPropertyMustBePrimitive,
        title: "A node or an edge can only have primitive properties",
        messageFormat: "The property '{0}' of the {1} '{2}' is an object. A graph stores primitives and arrays of primitives only.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Nodes and edges in a property graph hold primitive values and arrays of primitive values. A nested object cannot be stored as a property.");

    static readonly DiagnosticDescriptor KeyRule = new(
        id: DiagnosticIds.GraphElementKeyMustBeDeclaredOnce,
        title: "A node needs exactly one key",
        messageFormat: "The {0} '{1}' has {2} members with the [Key] attribute, {3}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A node is identified by exactly one key property. An edge is identified by its type, source and target, and can additionally have one key property that identifies it among edges of the same type between the same two nodes.");

    static readonly DiagnosticDescriptor ReadModelRule = new(
        id: DiagnosticIds.GraphElementCannotBeReadModel,
        title: "A node or an edge cannot be a read model",
        messageFormat: "The type '{0}' is a node or an edge in a graph and is also marked as a read model",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A node or an edge is projected onto a graph, a read model onto a document or relational store. A type is one or the other.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(NestedObjectRule, KeyRule, ReadModelRule);

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
        var isNode = WellKnownTypes.HasAttribute(type, WellKnownTypes.NodeAttributeName);
        var isEdge = WellKnownTypes.HasAttribute(type, WellKnownTypes.EdgeAttributeName);
        if (!isNode && !isEdge)
        {
            return;
        }

        var kind = isNode ? "node" : "edge";
        var location = type.Locations.FirstOrDefault() ?? Location.None;
        if (WellKnownTypes.HasAttribute(type, WellKnownTypes.ReadModelAttributeName))
        {
            context.ReportDiagnostic(Diagnostic.Create(ReadModelRule, location, type.Name));
        }

        var members = Members(type).ToArray();
        var keys = members.Count(member => WellKnownTypes.HasAttribute(member.Symbol, WellKnownTypes.KeyAttributeName));
        if (isNode && keys != 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(KeyRule, location, kind, type.Name, keys, "but a node needs exactly one"));
        }
        else if (isEdge && keys > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(KeyRule, location, kind, type.Name, keys, "but an edge can have at most one"));
        }

        foreach (var (symbol, _) in members.Where(member => !IsPrimitive(member.Type)))
        {
            context.ReportDiagnostic(Diagnostic.Create(NestedObjectRule, symbol.Locations.FirstOrDefault() ?? location, symbol.Name, kind, type.Name));
        }
    }

    static IEnumerable<(ISymbol Symbol, ITypeSymbol Type)> Members(INamedTypeSymbol type)
    {
        var primaryConstructor = type.InstanceConstructors.OrderByDescending(constructor => constructor.Parameters.Length).FirstOrDefault();
        var parameters = primaryConstructor?.Parameters ?? [];
        foreach (var parameter in parameters)
        {
            yield return (parameter, parameter.Type);
        }

        foreach (var property in type.GetMembers().OfType<IPropertySymbol>().Where(property => !property.IsStatic && !property.IsImplicitlyDeclared && !parameters.Any(parameter => parameter.Name == property.Name)))
        {
            yield return (property, property.Type);
        }
    }

    static bool IsPrimitive(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array)
        {
            return IsPrimitive(array.ElementType);
        }

        if (type is INamedTypeSymbol { IsGenericType: true } generic)
        {
            var definition = generic.OriginalDefinition.SpecialType;
            if (definition == SpecialType.System_Nullable_T)
            {
                return IsPrimitive(generic.TypeArguments[0]);
            }

            if (definition == SpecialType.System_Collections_Generic_IEnumerable_T ||
                definition == SpecialType.System_Collections_Generic_IList_T ||
                definition == SpecialType.System_Collections_Generic_ICollection_T ||
                definition == SpecialType.System_Collections_Generic_IReadOnlyList_T ||
                definition == SpecialType.System_Collections_Generic_IReadOnlyCollection_T ||
                generic.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.List<T>")
            {
                return IsPrimitive(generic.TypeArguments[0]);
            }
        }

        if (type.SpecialType != SpecialType.None || type.TypeKind == TypeKind.Enum)
        {
            return type.SpecialType != SpecialType.System_Object;
        }

        if (WellKnownValueTypes.Contains(type.ToDisplayString()))
        {
            return true;
        }

        // A concept wraps a primitive.
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name.StartsWith("ConceptAs", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
