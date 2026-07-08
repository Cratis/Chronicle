// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that emits an informational heads-up when a model-bound read model property is explicitly sourced
/// (via <c>[SetFrom]</c>, <c>[SetFromContext]</c>, <c>[SetValue]</c>, <c>[AddFrom]</c>, <c>[SubtractFrom]</c>,
/// or <c>[Join]</c>) but another value-mapped event referenced by the same projection carries an identically
/// named property that AutoMap will write on top of it. It is informational — not a warning — because the
/// collision has two legitimate resolutions and only the developer knows which was intended: fence the property
/// with property-level <c>[NoAutoMap]</c>, or leave it because updates from the other event are wanted.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AutoMapSameNamePropertyCollisionAnalyzer : DiagnosticAnalyzer
{
    const string ModelBoundNamespace = "Cratis.Chronicle.Projections.ModelBound";
    const string NoAutoMapAttributeName = "Cratis.Chronicle.Projections.NoAutoMapAttribute";

    static readonly HashSet<string> NonAggregateAttributeNames = new(StringComparer.Ordinal)
    {
        "FromEventAttribute",
        "SetFromAttribute",
        "JoinAttribute",
        "SetFromContextAttribute",
        "ChildrenFromAttribute",
        "AddFromAttribute",
        "SubtractFromAttribute",
        "RemovedWithAttribute",
        "RemovedWithJoinAttribute",
        "FromEveryAttribute"
    };

    static readonly HashSet<string> AggregateAttributeNames = new(StringComparer.Ordinal)
    {
        "CountAttribute",
        "IncrementAttribute",
        "DecrementAttribute"
    };

    static readonly HashSet<string> ExplicitSourceAttributeNames = new(StringComparer.Ordinal)
    {
        "SetFromAttribute",
        "SetFromContextAttribute",
        "SetValueAttribute",
        "AddFromAttribute",
        "SubtractFromAttribute",
        "JoinAttribute"
    };

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.AutoMapSameNamePropertyCollision,
        title: "Explicitly sourced read model property may be overwritten by AutoMap",
        messageFormat: "Property '{0}' is set explicitly, but event '{1}' (also referenced by this projection) carries a property named '{0}' that AutoMap will write on top of it. If '{1}' should not overwrite it, add [NoAutoMap] to '{0}'. If updates from '{1}' are intended (for example a later event updating the value by name), this is fine — no change is needed.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "In a model-bound projection, AutoMap runs per event across every event the projection references — it does not defer to explicit setters. A property set with [SetFrom<A>] (or another value setter) can therefore be overwritten when another referenced event B carries a property of the same name and AutoMap writes B's value on top. This is reported as information, not a warning, because the collision is sometimes intended (a later event legitimately updating the value by name); the two resolutions are to add [NoAutoMap] to the property, or to leave it. Name matching compares the C# member names (OrdinalIgnoreCase) and does not account for explicit [SetFrom<T>(\"eventProperty\")] targets or a custom INamingPolicy, so it is an approximation.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    static void AnalyzeType(SymbolAnalysisContext context)
    {
        var typeSymbol = (INamedTypeSymbol)context.Symbol;

        // A class-level [NoAutoMap] disables AutoMap for the whole read model, so no property can be
        // overwritten by name-AutoMap and there is nothing to flag.
        if (HasNoAutoMap(typeSymbol))
        {
            return;
        }

        var subscribedEvents = CollectSubscribedEvents(typeSymbol);
        if (subscribedEvents.Count == 0)
        {
            return;
        }

        foreach (var member in GetPropertiesAndParameters(typeSymbol))
        {
            AnalyzeMember(context, member, subscribedEvents);
        }
    }

    static void AnalyzeMember(SymbolAnalysisContext context, ISymbol member, IReadOnlyDictionary<INamedTypeSymbol, (bool HasNonAggregate, bool HasAggregate)> subscribedEvents)
    {
        var explicitSource = GetExplicitSourceEvent(member);
        if (explicitSource is null || HasNoAutoMap(member))
        {
            return;
        }

        foreach (var subscribed in subscribedEvents)
        {
            var eventType = subscribed.Key;
            var contribution = subscribed.Value;
            var isAggregateOnly = contribution.HasAggregate && !contribution.HasNonAggregate;

            if (SymbolEqualityComparer.Default.Equals(eventType, explicitSource) || isAggregateOnly)
            {
                continue;
            }

            if (!HasPublicInstancePropertyNamed(eventType, member.Name))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                member.Locations.FirstOrDefault(),
                member.Name,
                eventType.Name));

            // Report at most once per property.
            return;
        }
    }

    static Dictionary<INamedTypeSymbol, (bool HasNonAggregate, bool HasAggregate)> CollectSubscribedEvents(INamedTypeSymbol typeSymbol)
    {
        var events = new Dictionary<INamedTypeSymbol, (bool HasNonAggregate, bool HasAggregate)>(SymbolEqualityComparer.Default);

        AddContributions(events, typeSymbol.GetAttributes());

        foreach (var member in GetPropertiesAndParameters(typeSymbol))
        {
            AddContributions(events, member.GetAttributes());
        }

        return events;
    }

    static void AddContributions(Dictionary<INamedTypeSymbol, (bool HasNonAggregate, bool HasAggregate)> events, ImmutableArray<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (!TryGetModelBoundEvent(attribute, out var eventType, out var isAggregate))
            {
                continue;
            }

            events.TryGetValue(eventType, out var existing);
            events[eventType] = (
                existing.HasNonAggregate || !isAggregate,
                existing.HasAggregate || isAggregate);
        }
    }

    static bool TryGetModelBoundEvent(AttributeData attribute, out INamedTypeSymbol eventType, out bool isAggregate)
    {
        eventType = null!;
        isAggregate = false;

        if (attribute.AttributeClass is not { } attributeClass ||
            attributeClass.ContainingNamespace?.ToDisplayString() != ModelBoundNamespace ||
            attributeClass.TypeArguments.Length != 1 ||
            attributeClass.TypeArguments[0] is not INamedTypeSymbol resolvedEventType)
        {
            return false;
        }

        var name = attributeClass.Name;
        if (NonAggregateAttributeNames.Contains(name))
        {
            eventType = resolvedEventType;
            isAggregate = false;
            return true;
        }

        if (AggregateAttributeNames.Contains(name))
        {
            eventType = resolvedEventType;
            isAggregate = true;
            return true;
        }

        return false;
    }

    static INamedTypeSymbol? GetExplicitSourceEvent(ISymbol member) =>
        member.GetAttributes()
            .Where(attribute =>
                attribute.AttributeClass is { TypeArguments.Length: 1 } attributeClass &&
                attributeClass.ContainingNamespace?.ToDisplayString() == ModelBoundNamespace &&
                ExplicitSourceAttributeNames.Contains(attributeClass.Name) &&
                attributeClass.TypeArguments[0] is INamedTypeSymbol)
            .Select(attribute => attribute.AttributeClass!.TypeArguments[0] as INamedTypeSymbol)
            .FirstOrDefault();

    static bool HasNoAutoMap(ISymbol member) =>
        member.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == NoAutoMapAttributeName);

    static bool HasPublicInstancePropertyNamed(INamedTypeSymbol eventType, string name) =>
        eventType.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(property =>
                !property.IsStatic &&
                property.DeclaredAccessibility == Accessibility.Public &&
                string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase));

    static IEnumerable<ISymbol> GetPropertiesAndParameters(INamedTypeSymbol typeSymbol)
    {
        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>().Where(property => !property.IsStatic))
        {
            yield return property;
        }

        // For a positional record, attributes without an explicit target land on the constructor parameter
        // rather than the generated property, so the parameters must be inspected too.
        var primaryConstructor = typeSymbol.InstanceConstructors
            .OrderByDescending(constructor => constructor.Parameters.Length)
            .FirstOrDefault();

        if (primaryConstructor is not null)
        {
            foreach (var parameter in primaryConstructor.Parameters)
            {
                yield return parameter;
            }
        }
    }
}
