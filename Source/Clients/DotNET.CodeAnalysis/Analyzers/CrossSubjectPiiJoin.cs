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
        messageFormat: "The join with '{1}' on '{0}' copies the [PII] value '{1}.{2}' through '{3}', which differs from this read model's apparent compliance subject. Chronicle can then release the joined value under the wrong key and freeze the projection with an 'oaep decoding error'. Resolve the value at the query edge under its owner's own subject instead.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A join that explicitly selects a key different from the read model's apparent compliance subject can copy PII between subject boundaries. A read model stores one compliance subject and releases all of its PII under it, so the value may not decrypt. This established definite-boundary diagnostic remains an error. Cases where the source shape appears to use the same subject but append metadata prevents proof are reported separately by CHR0044 as a warning.");

    /// <summary>
    /// The warning reported when the source shape appears to use the same subject but persisted runtime subject
    /// metadata prevents a source-level proof.
    /// </summary>
    internal static readonly DiagnosticDescriptor UnprovableRule = new(
        id: DiagnosticIds.UnprovableCrossSubjectPiiJoin,
        title: "[Join] of a [PII] value cannot prove compliance subject equality",
        messageFormat: "The join with '{1}' on '{0}' copies the [PII] value '{1}.{2}' through the apparent subject '{3}', but append metadata and historical events can assign another persisted runtime subject. If the runtime subjects differ, Chronicle can release or erase the copy under the wrong key. Keep the value owner-scoped or resolve it at the query edge.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An omitted join key or a key that names the read model's apparent subject does not prove that every joined event was persisted under that subject. Explicit append metadata can override an event declaration, and historical events retain their stored subject. This conservative expansion is a warning so adding it does not turn previously accepted source into a transitive build break; CHR0038 remains the error for an explicitly different join boundary.");

    static readonly char[] _propertyPathSeparators = ['.'];

    /// <summary>
    /// Resolve the member that best describes a read model's apparent subject boundary in a diagnostic.
    /// </summary>
    /// <param name="typeSymbol">The read model type.</param>
    /// <returns>The name of the subject member, or <see langword="null"/> when none can be resolved.</returns>
    /// <remarks>
    /// <para>
    /// The kernel stores an explicit persisted event subject when it differs from the event source id and otherwise
    /// uses the resolved document key. Client release resolves [Subject] and then 'Id' from the materialized model.
    /// Neither runtime value is reproducible from a type declaration alone. This ordering deliberately preserves
    /// CHR0038's released source boundary: [Subject], [Key], an <c>EventSourceId&lt;T&gt;</c>-derived value, then 'Id'.
    /// </para>
    /// <para>
    /// The result is an apparent boundary and diagnostic context only. It is not the stored document compliance
    /// subject and is not proof that a joined event has the same persisted runtime subject.
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

        var currentType = eventType;
        var path = propertyName.Split(_propertyPathSeparators, StringSplitOptions.RemoveEmptyEntries);

        for (var index = 0; index < path.Length; index++)
        {
            var members = GetMembers(currentType)
                .Where(candidate => string.Equals(candidate.Name, path[index], StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (members.Length == 0)
            {
                return false;
            }

            if (members.Any(member => member.Attributes.Any(attribute =>
                    attribute.AttributeClass?.ToDisplayString() == WellKnownTypes.PiiAttributeName)))
            {
                return true;
            }

            var memberType = members.Select(member => member.Type).FirstOrDefault(type => type is not null);

            if (index == path.Length - 1)
            {
                return ContainsPii(memberType, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));
            }

            if (memberType is not INamedTypeSymbol nestedType)
            {
                return false;
            }

            currentType = nestedType;
        }

        return false;
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
        var mappings = explicitMappings.ToArray();
        var explicitMapping = mappings.FirstOrDefault(mapping => IsPii(eventType, mapping.EventPropertyName));

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
            .Concat(mappings.Select(mapping => GetLastPathSegment(mapping.TargetName)))
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

    static string GetLastPathSegment(string path)
    {
        var segments = path.Split(_propertyPathSeparators, StringSplitOptions.RemoveEmptyEntries);
        return segments[segments.Length - 1];
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
