// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports a fluent <c>builder.From&lt;TEvent&gt;(_ =&gt; _.UsingKey(...))</c> carrying a
/// <c>[PII]</c> value onto a read model whose document is keyed by something other than the stream the event
/// was appended to.
/// </summary>
/// <remarks>
/// The model-bound equivalent is covered by <see cref="KeyRedirectionPiiAnalyzer"/>; both report
/// <see cref="DiagnosticIds.KeyRedirectionPii"/>.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FluentKeyRedirectionPiiAnalyzer : DiagnosticAnalyzer
{
    const string AddChildMethodName = "AddChild";
    const string FromMethodName = "From";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(KeyRedirectionPii.Rule);

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

        var builderCallbackExpression = invocation.ArgumentList.Arguments[0].Expression;
        if (!FluentProjectionCallback.TryResolve(context, builderCallbackExpression, out var builderCallback))
        {
            return;
        }

        var (isChildScope, documentReadModelType) = FluentProjectionMappings.ResolveStoredDocumentScope(context, invocation, readModelType, symbols);

        var autoMapIsOn = FluentProjectionMappings.AutoMapIsOn(context, invocation, readModelType, symbols);
        if (FindRedirection(builderCallback, eventType, readModelType, isChildScope, symbols) is { } redirection)
        {
            ReportIfPiiReachesRedirectedDocument(
                context,
                eventType,
                readModelType,
                FluentProjectionMappings.GetExplicitMappings(builderCallback.SemanticModel, builderCallback.Body, symbols, readModelType, eventType),
                autoMapIsOn,
                redirection,
                documentReadModelType);
        }

        foreach (var addChild in builderCallback.Body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (builderCallback.SemanticModel.GetSymbolInfo(addChild).Symbol is not IMethodSymbol addChildMethod ||
                addChildMethod.Name != AddChildMethodName ||
                !FluentProjectionSymbols.IsMethodOn(addChildMethod, symbols.ReadModelPropertiesBuilder) ||
                addChildMethod.ContainingType.TypeArguments.Length < 2 ||
                !SymbolEqualityComparer.Default.Equals(addChildMethod.ContainingType.TypeArguments[0], readModelType) ||
                !SymbolEqualityComparer.Default.Equals(addChildMethod.ContainingType.TypeArguments[1], eventType) ||
                addChild.ArgumentList.Arguments.Count != 2 ||
                !FluentProjectionCallback.TryResolve(
                    context.Compilation,
                    builderCallback.SemanticModel,
                    addChild.ArgumentList.Arguments[1].Expression,
                    out var addChildCallback) ||
                FindAddChildRedirection(addChildCallback, eventType, symbols) is not { } addChildRedirection)
            {
                continue;
            }

            ReportIfPiiReachesRedirectedDocument(
                context,
                eventType,
                readModelType,
                FluentProjectionMappings.GetAddChildMappings(
                    builderCallback.SemanticModel,
                    addChild,
                    symbols,
                    eventType,
                    autoMapIsOn,
                    addChildCallback),
                false,
                addChildRedirection,
                documentReadModelType);
        }
    }

    static void ReportIfPiiReachesRedirectedDocument(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol eventType,
        INamedTypeSymbol readModelType,
        IEnumerable<(string TargetName, string EventPropertyName)> mappings,
        bool autoMapIsOn,
        (string Key, Location Location) redirection,
        INamedTypeSymbol documentReadModelType)
    {
        if (CrossSubjectPiiJoin.FindPiiReachingTheReadModel(
                eventType,
                readModelType,
                mappings,
                autoMapIsOn) is not { } piiSource)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            KeyRedirectionPii.Rule,
            redirection.Location,
            piiSource.TargetName,
            eventType.Name,
            piiSource.EventPropertyName,
            redirection.Key,
            KeyRedirectionPii.ClientReleaseSubjectDescriptionOf(documentReadModelType)));
    }

    /// <summary>
    /// Find the call inside a <c>From&lt;TEvent&gt;</c> callback that points the document at a key other than
    /// the stream the event was appended to.
    /// </summary>
    /// <param name="builderCallback">The <c>From</c> builder callback.</param>
    /// <param name="eventType">The event the block reads.</param>
    /// <param name="readModelType">The read model being projected.</param>
    /// <param name="isChildScope">Whether the From block fills a child inside a containing document.</param>
    /// <param name="symbols">The Chronicle builder symbols.</param>
    /// <returns>The redirected key and where it was written, or <see langword="null"/> when nothing redirects.</returns>
    static (string Key, Location Location)? FindRedirection(
        FluentProjectionCallback builderCallback,
        INamedTypeSymbol eventType,
        INamedTypeSymbol readModelType,
        bool isChildScope,
        FluentProjectionSymbols symbols) =>
        builderCallback.Body.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => Describe(builderCallback.SemanticModel, invocation, eventType, readModelType, isChildScope, symbols))
            .FirstOrDefault(redirection => redirection is not null);

    static (string Key, Location Location)? FindAddChildRedirection(
        FluentProjectionCallback builderCallback,
        INamedTypeSymbol eventType,
        FluentProjectionSymbols symbols) =>
        builderCallback.Body.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Where(invocation =>
                builderCallback.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
                FluentProjectionSymbols.IsMethodOn(method, symbols.AddChildBuilder) &&
                method.ContainingType.TypeArguments.Length == 2 &&
                SymbolEqualityComparer.Default.Equals(method.ContainingType.TypeArguments[1], eventType) &&
                KeyRedirection.Parent.Contains(method.Name))
            .Select(invocation => DescribeKey(builderCallback.SemanticModel, invocation))
            .FirstOrDefault(redirection => redirection is not null);

    static (string Key, Location Location)? Describe(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        INamedTypeSymbol eventType,
        INamedTypeSymbol readModelType,
        bool isChildScope,
        FluentProjectionSymbols symbols)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member ||
            !KeyRedirection.All.Contains(member.Name.Identifier.Text) ||
            !KeyRedirection.IsDocumentRedirectionFor(semanticModel, invocation, eventType, readModelType, isChildScope, symbols))
        {
            return null;
        }

        return DescribeKey(semanticModel, invocation);
    }

    static (string Key, Location Location)? DescribeKey(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member)
        {
            return null;
        }

        var location = member.Name.GetLocation();
        var argument = invocation.ArgumentList.Arguments.Count == 1
            ? invocation.ArgumentList.Arguments[0].Expression
            : null;

        return member.Name.Identifier.Text switch
        {
            KeyRedirection.UsingKey or KeyRedirection.UsingParentKey =>
                argument is not null && FluentProjectionMappings.TryGetPropertyPath(semanticModel, argument, out var eventProperty)
                    ? (eventProperty, location)
                    : null,

            KeyRedirection.UsingKeyFromContext or KeyRedirection.UsingParentKeyFromContext =>
                argument is not null &&
                FluentProjectionMappings.TryGetPropertyPath(semanticModel, argument, out var contextProperty) &&
                KeyRedirectionPii.ContextMemberRedirects(contextProperty)
                    ? (KeyRedirectionPii.DescribeContextKey(contextProperty), location)
                    : null,

            KeyRedirection.UsingConstantKey or KeyRedirection.UsingConstantParentKey =>
                argument is not null && semanticModel.GetConstantValue(argument).Value is string constant
                    ? (KeyRedirectionPii.DescribeConstantKey(constant), location)
                    : (KeyRedirectionPii.ConstantKeyDescription, location),

            KeyRedirection.UsingCompositeKey or KeyRedirection.UsingParentCompositeKey =>
                (KeyRedirectionPii.CompositeKeyDescription, location),

            _ => null
        };
    }
}
