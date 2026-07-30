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
/// Analyzer that reports a <see cref="System.Threading.Tasks.Task"/>-returning testing assertion whose result is
/// discarded, making the assertion incapable of ever failing.
/// </summary>
/// <remarks>
/// The compiler's own CS4014 only fires inside an <see langword="async"/> method, and the natural shape of a
/// spec is a <see langword="void"/>-bodied fact — precisely where CS4014 says nothing. The assertion's exception
/// is thrown on a <see cref="System.Threading.Tasks.Task"/> nobody observes, so the spec passes no matter what
/// the system does. Sibling assertions on the same surface are synchronous and <see langword="void"/>, so
/// nothing at the call site hints that these particular ones behave differently.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NonAwaitedAssertionAnalyzer : DiagnosticAnalyzer
{
    const string AssertionMethodPrefix = "Should";
    const string TestingNamespaceSegment = ".Testing.";
    const string CratisNamespacePrefix = "Cratis.";
    const string TaskTypeName = "System.Threading.Tasks.Task";

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.NonAwaitedAssertion,
        title: "Assertion result is discarded and can never fail",
        messageFormat: "'{0}' returns a Task that is never awaited, so the assertion can never fail. Await it and make the containing member 'async Task'.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Task-returning assertions throw on a Task the caller must observe. Discarding that Task from a void-bodied test silently turns the assertion into a no-op that passes regardless of behavior, and the compiler's CS4014 does not fire outside an async method. Await the assertion and declare the containing member as 'async Task'.");

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

        if (!IsTaskReturningAssertion(methodSymbol) || !IsResultDiscarded(invocation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), methodSymbol.Name));
    }

    static bool IsTaskReturningAssertion(IMethodSymbol methodSymbol)
    {
        if (!methodSymbol.Name.StartsWith(AssertionMethodPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        if (methodSymbol.ReturnType.OriginalDefinition.ToDisplayString() != TaskTypeName)
        {
            return false;
        }

        var containingNamespace = methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString();
        if (containingNamespace is null)
        {
            return false;
        }

        // Scoped to the Cratis testing surfaces so an unrelated Should-prefixed API is never flagged. The
        // trailing dot makes the segment match a namespace that ends in '.Testing' as well as one nested below it.
        return containingNamespace.StartsWith(CratisNamespacePrefix, StringComparison.Ordinal) &&
               (containingNamespace + ".").Contains(TestingNamespaceSegment);
    }

    static bool IsResultDiscarded(InvocationExpressionSyntax invocation) =>
        invocation.Parent switch
        {
            // A statement on its own line — nothing observes the returned Task.
            ExpressionStatementSyntax => true,

            // An expression body observes the Task only when the member hands it back to the caller.
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
