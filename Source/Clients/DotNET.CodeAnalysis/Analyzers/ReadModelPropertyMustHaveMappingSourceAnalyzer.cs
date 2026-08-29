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
/// Analyzer that warns when a model-bound projection read model declares a property that has no property-level
/// mapping attribute and matches no subscribed event's property name, meaning AutoMap can never populate it.
/// </summary>
/// <remarks>
/// No code fix is offered: the property is flagged precisely because no subscribed event carries a same-named
/// property, so there is no valid event type to synthesize a <c>[SetFrom&lt;T&gt;]</c> for. The fix requires a
/// human decision — either subscribe to an event that carries the value, or add the appropriate mapping attribute.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReadModelPropertyMustHaveMappingSourceAnalyzer : DiagnosticAnalyzer
{
    const string ModelBoundNamespace = "Cratis.Chronicle.Projections.ModelBound";
    const string IdentifierName = "Id";
    const string SetValueAttributeName = "SetValueAttribute";

    static readonly HashSet<string> ModelBoundMappingAttributeNames = new(StringComparer.Ordinal)
    {
        "FromEventAttribute",
        "SetFromAttribute",
        "SetFromContextAttribute",
        SetValueAttributeName,
        "ChildrenFromAttribute",
        "JoinAttribute",
        "AddFromAttribute",
        "SubtractFromAttribute",
        "CountAttribute",
        "IncrementAttribute",
        "DecrementAttribute",
        "RemovedWithAttribute",
        "RemovedWithJoinAttribute",
        "FromEveryAttribute"
    };

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ReadModelPropertyMustHaveMappingSource,
        title: "Read model property has no mapping source",
        messageFormat: "Read model property '{0}' has no mapping source: no mapping attribute, and no subscribed event carries a same-named property for AutoMap to bind. It will never be populated. Add an explicit mapping (e.g. [SetFrom<T>(nameof(...))]).",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "In a model-bound projection read model, AutoMap populates a property only when a subscribed event carries a property of the same name. A property with no property-level mapping attribute and no same-named property on any subscribed event will silently never be populated. Add an explicit mapping attribute, or subscribe to an event that carries the value.");

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

        var subscribedEvents = GetSubscribedEventTypes(typeSymbol);

        // Only a model-bound projection is analyzed: it must carry at least one model-bound mapping attribute
        // (class-level [FromEvent<T>] or a member-level mapping attribute). Be conservative — never fire on
        // arbitrary records.
        if (!IsModelBoundProjection(typeSymbol))
        {
            return;
        }

        foreach (var (member, memberType, attributes) in GetMembers(typeSymbol))
        {
            AnalyzeMember(context, member, memberType, attributes, subscribedEvents);
        }
    }

    static void AnalyzeMember(
        SymbolAnalysisContext context,
        ISymbol member,
        ITypeSymbol memberType,
        IReadOnlyList<AttributeData> attributes,
        IReadOnlyCollection<INamedTypeSymbol> subscribedEvents)
    {
        // Skip members that are explicitly sourced by a mapping attribute or marked as the key.
        if (attributes.Any(IsMappingOrKeyAttribute))
        {
            return;
        }

        // Skip the identifier — it is resolved from the key/stream identity, not AutoMap.
        if (string.Equals(member.Name, IdentifierName, StringComparison.OrdinalIgnoreCase) ||
            WellKnownTypes.DerivesFromEventSourceId(memberType))
        {
            return;
        }

        // Skip when a subscribed event carries a same-named property — AutoMap would bind it.
        if (subscribedEvents.Any(eventType => HasPublicInstanceProperty(eventType, member.Name)))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            member.Locations.FirstOrDefault(),
            member.Name));
    }

    static bool IsModelBoundProjection(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.GetAttributes().Any(IsModelBoundMappingAttribute))
        {
            return true;
        }

        foreach (var (_, _, attributes) in GetMembers(typeSymbol))
        {
            if (attributes.Any(IsModelBoundMappingAttribute))
            {
                return true;
            }
        }

        return false;
    }

    static List<INamedTypeSymbol> GetSubscribedEventTypes(INamedTypeSymbol typeSymbol)
    {
        var events = new List<INamedTypeSymbol>();

        void Collect(IEnumerable<AttributeData> attributes)
        {
            foreach (var attribute in attributes.Where(IsModelBoundMappingAttribute))
            {
                if (attribute.AttributeClass is { TypeArguments.Length: > 0 } attributeClass &&
                    attributeClass.TypeArguments[0] is INamedTypeSymbol eventType)
                {
                    events.Add(eventType);
                }
            }
        }

        Collect(typeSymbol.GetAttributes());

        foreach (var (_, _, attributes) in GetMembers(typeSymbol))
        {
            Collect(attributes);
        }

        return events;
    }

    static IEnumerable<(ISymbol Member, ITypeSymbol Type, IReadOnlyList<AttributeData> Attributes)> GetMembers(INamedTypeSymbol typeSymbol)
    {
        // Restrict to public instance properties: this excludes the compiler-generated protected
        // EqualityContract on records and any other non-public synthesized members.
        var properties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(property => !property.IsStatic && !property.IsIndexer && property.DeclaredAccessibility == Accessibility.Public)
            .ToArray();

        var propertiesByName = properties.ToDictionary(property => property.Name, StringComparer.Ordinal);
        var handledByParameter = new HashSet<string>(StringComparer.Ordinal);

        // For a positional record, a member-level mapping/key attribute (and the member location) lands on the
        // constructor parameter rather than the generated property, so the primary-constructor parameters that
        // back generated properties are inspected in their place.
        var primaryConstructor = typeSymbol.InstanceConstructors
            .OrderByDescending(constructor => constructor.Parameters.Length)
            .FirstOrDefault();

        if (typeSymbol.IsRecord && primaryConstructor is not null)
        {
            // A positional parameter generates a matching property; filtering on the property names excludes the
            // compiler-generated copy constructor's parameter, which has no corresponding property.
            foreach (var parameter in primaryConstructor.Parameters.Where(parameter => propertiesByName.ContainsKey(parameter.Name)))
            {
                handledByParameter.Add(parameter.Name);

                // Where an attribute lands depends on how it was written: a bare [SetFrom<T>] on a positional
                // parameter binds to the parameter, whereas [property: SetFrom<T>] binds to the generated
                // property. Both are valid ways to declare the same mapping and both are honored at runtime, so
                // the two have to be considered together - looking at only one of them reports a mapping that is
                // plainly there as missing.
                yield return (
                    parameter,
                    parameter.Type,
                    [.. parameter.GetAttributes(), .. propertiesByName[parameter.Name].GetAttributes()]);
            }
        }

        foreach (var property in properties.Where(property => !handledByParameter.Contains(property.Name)))
        {
            yield return (property, property.Type, property.GetAttributes());
        }
    }

    static bool HasPublicInstanceProperty(INamedTypeSymbol eventType, string name) =>
        eventType.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(property =>
                !property.IsStatic &&
                property.DeclaredAccessibility == Accessibility.Public &&
                string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase));

    static bool IsModelBoundMappingAttribute(AttributeData attribute) =>
        attribute.AttributeClass is { } attributeClass &&
        ModelBoundMappingAttributeNames.Contains(attributeClass.Name) &&
        attributeClass.ContainingNamespace?.ToDisplayString() == ModelBoundNamespace;

    static bool IsMappingOrKeyAttribute(AttributeData attribute) =>
        (IsModelBoundMappingAttribute(attribute) && !DeclaresAClear(attribute)) ||
        attribute.AttributeClass?.ToDisplayString() == WellKnownTypes.KeyAttributeName;

    /// <summary>
    /// Determines whether an attribute declares a clear rather than a mapping source.
    /// </summary>
    /// <param name="attribute">The attribute to inspect.</param>
    /// <returns>True when the attribute clears the member rather than sourcing it.</returns>
    /// <remarks>
    /// A null <c>[SetValue]</c> is a clear: it says what returns the member to no value, never where a value comes
    /// from. Counting it as a mapping source would silence this rule for a member that genuinely has none - the
    /// member would be cleared by one event and populated by nothing, which is precisely what the rule reports.
    /// <c>[ClearWith]</c> is not listed as a mapping attribute at all, so it needs no exclusion here.
    /// </remarks>
    static bool DeclaresAClear(AttributeData attribute) =>
        attribute.AttributeClass?.Name == SetValueAttributeName &&
        attribute.ConstructorArguments.Length > 0 &&
        attribute.ConstructorArguments[0] is { Kind: not TypedConstantKind.Error, Value: null };
}
