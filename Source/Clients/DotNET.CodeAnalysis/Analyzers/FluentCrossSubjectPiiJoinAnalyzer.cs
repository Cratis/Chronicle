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
/// whose persisted runtime subject cannot be proven to be the read model's compliance subject.
/// </summary>
/// <remarks>
/// The model-bound equivalent is covered by <see cref="CrossSubjectPiiJoinAnalyzer"/>. An explicitly different
/// boundary reports <see cref="DiagnosticIds.CrossSubjectPiiJoin"/>; runtime-subject equality that cannot be
/// proven reports <see cref="DiagnosticIds.UnprovableCrossSubjectPiiJoin"/>.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FluentCrossSubjectPiiJoinAnalyzer : DiagnosticAnalyzer
{
    const string JoinMethodName = "Join";
    const string OnMethodName = "On";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        CrossSubjectPiiJoin.Rule,
        CrossSubjectPiiJoin.UnprovableRule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(startContext =>
        {
            if (FluentProjectionSymbols.TryCreate(startContext.Compilation) is { } symbols)
            {
                startContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeInvocation(syntaxContext, symbols),
                    SyntaxKind.InvocationExpression);
            }
        });
    }

    static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, FluentProjectionSymbols symbols)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax member ||
            member.Name.Identifier.Text != JoinMethodName ||
            invocation.ArgumentList.Arguments.Count > 1)
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method ||
            !FluentProjectionSymbols.IsMethodOn(method, symbols.ProjectionBuilder) ||
            method.TypeArguments.FirstOrDefault() is not INamedTypeSymbol eventType ||
            method.ContainingType.TypeArguments.FirstOrDefault() is not INamedTypeSymbol readModelType)
        {
            return;
        }

        var builderCallbackExpression = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
        var builderCallback = default(FluentProjectionCallback);
        var hasResolvedCallback = builderCallbackExpression is not null &&
                                  FluentProjectionCallback.TryResolve(context, builderCallbackExpression, out builderCallback);
        var on = string.Empty;
        var hasExplicitOn = hasResolvedCallback && TryGetOnProperty(builderCallback, symbols, out on);
        var (isChildScope, documentReadModelType) = FluentProjectionMappings.ResolveStoredDocumentScope(context, invocation, readModelType, symbols);
        var subjectName = CrossSubjectPiiJoin.GetSubjectMemberName(documentReadModelType);
        var apparentSubject = subjectName ?? CrossSubjectPiiJoin.IdentifierName;

        // Root joins require On. A callbackless or On-less root shape is invalid independently of compliance,
        // so do not turn invalid source into the conservative warning. Child joins validly use IdentifiedBy and
        // can omit the callback altogether.
        if (!hasExplicitOn && !isChildScope)
        {
            return;
        }

        var autoMapIsOn = FluentProjectionMappings.AutoMapIsOn(context, invocation, readModelType, symbols);
        var explicitMappings = GetExplicitMappings(hasResolvedCallback ? builderCallback : null, symbols, readModelType, eventType);
        var source = CrossSubjectPiiJoin.FindPiiReachingTheReadModel(
            eventType,
            readModelType,
            explicitMappings,
            autoMapIsOn);

        if (source is not { } piiSource)
        {
            return;
        }

        var rule = hasExplicitOn && !string.Equals(on, subjectName, StringComparison.OrdinalIgnoreCase)
            ? CrossSubjectPiiJoin.Rule
            : CrossSubjectPiiJoin.UnprovableRule;

        context.ReportDiagnostic(Diagnostic.Create(
            rule,
            member.Name.GetLocation(),
            piiSource.TargetName,
            eventType.Name,
            piiSource.EventPropertyName,
            hasExplicitOn ? on : apparentSubject));
    }

    static IEnumerable<(string TargetName, string EventPropertyName)> GetExplicitMappings(
        FluentProjectionCallback? builderCallback,
        FluentProjectionSymbols symbols,
        INamedTypeSymbol readModelType,
        INamedTypeSymbol eventType) =>
        builderCallback is null
            ? []
            : FluentProjectionMappings.GetExplicitMappings(
                builderCallback.Value.SemanticModel,
                builderCallback.Value.Body,
                symbols,
                readModelType,
                eventType);

    static bool TryGetOnProperty(
        FluentProjectionCallback builderCallback,
        FluentProjectionSymbols symbols,
        out string on)
    {
        on = string.Empty;

        var onInvocations = builderCallback.Body.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Where(invocation =>
                invocation.Expression is MemberAccessExpressionSyntax member &&
                member.Name.Identifier.Text == OnMethodName &&
                invocation.ArgumentList.Arguments.Count == 1 &&
                IsJoinBuilderMethod(builderCallback.SemanticModel, invocation, symbols));

        foreach (var invocation in onInvocations)
        {
            if (FluentProjectionMappings.TryGetPropertyPath(
                builderCallback.SemanticModel,
                invocation.ArgumentList.Arguments[0].Expression,
                out on))
            {
                return true;
            }
        }

        return false;
    }

    static bool IsJoinBuilderMethod(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        FluentProjectionSymbols symbols) =>
        semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
        FluentProjectionSymbols.IsMethodOn(method, symbols.JoinBuilder);
}
