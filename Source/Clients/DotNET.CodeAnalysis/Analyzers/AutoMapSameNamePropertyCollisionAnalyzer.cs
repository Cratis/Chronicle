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
/// Analyzer that warns when a model-bound read model property is explicitly sourced via <c>[SetFrom]</c> or
/// <c>[Join]</c>, but another subscribed event carries an identically named property that AutoMap will silently
/// overwrite the explicit value with — unless the property is marked with property-level <c>[NoAutoMap]</c>.
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
        "JoinAttribute"
    };

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.AutoMapSameNamePropertyCollision,
        title: "Explicitly sourced read model property is overwritten by AutoMap",
        messageFormat: "Read model property '{0}' is explicitly sourced but event '{1}' carries an identically named property; AutoMap from '{1}' will overwrite it. Add [NoAutoMap] to the property.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When a read model property is explicitly sourced via [SetFrom<A>] or [Join<A>], but another subscribed event B carries a property with the same name, AutoMap from B silently overwrites the explicit value. Add property-level [NoAutoMap] to the property so only the explicit mapping applies.");

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

    static INamedTypeSymbol? GetExplicitSourceEvent(ISymbol member)
    {
        foreach (var attribute in member.GetAttributes())
        {
            if (attribute.AttributeClass is { } attributeClass &&
                attributeClass.ContainingNamespace?.ToDisplayString() == ModelBoundNamespace &&
                ExplicitSourceAttributeNames.Contains(attributeClass.Name) &&
                attributeClass.TypeArguments.Length == 1 &&
                attributeClass.TypeArguments[0] is INamedTypeSymbol eventType)
            {
                return eventType;
            }
        }

        return null;
    }

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
        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (!property.IsStatic)
            {
                yield return property;
            }
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
