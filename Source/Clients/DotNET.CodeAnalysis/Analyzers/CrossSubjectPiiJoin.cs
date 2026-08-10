// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// The rule and shared reasoning behind <see cref="DiagnosticIds.CrossSubjectPiiJoin"/>, which is reported from
/// both the model-bound and the fluent join analyzer.
/// </summary>
static class CrossSubjectPiiJoin
{
    /// <summary>
    /// The conventional name of a read model's identifier, used as the last fallback when resolving its subject.
    /// </summary>
    internal const string IdentifierName = "Id";

    /// <summary>
    /// The shared descriptor for the diagnostic.
    /// </summary>
    internal static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.CrossSubjectPiiJoin,
        title: "[Join] of a [PII] value crosses the compliance subject",
        messageFormat: "The join with '{1}' on '{0}' copies the [PII] value '{1}.{2}' from the runtime subject reached through '{3}', which is not provably this read model's compliance subject. If those subjects differ, Chronicle cannot decrypt the joined value and the projection fails with an 'oaep decoding error' that freezes every partition. Resolve the value at the query edge under its owner's own subject instead.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A join reads an event through a stream selected by the join key, but the event's stored runtime subject can differ from that stream whenever an explicit subject was supplied at append. The absence of a [Subject] declaration does not prove equality, and a declaration does not prove it either: its value can be null or empty, an append can override it, and historical events retain the subject stored when they were appended. A read model stores one compliance subject and releases all of its PII under it, so a value belonging to a different subject cannot be decrypted. Beyond failing to read, materializing another subject's PII into this read model puts that personal data outside the reach of their erasure, which is a compliance defect in its own right. Join a non-PII value, or look the personal value up at the query edge under the subject that owns it.");

    /// <summary>
    /// Resolve the member that best describes a read model's apparent subject in a diagnostic.
    /// </summary>
    /// <param name="typeSymbol">The read model type.</param>
    /// <returns>The name of the subject member, or <see langword="null"/> when none can be resolved.</returns>
    /// <remarks>
    /// <para>
    /// At runtime the compliance subject is the resolved document key (see <c>ResolveComplianceIdentifier</c>),
    /// with <c>ReadModelSubjectResolver</c> preferring an explicit [Subject] and otherwise falling back to 'Id'.
    /// Neither is reproducible from syntax alone, so this is a deliberate approximation of both: an explicit
    /// [Subject], then an explicit [Key], then an <c>EventSourceId&lt;T&gt;</c>-derived value, then 'Id'.
    /// </para>
    /// <para>
    /// The value is diagnostic context only. It is not proof that a joined event has the same runtime subject:
    /// any append can supply an explicit subject, including when the event declares no [Subject] member.
    /// </para>
    /// </remarks>
    internal static string? GetSubjectMemberName(INamedTypeSymbol typeSymbol)
    {
        var members = GetMembers(typeSymbol).ToArray();

        return FirstNameWith(members, WellKnownTypes.SubjectAttributeName)
            ?? FirstNameWith(members, WellKnownTypes.KeyAttributeName)
            ?? members.FirstOrDefault(member => WellKnownTypes.DerivesFromEventSourceId(member.Type)).Name
            ?? members.FirstOrDefault(member => string.Equals(member.Name, IdentifierName, StringComparison.OrdinalIgnoreCase)).Name;
    }

    /// <summary>
    /// Determine whether the named property on an event carries personally identifiable information.
    /// </summary>
    /// <param name="eventType">The event type the join reads from.</param>
    /// <param name="propertyName">The name of the property on the event.</param>
    /// <returns>True when the value is PII, false otherwise.</returns>
    /// <remarks>
    /// Mirrors the compliance schema's reach: [PII] counts whether it sits on the declaring type, the property,
    /// the positional record's parameter, or anywhere inside the property's value-object, collection, array, or
    /// inherited member graph.
    /// </remarks>
    internal static bool IsPii(INamedTypeSymbol eventType, string propertyName)
    {
        if (HasPiiInTypeHierarchy(eventType))
        {
            return true;
        }

        return GetMembers(eventType).Any(member =>
            string.Equals(member.Name, propertyName, StringComparison.OrdinalIgnoreCase) &&
            (member.Attributes.Any(attribute => attribute.AttributeClass?.ToDisplayString() == WellKnownTypes.PiiAttributeName) ||
             ContainsPii(member.Type, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default))));
    }

    /// <summary>
    /// Find a <c>[PII]</c> value on an event that ends up on a read model.
    /// </summary>
    /// <param name="eventType">The event the value comes from.</param>
    /// <param name="readModelType">The read model the value lands on.</param>
    /// <param name="explicitMappings">The mappings written out by hand, target property first.</param>
    /// <param name="autoMapIsOn">Whether AutoMap can carry unmapped properties across.</param>
    /// <returns>The offending mapping, or <see langword="null"/> when no PII reaches the read model.</returns>
    /// <remarks>
    /// An event fills a read model both explicitly — <c>.Set(x =&gt; x.P).To(e =&gt; e.Q)</c> for the fluent
    /// builders, <c>[SetFrom&lt;TEvent&gt;]</c> for the model-bound ones — and implicitly through AutoMap,
    /// which matches identically named properties. The explicit route always applies; the implicit one only
    /// while AutoMap is on.
    /// </remarks>
    internal static (string TargetName, string EventPropertyName)? FindPiiReachingTheReadModel(
        INamedTypeSymbol eventType,
        INamedTypeSymbol readModelType,
        IEnumerable<(string TargetName, string EventPropertyName)> explicitMappings,
        bool autoMapIsOn)
    {
        var explicitMapping = explicitMappings.FirstOrDefault(mapping => IsPii(eventType, mapping.EventPropertyName));

        if (explicitMapping.EventPropertyName is not null)
        {
            return explicitMapping;
        }

        if (!autoMapIsOn)
        {
            return null;
        }

        // A positional record surfaces each member twice — once as the property, once as the constructor
        // parameter — and [NoAutoMap] written without a target lands only on the parameter. Excluding the one
        // entry that carries it would leave the other behind, so exclude the name outright.
        var excludedNames = GetMembers(readModelType)
            .Where(member => member.Attributes.Any(attribute => attribute.AttributeClass?.ToDisplayString() == WellKnownTypes.NoAutoMapAttributeName))
            .Select(member => member.Name)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        var mappableNames = GetMembers(readModelType)
            .Select(member => member.Name)
            .Where(name => !excludedNames.Contains(name))
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        var autoMapped = GetMembers(eventType)
            .FirstOrDefault(member => mappableNames.Contains(member.Name) && IsPii(eventType, member.Name))
            .Name;

        return autoMapped is null ? null : (autoMapped, autoMapped);
    }

    /// <summary>
    /// Enumerate the properties of a type and its base types together with the positional record parameters that back them.
    /// </summary>
    /// <param name="typeSymbol">The type to enumerate.</param>
    /// <returns>The name, type, and attributes of every member.</returns>
    /// <remarks>
    /// An attribute written without an explicit target on a positional record lands on the primary constructor's
    /// parameter rather than on the generated property, so both have to be inspected. Reflection-based compliance
    /// schema generation also sees inherited public properties, so the analyzer walks the same inheritance chain.
    /// </remarks>
    internal static IEnumerable<(string Name, ITypeSymbol? Type, ImmutableArray<AttributeData> Attributes)> GetMembers(INamedTypeSymbol typeSymbol)
    {
        for (var current = typeSymbol; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>().Where(property =>
                !property.IsStatic && property.DeclaredAccessibility == Accessibility.Public))
            {
                yield return (property.Name, property.Type, property.GetAttributes());
            }

            var primaryConstructor = current.InstanceConstructors
                .OrderByDescending(constructor => constructor.Parameters.Length)
                .FirstOrDefault();

            if (primaryConstructor is null)
            {
                continue;
            }

            foreach (var parameter in primaryConstructor.Parameters)
            {
                yield return (parameter.Name, parameter.Type, parameter.GetAttributes());
            }
        }
    }

    static bool ContainsPii(ITypeSymbol? type, HashSet<ITypeSymbol> path)
    {
        if (type is null)
        {
            return false;
        }

        if (type is IArrayTypeSymbol array)
        {
            return ContainsPii(array.ElementType, path);
        }

        if (type is not INamedTypeSymbol namedType || namedType.TypeKind == TypeKind.Enum)
        {
            return false;
        }

        if (HasPiiInTypeHierarchy(namedType))
        {
            return true;
        }

        // Primitive framework types are leaves. In particular, string implements IEnumerable<char> but is not
        // a collection-shaped compliance object.
        if (namedType.SpecialType != SpecialType.None)
        {
            return false;
        }

        var definition = namedType.OriginalDefinition;
        if (!path.Add(definition))
        {
            // A recursive generic can change construction forever (Recursive<T> -> Recursive<List<T>>).
            // Type arguments alone are not serialized values, so stop this repeated definition. The first
            // occurrence continues across its remaining public members and will still find T when a real
            // member such as Value exposes it.
            return false;
        }

        try
        {
            var enumerable = namedType.AllInterfaces
                .Concat([namedType])
                .FirstOrDefault(candidate =>
                    candidate.IsGenericType &&
                    candidate.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);

            if (enumerable is not null)
            {
                return ContainsPii(enumerable.TypeArguments[0], path);
            }

            return GetMembers(namedType).Any(member =>
                member.Attributes.Any(attribute => attribute.AttributeClass?.ToDisplayString() == WellKnownTypes.PiiAttributeName) ||
                ContainsPii(member.Type, path));
        }
        finally
        {
            path.Remove(definition);
        }
    }

    static bool HasPiiInTypeHierarchy(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (WellKnownTypes.HasAttribute(current, WellKnownTypes.PiiAttributeName))
            {
                return true;
            }
        }

        return false;
    }

    static string? FirstNameWith((string Name, ITypeSymbol? Type, ImmutableArray<AttributeData> Attributes)[] members, string attributeFullName) =>
        members.FirstOrDefault(member => member.Attributes.Any(attribute => attribute.AttributeClass?.ToDisplayString() == attributeFullName)).Name;
}
