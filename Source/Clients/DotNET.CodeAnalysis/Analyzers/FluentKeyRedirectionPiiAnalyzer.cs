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
    const string FromMethodName = "From";
    const string ProjectionBuilderInterfaceName = "IProjectionBuilder";
    const string ReadModelPropertiesBuilderInterfaceName = "IReadModelPropertiesBuilder";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(KeyRedirectionPii.Rule);

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
            member.Name.Identifier.Text != FromMethodName ||
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

        // An event that names its own compliance subject keeps it whatever the document is keyed by.
        if (KeyRedirectionPii.CarriesItsOwnSubject(eventType))
        {
            return;
        }

        var builderCallback = invocation.ArgumentList.Arguments[0].Expression;

        if (FindRedirection(context, builderCallback, eventType, readModelType) is not { } redirection)
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
            KeyRedirectionPii.Rule,
            redirection.Location,
            piiSource.TargetName,
            eventType.Name,
            piiSource.EventPropertyName,
            redirection.Key,
            KeyRedirectionPii.SubjectMemberNameOf(readModelType)));
    }

    /// <summary>
    /// Find the call inside a <c>From&lt;TEvent&gt;</c> callback that points the document at a key other than
    /// the stream the event was appended to.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    /// <param name="builderCallback">The <c>From</c> builder callback.</param>
    /// <param name="eventType">The event the block reads.</param>
    /// <param name="readModelType">The read model being projected.</param>
    /// <returns>The redirected key and where it was written, or <see langword="null"/> when nothing redirects.</returns>
    static (string Key, Location Location)? FindRedirection(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax builderCallback,
        INamedTypeSymbol eventType,
        INamedTypeSymbol readModelType) =>
        builderCallback.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => Describe(context, invocation, eventType, readModelType))
            .FirstOrDefault(redirection => redirection is not null);

    static (string Key, Location Location)? Describe(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        INamedTypeSymbol eventType,
        INamedTypeSymbol readModelType)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member ||
            !KeyRedirection.All.Contains(member.Name.Identifier.Text) ||
            !IsKeyRedirectionFor(context, invocation, eventType, readModelType))
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
                argument is not null && FluentProjectionMappings.TryGetSimpleMemberName(argument, out var eventProperty)
                    ? (eventProperty, location)
                    : null,

            KeyRedirection.UsingKeyFromContext or KeyRedirection.UsingParentKeyFromContext =>
                argument is not null &&
                FluentProjectionMappings.TryGetSimpleMemberName(argument, out var contextProperty) &&
                KeyRedirectionPii.ContextMemberRedirects(contextProperty)
                    ? (KeyRedirectionPii.DescribeContextKey(contextProperty), location)
                    : null,

            KeyRedirection.UsingConstantKey or KeyRedirection.UsingConstantParentKey =>
                argument is not null && context.SemanticModel.GetConstantValue(argument).Value is string constant
                    ? (KeyRedirectionPii.DescribeConstantKey(constant), location)
                    : null,

            KeyRedirection.UsingCompositeKey or KeyRedirection.UsingParentCompositeKey =>
                (KeyRedirectionPii.CompositeKeyDescription, location),

            _ => null
        };
    }

    /// <summary>
    /// Determine whether an invocation is one of the key-redirection calls belonging to this exact
    /// <c>From&lt;TEvent&gt;</c> block.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    /// <param name="invocation">The invocation to check.</param>
    /// <param name="eventType">The event the block reads.</param>
    /// <param name="readModelType">The read model being projected.</param>
    /// <returns>True when the call redirects this block's key, false otherwise.</returns>
    /// <remarks>
    /// Matching the declaring interface together with both of its type arguments is what keeps a nested
    /// builder — an <c>AddChild</c> callback, a sibling block written on the same chain — from being read as
    /// this block's own key.
    /// </remarks>
    static bool IsKeyRedirectionFor(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        INamedTypeSymbol eventType,
        INamedTypeSymbol readModelType) =>
        context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
        KeyRedirection.All.Contains(method.Name) &&
        method.ContainingType?.OriginalDefinition.Name == ReadModelPropertiesBuilderInterfaceName &&
        method.ContainingType.TypeArguments.Length == 3 &&
        SymbolEqualityComparer.Default.Equals(method.ContainingType.TypeArguments[0], readModelType) &&
        SymbolEqualityComparer.Default.Equals(method.ContainingType.TypeArguments[1], eventType);

    /// <summary>
    /// The builder methods that decide which document a projected event lands on.
    /// </summary>
    static class KeyRedirection
    {
        internal const string UsingKey = nameof(UsingKey);
        internal const string UsingKeyFromContext = nameof(UsingKeyFromContext);
        internal const string UsingParentKey = nameof(UsingParentKey);
        internal const string UsingParentKeyFromContext = nameof(UsingParentKeyFromContext);
        internal const string UsingCompositeKey = nameof(UsingCompositeKey);
        internal const string UsingParentCompositeKey = nameof(UsingParentCompositeKey);
        internal const string UsingConstantKey = nameof(UsingConstantKey);
        internal const string UsingConstantParentKey = nameof(UsingConstantParentKey);

        internal static readonly ImmutableHashSet<string> All = ImmutableHashSet.Create(
            UsingKey,
            UsingKeyFromContext,
            UsingParentKey,
            UsingParentKeyFromContext,
            UsingCompositeKey,
            UsingParentCompositeKey,
            UsingConstantKey,
            UsingConstantParentKey);
    }
}
