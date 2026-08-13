// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports a fluent projection which re-keys the root document of a <b>passive</b> read model,
/// so that a read replays a stream the events were never appended to.
/// </summary>
/// <remarks>
/// The model-bound equivalent is covered by <see cref="PassiveProjectionKeyRedirectionAnalyzer"/>; both report
/// <see cref="DiagnosticIds.PassiveProjectionKeyRedirection"/>.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FluentPassiveProjectionKeyRedirectionAnalyzer : DiagnosticAnalyzer
{
    const string FromMethodName = "From";
    const string ProjectionBuilderForMetadataName = "Cratis.Chronicle.Projections.IProjectionBuilderFor`1";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(PassiveProjectionKeyRedirection.Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(startContext =>
        {
            if (FluentProjectionSymbols.TryCreate(startContext.Compilation) is { } symbols &&
                startContext.Compilation.GetTypeByMetadataName(ProjectionBuilderForMetadataName) is { } projectionBuilderFor)
            {
                startContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeInvocation(syntaxContext, symbols, projectionBuilderFor),
                    SyntaxKind.InvocationExpression);
            }
        });
    }

    static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        FluentProjectionSymbols symbols,
        INamedTypeSymbol projectionBuilderFor)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax member ||
            member.Name.Identifier.Text != FromMethodName ||
            invocation.ArgumentList.Arguments.Count != 1)
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

        if (!FluentProjectionCallback.TryResolve(context, invocation.ArgumentList.Arguments[0].Expression, out var builderCallback))
        {
            return;
        }

        var (isChildScope, documentReadModelType) = FluentProjectionMappings.ResolveStoredDocumentScope(context, invocation, readModelType, symbols);
        if (isChildScope)
        {
            // A child block keys the child inside its containing document. The document the passive read
            // resolves is still the parent's, so nothing here moves the read off the event's own stream.
            return;
        }

        if (!FluentProjectionMappings.PassiveIsOn(context, invocation, documentReadModelType, symbols, projectionBuilderFor))
        {
            return;
        }

        if (FindRootRedirection(builderCallback, eventType, readModelType, symbols) is not { } redirection)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            PassiveProjectionKeyRedirection.Rule,
            redirection.Location,
            redirection.MethodName,
            documentReadModelType.Name,
            redirection.Key,
            eventType.Name));
    }

    /// <summary>
    /// Find the call inside a <c>From&lt;TEvent&gt;</c> callback that points the root document at a key other
    /// than the stream the event was appended to.
    /// </summary>
    /// <param name="builderCallback">The <c>From</c> builder callback.</param>
    /// <param name="eventType">The event the block reads.</param>
    /// <param name="readModelType">The read model being projected.</param>
    /// <param name="symbols">The Chronicle builder symbols.</param>
    /// <returns>The redirection, or <see langword="null"/> when nothing redirects the root document.</returns>
    static (string MethodName, string Key, Location Location)? FindRootRedirection(
        FluentProjectionCallback builderCallback,
        INamedTypeSymbol eventType,
        INamedTypeSymbol readModelType,
        FluentProjectionSymbols symbols) =>
        builderCallback.Body.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => Describe(builderCallback.SemanticModel, invocation, eventType, readModelType, symbols))
            .FirstOrDefault(redirection => redirection is not null);

    static (string MethodName, string Key, Location Location)? Describe(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        INamedTypeSymbol eventType,
        INamedTypeSymbol readModelType,
        FluentProjectionSymbols symbols)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member ||
            !KeyRedirection.Root.Contains(member.Name.Identifier.Text) ||
            !KeyRedirection.IsDocumentRedirectionFor(semanticModel, invocation, eventType, readModelType, false, symbols) ||
            PassiveProjectionKeyRedirection.DescribeRootKey(semanticModel, invocation) is not { } described)
        {
            return null;
        }

        return (described.MethodName, described.Key, member.Name.GetLocation());
    }
}
