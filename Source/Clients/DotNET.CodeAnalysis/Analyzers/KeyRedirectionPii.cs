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
    /// at — the value re-encrypts and reads back cleanly — and the analyzer cannot prove the runtime subject
    /// stored with every historical and future event. Change <c>defaultSeverity</c> below to
    /// <see cref="DiagnosticSeverity.Error"/> only through a separately reviewed rollout decision.
    /// </remarks>
    internal static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.KeyRedirectionPii,
        title: "Key redirection carries a [PII] value across the compliance subject",
        messageFormat: "'{0}' takes the [PII] value '{1}.{2}', but this projection keys the document by '{3}' instead of by the stream '{1}' is appended to. The value's stored runtime subject is not provably the document's subject '{4}'; a [Subject] declaration cannot prove equality because append metadata and historical events decide the EventContext. If those subjects differ, Chronicle re-encrypts the value under the document's subject, so crypto-shredding the owner never reaches this copy. Keep the [PII] value on an owner-scoped read model, or resolve it at the query edge under its owner.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A read model document carries exactly one compliance subject. Redirecting the key can move the document away from the runtime subject stored with an event, so a [PII] value the event carries is re-encrypted under an identity that is not its owner's and falls outside the reach of their erasure - the same defect CHR0038 refuses for a cross-subject [Join], reached through a different construct. Declaring [Subject] on the event is not sufficient static proof: its value can be null or empty, an append can override it, and historical events retain the subject stored when they were appended. Nothing fails at read time, which is what makes the defect invisible without a rule. Keep the personal value on a read model scoped to its owner, or resolve it at the query edge under that owner.");

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
