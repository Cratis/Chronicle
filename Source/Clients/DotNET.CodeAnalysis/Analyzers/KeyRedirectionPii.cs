// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// The rule and shared reasoning behind <see cref="DiagnosticIds.KeyRedirectionPii"/>, which is reported from
/// both the model-bound and the fluent key-redirection analyzer.
/// </summary>
static class KeyRedirectionPii
{
    /// <summary>
    /// The description used for a composite key, whose canonical identifier is never a single subject.
    /// </summary>
    internal const string CompositeKeyDescription = "a composite key";

    /// <summary>
    /// The shared descriptor for the diagnostic.
    /// </summary>
    /// <remarks>
    /// The severity is deliberately one line, and deliberately a warning for its first release: unlike
    /// <see cref="DiagnosticIds.CrossSubjectPiiJoin"/> this shape has no runtime symptom to point a consumer
    /// at — the value re-encrypts and reads back cleanly — and the analyzer cannot see an explicit
    /// <c>subject:</c> passed at the append site, which is a route to a correct program that still trips the
    /// rule. Change <c>defaultSeverity</c> below to <see cref="DiagnosticSeverity.Error"/> to promote it once
    /// consumers have had a release to react.
    /// </remarks>
    internal static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.KeyRedirectionPii,
        title: "Key redirection carries a [PII] value across the compliance subject",
        messageFormat: "'{0}' takes the [PII] value '{1}.{2}', but this projection keys the document by '{3}' instead of by the stream '{1}' is appended to. '{1}' declares no [Subject], so the value belongs to that stream's subject, while the document — and its own subject '{4}' — is stamped with '{3}'. Chronicle re-encrypts the value under the document's subject, so crypto-shredding the owner's key never reaches this copy. Mark the owning identity on '{1}' with [Subject], or keep the [PII] value off a read model keyed by something else.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A read model document carries exactly one compliance subject, and the kernel resolves it from the document key unless the event supplies an explicit [Subject]. Redirecting the key moves the document away from the stream the event was appended to, so a [PII] value the event carries is re-encrypted under an identity that is not its owner's and falls outside the reach of their erasure - the same defect CHR0038 refuses for a cross-subject [Join], reached through a different construct. Nothing fails at read time, which is what makes it invisible without a rule. Mark the owning identity on the event with [Subject] so Chronicle stamps the document with it, or keep the personal value off a read model keyed by something else.");

    /// <summary>
    /// The simple name of the event context type, used when naming a context-sourced key in the diagnostic.
    /// </summary>
    const string EventContextDisplayName = "EventContext";

    /// <summary>
    /// The <c>EventContext</c> members whose value is the event's own compliance subject, so keying a document
    /// by one of them is not a redirection at all.
    /// </summary>
    /// <remarks>
    /// <c>EventContext.Subject</c> is the compliance subject itself, and it defaults to the event source id,
    /// which is what a projection keys by when nothing redirects it.
    /// </remarks>
    static readonly string[] _contextMembersThatAreTheEventsOwnSubject = ["EventSourceId", "Subject"];

    /// <summary>
    /// Determine whether an event names its own compliance subject.
    /// </summary>
    /// <param name="eventType">The event type the projection reads.</param>
    /// <returns>True when the event carries a <c>[Subject]</c> member, false otherwise.</returns>
    /// <remarks>
    /// <c>ResolveComplianceIdentifier</c> honors an explicit subject over the resolved document key, so an
    /// event that names its own subject stamps the document with it however the projection is keyed and its
    /// personal data stays under its owner. Skipping those is a deliberate false negative — a later event
    /// without a subject can still re-stamp the same document — chosen because the alternative is a false
    /// positive over the very shape this rule tells consumers to write.
    /// </remarks>
    internal static bool CarriesItsOwnSubject(INamedTypeSymbol eventType) =>
        CrossSubjectPiiJoin.GetMembers(eventType).Any(member =>
            member.Attributes.Any(attribute => attribute.AttributeClass?.ToDisplayString() == WellKnownTypes.SubjectAttributeName));

    /// <summary>
    /// Resolve the member a read model is subjected by, for the diagnostic message.
    /// </summary>
    /// <param name="readModelType">The read model type.</param>
    /// <returns>The name of the subject member, falling back to the conventional identifier.</returns>
    internal static string SubjectMemberNameOf(INamedTypeSymbol readModelType) =>
        CrossSubjectPiiJoin.GetSubjectMemberName(readModelType) ?? CrossSubjectPiiJoin.IdentifierName;

    /// <summary>
    /// Determine whether keying a document by an <c>EventContext</c> member redirects it away from the event's subject.
    /// </summary>
    /// <param name="contextMemberName">The name of the member on <c>EventContext</c>.</param>
    /// <returns>True when the member is something other than the event's own subject, false otherwise.</returns>
    internal static bool ContextMemberRedirects(string contextMemberName) =>
        !_contextMembersThatAreTheEventsOwnSubject.Contains(contextMemberName, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Describe a constant key for the diagnostic message.
    /// </summary>
    /// <param name="value">The constant the key is pinned to.</param>
    /// <returns>The description to name in the diagnostic.</returns>
    internal static string DescribeConstantKey(string value) => $"the constant '{value}'";

    /// <summary>
    /// Describe an <c>EventContext</c>-sourced key for the diagnostic message.
    /// </summary>
    /// <param name="contextMemberName">The name of the member on <c>EventContext</c>.</param>
    /// <returns>The description to name in the diagnostic.</returns>
    internal static string DescribeContextKey(string contextMemberName) => $"{EventContextDisplayName}.{contextMemberName}";
}
