// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports a fluent <c>builder.Join&lt;TEvent&gt;(_ =&gt; _.On(...))</c> copying a <c>[PII]</c> value
/// out of a stream keyed by something other than the read model's own compliance subject.
/// </summary>
/// <remarks>
/// The model-bound equivalent is covered by <see cref="CrossSubjectPiiJoinAnalyzer"/>; both report
/// <see cref="DiagnosticIds.CrossSubjectPiiJoin"/>.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FluentCrossSubjectPiiJoinAnalyzer : DiagnosticAnalyzer
{
    const string JoinMethodName = "Join";
    const string OnMethodName = "On";
    const string SetMethodName = "Set";
    const string ToMethodName = "To";
    const string ProjectionBuilderInterfaceName = "IProjectionBuilder";
    const string JoinBuilderInterfaceName = "IJoinBuilder";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(CrossSubjectPiiJoin.Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax member ||
            member.Name.Identifier.Text != JoinMethodName ||
            invocation.ArgumentList.Arguments.Count != 1)
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method ||
            method.ContainingType?.OriginalDefinition.Name != ProjectionBuilderInterfaceName ||
            method.TypeArguments.FirstOrDefault() is not INamedTypeSymbol eventType ||
            method.ContainingType.TypeArguments.FirstOrDefault() is not INamedTypeSymbol readModelType)
        {
            return;
        }

        var builderCallback = invocation.ArgumentList.Arguments[0].Expression;

        // A child join has no On() — it keys on the child's identifiedBy, which the parent already scopes,
        // so there is no separate stream to cross into.
        if (!TryGetOnProperty(context, builderCallback, out var on) ||
            string.Equals(on, CrossSubjectPiiJoin.GetSubjectMemberName(readModelType), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (FindPiiSource(context, builderCallback, eventType, readModelType) is not { } source)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            CrossSubjectPiiJoin.Rule,
            member.Name.GetLocation(),
            source.TargetName,
            eventType.Name,
            source.EventPropertyName,
            on));
    }

    /// <summary>
    /// Find a <c>[PII]</c> value on the joined event that ends up on the read model.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    /// <param name="builderCallback">The join builder callback.</param>
    /// <param name="eventType">The joined event type.</param>
    /// <param name="readModelType">The read model being projected.</param>
    /// <returns>The offending mapping, or <see langword="null"/> when no PII reaches the read model.</returns>
    /// <remarks>
    /// A join fills the read model both explicitly, through <c>.Set(x =&gt; x.P).To(e =&gt; e.Q)</c>, and implicitly
    /// through AutoMap, which matches identically named properties. Both routes are inspected.
    /// </remarks>
    static (string TargetName, string EventPropertyName)? FindPiiSource(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax builderCallback,
        INamedTypeSymbol eventType,
        INamedTypeSymbol readModelType)
    {
        foreach (var (targetName, eventPropertyName) in GetExplicitMappings(builderCallback))
        {
            if (CrossSubjectPiiJoin.IsPii(eventType, eventPropertyName))
            {
                return (targetName, eventPropertyName);
            }
        }

        var readModelNames = CrossSubjectPiiJoin.GetMembers(readModelType)
            .Select(member => member.Name)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var member in CrossSubjectPiiJoin.GetMembers(eventType))
        {
            if (readModelNames.Contains(member.Name) && CrossSubjectPiiJoin.IsPii(eventType, member.Name))
            {
                return (member.Name, member.Name);
            }
        }

        return null;
    }

    static bool TryGetOnProperty(SyntaxNodeAnalysisContext context, ExpressionSyntax builderCallback, out string on)
    {
        on = string.Empty;

        foreach (var invocation in builderCallback.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member ||
                member.Name.Identifier.Text != OnMethodName ||
                invocation.ArgumentList.Arguments.Count != 1 ||
                !IsJoinBuilderMethod(context, invocation))
            {
                continue;
            }

            if (TryGetSimpleMemberName(invocation.ArgumentList.Arguments[0].Expression, out on))
            {
                return true;
            }
        }

        return false;
    }

    static IEnumerable<(string TargetName, string EventPropertyName)> GetExplicitMappings(ExpressionSyntax builderCallback)
    {
        foreach (var toInvocation in builderCallback.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (toInvocation.Expression is not MemberAccessExpressionSyntax toMember ||
                toMember.Name.Identifier.Text != ToMethodName ||
                toInvocation.ArgumentList.Arguments.Count != 1 ||
                toMember.Expression is not InvocationExpressionSyntax setInvocation ||
                setInvocation.Expression is not MemberAccessExpressionSyntax setMember ||
                setMember.Name.Identifier.Text != SetMethodName ||
                setInvocation.ArgumentList.Arguments.Count != 1)
            {
                continue;
            }

            if (TryGetSimpleMemberName(setInvocation.ArgumentList.Arguments[0].Expression, out var targetName) &&
                TryGetSimpleMemberName(toInvocation.ArgumentList.Arguments[0].Expression, out var eventPropertyName))
            {
                yield return (targetName, eventPropertyName);
            }
        }
    }

    static bool IsJoinBuilderMethod(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation) =>
        context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
        method.ContainingType?.OriginalDefinition.Name == JoinBuilderInterfaceName;

    static bool TryGetSimpleMemberName(ExpressionSyntax expression, out string name)
    {
        name = string.Empty;

        if (expression is not SimpleLambdaExpressionSyntax lambda ||
            lambda.Body is not MemberAccessExpressionSyntax memberAccess ||
            memberAccess.Expression is not IdentifierNameSyntax identifier ||
            identifier.Identifier.Text != lambda.Parameter.Identifier.Text)
        {
            return false;
        }

        name = memberAccess.Name.Identifier.Text;
        return true;
    }
}
