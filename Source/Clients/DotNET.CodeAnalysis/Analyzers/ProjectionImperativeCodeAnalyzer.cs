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
/// Analyzer that checks that the Define() method on a class implementing IProjectionFor&lt;T&gt; does not contain imperative code.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ProjectionImperativeCodeAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ProjectionDefineMustNotContainImperativeCode,
        title: "Projection Define() must not contain imperative code",
        messageFormat: "Projection '{0}' Define() method contains imperative statement '{1}'. Projection definitions only declare shape, they do not execute.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Chronicle analyses the projection's Define() method at startup to build a static mapping definition; the method body is never executed at runtime. Imperative statements—conditionals, loops, or assignments—will be silently ignored and will not affect the read model. Remove all imperative code from Define() and express the mapping using only the fluent builder methods provided by the IProjectionBuilderFor<T> parameter (e.g., .From<TEvent>(), .Set(), .Join<TEvent>()).");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        if (methodDeclaration.Identifier.Text != "Define")
        {
            return;
        }

        if (methodDeclaration.Body is null && methodDeclaration.ExpressionBody is null)
        {
            return;
        }

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
        if (methodSymbol is null)
        {
            return;
        }

        var containingType = methodSymbol.ContainingType;
        if (!WellKnownTypes.ImplementsIProjectionFor(containingType, context.Compilation))
        {
            return;
        }

        if (methodDeclaration.Body is not null)
        {
            CheckBodyStatements(context, methodDeclaration.Body.Statements, containingType.Name);
        }
    }

    static void CheckBodyStatements(SyntaxNodeAnalysisContext context, SyntaxList<StatementSyntax> statements, string typeName)
    {
        var diagnostics = statements
            .Where(WellKnownTypes.IsImperativeStatement)
            .Select(statement => Diagnostic.Create(
                Rule,
                statement.GetLocation(),
                typeName,
                statement.Kind().ToString()));

        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }
}
