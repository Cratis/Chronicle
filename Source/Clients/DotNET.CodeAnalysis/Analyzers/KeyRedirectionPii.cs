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
    /// The description used when a constant-key call cannot be evaluated by the compiler.
    /// </summary>
    internal const string ConstantKeyDescription = "a constant key";

    /// <summary>
    /// The shared descriptor for the diagnostic.
    /// </summary>
    /// <remarks>
    /// The severity is deliberately one line, and deliberately a warning for its first release: unlike
    /// Unlike a joined value with tracked per-property ownership, this shape need not produce a runtime symptom — the value
    /// can re-encrypt and read back cleanly while erasure misses it — and the analyzer cannot prove the runtime
    /// subject stored with every historical and future event. Change <c>defaultSeverity</c> below to
    /// <see cref="DiagnosticSeverity.Error"/> only through a separately reviewed rollout decision.
    /// </remarks>
    internal static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.KeyRedirectionPii,
        title: "Key redirection carries a [PII] value across the compliance subject",
        messageFormat: "'{0}' takes the [PII] value '{1}.{2}', but this projection routes the resolved document through '{3}'. The kernel derives the stored document compliance subject from an explicit persisted event subject or, otherwise, from that resolved key; client release later resolves it through '{4}'. Source declarations cannot prove those identities equal every historical and future value owner. Keep the [PII] value on an owner-scoped read model, or resolve it at the query edge under its owner.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A read model document carries exactly one stored compliance subject. The kernel resolves it from an explicit persisted event subject when one differs from the event source id, otherwise from the resolved document key. Redirecting that key can therefore move PII under an identity that is not its owner's; client release independently resolves [Subject] and then Id from the materialized model. Declaring [Subject] on an event type is not proof of any persisted value because append metadata and historical events decide EventContext. Nothing has to fail at read time, which is what makes the erasure defect invisible without a warning.");

    /// <summary>
    /// The simple name of the event context type, used when naming a context-sourced key in the diagnostic.
    /// </summary>
    const string EventContextDisplayName = "EventContext";

    /// <summary>
    /// The <c>EventContext</c> members that keep document routing aligned with the kernel's compliance-identifier
    /// resolution.
    /// </summary>
    /// <remarks>
    /// An explicit <c>EventContext.Subject</c> wins in the kernel regardless of the resolved key. Otherwise the
    /// subject defaults to the event source id, and routing by that id leaves the default document key unchanged.
    /// </remarks>
    static readonly string[] _contextMembersAlignedWithComplianceResolution = ["EventSourceId", "Subject"];

    /// <summary>
    /// Resolve the materialized member client-side compliance release will use, for the diagnostic message.
    /// </summary>
    /// <param name="readModelType">The read model type.</param>
    /// <returns>The [Subject] member, the conventional Id fallback, or an unresolved description.</returns>
    internal static string ClientReleaseSubjectDescriptionOf(INamedTypeSymbol readModelType)
    {
        var members = CrossSubjectPiiJoin.GetMembers(readModelType).ToArray();
        return members.FirstOrDefault(member => member.Attributes.Any(attribute =>
                   attribute.AttributeClass?.ToDisplayString() == WellKnownTypes.SubjectAttributeName)).Name
               ?? members.FirstOrDefault(member =>
                   string.Equals(member.Name, CrossSubjectPiiJoin.IdentifierName, StringComparison.OrdinalIgnoreCase)).Name
               ?? "no [Subject] or Id member";
    }

    /// <summary>
    /// Determine whether keying a document by an <c>EventContext</c> member can redirect it away from the kernel's
    /// resolved compliance identity.
    /// </summary>
    /// <param name="contextMemberName">The name of the member on <c>EventContext</c>.</param>
    /// <returns>True when the member can redirect compliance identity, false otherwise.</returns>
    internal static bool ContextMemberRedirects(string contextMemberName) =>
        !_contextMembersAlignedWithComplianceResolution.Contains(contextMemberName, StringComparer.OrdinalIgnoreCase);

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
