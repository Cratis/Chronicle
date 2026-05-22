// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks that the Define() method on a class implementing IConstraint does not contain imperative code.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConstraintImperativeCodeAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ConstraintDefineMustNotContainImperativeCode,
        title: "Constraint Define() must not contain imperative code",
        messageFormat: "Constraint '{0}' Define() method contains imperative statement '{1}'. Constraint definitions only declare rules, they do not execute.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Chronicle analyses the constraint's Define() method at startup to register constraint rules; the method body is never executed at runtime. Imperative statements—conditionals, loops, or assignments—will be silently ignored and will not affect constraint evaluation. Remove all imperative code from Define() and express the constraint using only the fluent builder methods provided by the IConstraintBuilder parameter.");

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
        if (!WellKnownTypes.ImplementsIConstraint(containingType, context.Compilation))
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
        foreach (var statement in statements)
        {
            if (WellKnownTypes.IsImperativeStatement(statement))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    statement.GetLocation(),
                    typeName,
                    statement.Kind().ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
