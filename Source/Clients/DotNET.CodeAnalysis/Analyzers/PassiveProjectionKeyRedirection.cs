// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// The rule and shared reasoning behind <see cref="DiagnosticIds.PassiveProjectionKeyRedirection"/>, which is
/// reported from both the model-bound and the fluent passive key-redirection analyzer.
/// </summary>
static class PassiveProjectionKeyRedirection
{
    /// <summary>
    /// The description used for a composite key, which never names a single event source.
    /// </summary>
    internal const string CompositeKeyDescription = "a composite key";

    /// <summary>
    /// The description used when a constant-key call cannot be evaluated by the compiler.
    /// </summary>
    internal const string ConstantKeyDescription = "a constant key";

    /// <summary>
    /// The way a model-bound re-key is named in the diagnostic.
    /// </summary>
    internal const string ModelBoundRedirectionDisplayName = "[FromEvent(key:)]";

    /// <summary>
    /// The shared descriptor for the diagnostic.
    /// </summary>
    /// <remarks>
    /// The severity is deliberately a warning for its first release, on the same reasoning recorded for
    /// <see cref="DiagnosticIds.KeyRedirectionPii"/> and <see cref="DiagnosticIds.UnprovableCrossSubjectPiiJoin"/>:
    /// the shape need not fail, a consumer may knowingly accept a read that only ever answers for the redirected
    /// stream, and a new error would break existing builds. Change <c>defaultSeverity</c> below to
    /// <see cref="DiagnosticSeverity.Error"/> only through a separately reviewed rollout decision.
    /// </remarks>
    internal static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.PassiveProjectionKeyRedirection,
        title: "A passive read model redirects the document key away from the event's own stream",
        messageFormat: "'{0}' keys the '{1}' document by '{2}', but '{1}' is passive. A passive read model has no sink, so a read is answered by replaying with the cursor constrained to eventSourceId equal to the requested key - exactly one physical event stream. '{3}' appended to any other event source is never reached and the read returns a default-initialized '{1}'. Registration succeeds and a ReadModelScenario still passes. Remove the passive declaration so an observer materializes the redirected document, or key '{1}' by the event source '{3}' is appended to.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A passive read model has no materialized sink, so nothing writes the redirected document ahead of a read. The kernel answers a passive read by replaying the event sequence with the cursor constrained to the requested key as the event source id, which walks one physical stream. A top-level key redirection is routing metadata for a materialized write, not a lookup index over the event log, so it cannot make a replay find events that live on another stream. The read therefore returns a default-initialized model rather than failing, which is what makes the defect invisible without a warning.");

    /// <summary>
    /// Describe a constant key for the diagnostic message.
    /// </summary>
    /// <param name="value">The constant the key is pinned to.</param>
    /// <returns>The description to name in the diagnostic.</returns>
    internal static string DescribeConstantKey(string value) => $"the constant '{value}'";

    /// <summary>
    /// Describe the root key a fluent redirection call points the document at.
    /// </summary>
    /// <param name="semanticModel">The semantic model for the invocation.</param>
    /// <param name="invocation">The redirection invocation.</param>
    /// <returns>The method name and key description, or <see langword="null"/> when nothing is redirected.</returns>
    /// <remarks>
    /// <c>UsingKeyFromContext</c> goes through <see cref="KeyRedirectionPii.ContextMemberRedirects"/>, which
    /// treats both <c>EventSourceId</c> and <c>Subject</c> as non-redirecting. <c>EventSourceId</c> is the
    /// identity and must never be reported; <c>Subject</c> is conservatively excluded too, so this rule
    /// under-reports rather than guessing at a shape the PII rule already reasons about.
    /// </remarks>
    internal static (string MethodName, string Key)? DescribeRootKey(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member)
        {
            return null;
        }

        var methodName = member.Name.Identifier.Text;
        var argument = invocation.ArgumentList.Arguments.Count == 1
            ? invocation.ArgumentList.Arguments[0].Expression
            : null;

        return methodName switch
        {
            KeyRedirection.UsingKey =>
                argument is not null && FluentProjectionMappings.TryGetPropertyPath(semanticModel, argument, out var eventProperty)
                    ? (methodName, eventProperty)
                    : null,

            KeyRedirection.UsingKeyFromContext =>
                argument is not null &&
                FluentProjectionMappings.TryGetPropertyPath(semanticModel, argument, out var contextProperty) &&
                KeyRedirectionPii.ContextMemberRedirects(contextProperty)
                    ? (methodName, KeyRedirectionPii.DescribeContextKey(contextProperty))
                    : null,

            KeyRedirection.UsingConstantKey =>
                argument is not null && semanticModel.GetConstantValue(argument).Value is string constant
                    ? (methodName, DescribeConstantKey(constant))
                    : (methodName, ConstantKeyDescription),

            KeyRedirection.UsingCompositeKey => (methodName, CompositeKeyDescription),

            _ => null
        };
    }
}
