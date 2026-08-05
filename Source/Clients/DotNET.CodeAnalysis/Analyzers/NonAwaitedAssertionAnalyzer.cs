// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports an awaitable-returning testing assertion whose result is discarded, making the
/// assertion incapable of ever failing.
/// </summary>
/// <remarks>
/// The compiler's own CS4014 only fires inside an <see langword="async"/> method, and the natural shape of a
/// spec is a <see langword="void"/>-bodied fact — precisely where CS4014 says nothing. The assertion's exception
/// is thrown on a <see cref="System.Threading.Tasks.Task"/> or <see cref="System.Threading.Tasks.ValueTask"/>
/// nobody observes, so the spec passes no matter what the system does. Sibling assertions on the same surface
/// are synchronous and <see langword="void"/>, so
/// nothing at the call site hints that these particular ones behave differently.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NonAwaitedAssertionAnalyzer : DiagnosticAnalyzer
{
    const string AssertionMethodPrefix = "Should";
    const string TestingNamespaceSegment = ".Testing.";
    const string IntegrationTestingNamespaceSegment = ".XUnit.";
    const string CratisNamespacePrefix = "Cratis.";
    const string TaskNamespaceName = "System.Threading.Tasks";
    const string TaskTypeName = "Task";
    const string ValueTaskTypeName = "ValueTask";

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.NonAwaitedAssertion,
        title: "Assertion result is discarded and can never fail",
        messageFormat: "'{0}' returns an awaitable that is never awaited, so the assertion can never fail. Await it and make the containing member 'async Task'.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Awaitable-returning assertions throw on a Task or ValueTask the caller must observe. Discarding that awaitable from a void-bodied test silently turns the assertion into a no-op that passes regardless of behavior, and the compiler's CS4014 does not fire outside an async method. Await the assertion and declare the containing member as 'async Task'.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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

        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (!IsAwaitableAssertion(methodSymbol) || !IsResultDiscarded(invocation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), methodSymbol.Name));
    }

    static bool IsAwaitableAssertion(IMethodSymbol methodSymbol)
    {
        if (!methodSymbol.Name.StartsWith(AssertionMethodPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        if (!IsAwaitable(methodSymbol.ReturnType))
        {
            return false;
        }

        var containingNamespace = methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString();
        if (containingNamespace is null)
        {
            return false;
        }

        return IsCratisTestingSurface(containingNamespace);
    }

    static bool IsAwaitable(ITypeSymbol returnType)
    {
        // Task, Task<T>, ValueTask and ValueTask<T> all carry the assertion exception to a caller that has to
        // observe it. Matching the original definition by namespace and name covers the generic shapes without
        // depending on how Roslyn renders their type parameters.
        return returnType.OriginalDefinition is INamedTypeSymbol awaitable &&
               awaitable.ContainingNamespace?.ToDisplayString() == TaskNamespaceName &&
               (awaitable.Name == TaskTypeName || awaitable.Name == ValueTaskTypeName);
    }

    static bool IsCratisTestingSurface(string containingNamespace)
    {
        // Scoped to the Cratis testing surfaces so an unrelated Should-prefixed API is never flagged. The
        // kernel-backed integration assertions are one of those surfaces without sitting under '.Testing', so
        // they get their own segment. The trailing dot makes a segment match a namespace that ends in it as
        // well as one nested below it.
        if (!containingNamespace.StartsWith(CratisNamespacePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var namespaceWithTrailingDot = containingNamespace + ".";

        return namespaceWithTrailingDot.Contains(TestingNamespaceSegment) ||
               namespaceWithTrailingDot.Contains(IntegrationTestingNamespaceSegment);
    }

    static bool IsResultDiscarded(InvocationExpressionSyntax invocation) =>
        invocation.Parent switch
        {
            // A statement on its own line — nothing observes the returned awaitable.
            ExpressionStatementSyntax => true,

            // An expression body observes the awaitable only when the member hands it back to the caller.
            ArrowExpressionClauseSyntax arrow => ReturnsVoid(arrow.Parent),

            // Awaited, returned, assigned, or passed along — all observe it.
            _ => false
        };

    static bool ReturnsVoid(SyntaxNode? member) =>
        member switch
        {
            MethodDeclarationSyntax method => IsVoid(method.ReturnType),
            LocalFunctionStatementSyntax local => IsVoid(local.ReturnType),
            _ => false
        };

    static bool IsVoid(TypeSyntax returnType) =>
        returnType is PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.VoidKeyword };
}
