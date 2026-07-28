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
        messageFormat: "The join with '{1}' on '{0}' copies the [PII] value '{1}.{2}' out of the stream identified by '{3}', which is not this read model's compliance subject. Chronicle releases a read model's PII under its own subject, so the joined value cannot be decrypted and the projection fails with an 'oaep decoding error' that freezes every partition. Resolve the value at the query edge under its owner's own subject instead.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A join reads the joined event from the stream whose event source id is the value of the join key, so a [PII] value it copies was encrypted under that stream's subject. A read model stores one compliance subject and releases all of its PII under it, so a value belonging to a different subject cannot be decrypted. Beyond failing to read, materializing another subject's PII into this read model puts that personal data outside the reach of their erasure, which is a compliance defect in its own right. Join a non-PII value, or look the personal value up at the query edge under the subject that owns it.");

    /// <summary>
    /// Resolve the member a read model is keyed — and therefore subjected — by.
    /// </summary>
    /// <param name="typeSymbol">The read model type.</param>
    /// <returns>The name of the subject member, or <see langword="null"/> when none can be resolved.</returns>
    /// <remarks>
    /// Mirrors Chronicle's own resolution order: an explicit [Subject], then an explicit [Key], then an
    /// <c>EventSourceId&lt;T&gt;</c>-derived value (which is both), and finally the conventional 'Id'.
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
    /// Mirrors <c>PIIMetadataProvider</c>: [PII] counts whether it sits on the declaring type, the property, the
    /// positional record's parameter, or the property's own <c>ConceptAs&lt;T&gt;</c> type.
    /// </remarks>
    internal static bool IsPii(INamedTypeSymbol eventType, string propertyName)
    {
        if (HasPii(eventType))
        {
            return true;
        }

        return GetMembers(eventType).Any(member =>
            string.Equals(member.Name, propertyName, StringComparison.OrdinalIgnoreCase) &&
            (member.Attributes.Any(attribute => attribute.AttributeClass?.ToDisplayString() == WellKnownTypes.PiiAttributeName) ||
             (member.Type is not null && HasPii(member.Type))));
    }

    /// <summary>
    /// Enumerate the properties of a type together with the positional record parameters that back them.
    /// </summary>
    /// <param name="typeSymbol">The type to enumerate.</param>
    /// <returns>The name, type, and attributes of every member.</returns>
    /// <remarks>
    /// An attribute written without an explicit target on a positional record lands on the primary constructor's
    /// parameter rather than on the generated property, so both have to be inspected.
    /// </remarks>
    internal static IEnumerable<(string Name, ITypeSymbol? Type, ImmutableArray<AttributeData> Attributes)> GetMembers(INamedTypeSymbol typeSymbol)
    {
        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>().Where(property => !property.IsStatic))
        {
            yield return (property.Name, property.Type, property.GetAttributes());
        }

        var primaryConstructor = typeSymbol.InstanceConstructors
            .OrderByDescending(constructor => constructor.Parameters.Length)
            .FirstOrDefault();

        if (primaryConstructor is null)
        {
            yield break;
        }

        foreach (var parameter in primaryConstructor.Parameters)
        {
            yield return (parameter.Name, parameter.Type, parameter.GetAttributes());
        }
    }

    static bool HasPii(ITypeSymbol type) => WellKnownTypes.HasAttribute(type, WellKnownTypes.PiiAttributeName);

    static string? FirstNameWith((string Name, ITypeSymbol? Type, ImmutableArray<AttributeData> Attributes)[] members, string attributeFullName) =>
        members.FirstOrDefault(member => member.Attributes.Any(attribute => attribute.AttributeClass?.ToDisplayString() == attributeFullName)).Name;
}
