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

        var source = CrossSubjectPiiJoin.FindPiiReachingTheReadModel(
            eventType,
            readModelType,
            FluentProjectionMappings.GetExplicitMappings(builderCallback),
            FluentProjectionMappings.AutoMapIsOn(invocation, readModelType));

        if (source is not { } piiSource)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            CrossSubjectPiiJoin.Rule,
            member.Name.GetLocation(),
            piiSource.TargetName,
            eventType.Name,
            piiSource.EventPropertyName,
            on));
    }

    static bool TryGetOnProperty(SyntaxNodeAnalysisContext context, ExpressionSyntax builderCallback, out string on)
    {
        on = string.Empty;

        var onInvocations = builderCallback.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(invocation =>
                invocation.Expression is MemberAccessExpressionSyntax member &&
                member.Name.Identifier.Text == OnMethodName &&
                invocation.ArgumentList.Arguments.Count == 1 &&
                IsJoinBuilderMethod(context, invocation));

        foreach (var invocation in onInvocations)
        {
            if (FluentProjectionMappings.TryGetSimpleMemberName(invocation.ArgumentList.Arguments[0].Expression, out on))
            {
                return true;
            }
        }

        return false;
    }

    static bool IsJoinBuilderMethod(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation) =>
        context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
        method.ContainingType?.OriginalDefinition.Name == JoinBuilderInterfaceName;
}
