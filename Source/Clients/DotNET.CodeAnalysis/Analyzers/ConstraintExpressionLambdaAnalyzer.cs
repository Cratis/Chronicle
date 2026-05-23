// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks expression lambdas passed to constraint builder methods only contain member access.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConstraintExpressionLambdaAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ConstraintExpressionLambdaMustOnlyAccessMembers,
        title: "Constraint expression lambda must only access members",
        messageFormat: "Constraint '{0}' passes an expression lambda with non-member-access code '{1}'. Expression lambdas in the constraint builder are used to extract property paths at startup—they are never executed at runtime.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "In Chronicle's constraint builder API, parameters typed as Expression<Func<...>> are inspected at startup to extract property names for constraint rules—the lambda body is never called at runtime. Writing method invocations, arithmetic, conditional expressions, or any non-member-access code in such a lambda will silently produce an incorrect or incomplete constraint definition. Fix this by replacing the lambda body with a direct property access (e.g. e => e.Email or e => e.OrderId).");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeLambda, SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression);
    }

    static void AnalyzeLambda(SyntaxNodeAnalysisContext context)
    {
        var lambda = (LambdaExpressionSyntax)context.Node;

        // Expression trees cannot have block bodies; only check expression-bodied lambdas
        if (lambda.ExpressionBody is not ExpressionSyntax body)
        {
            return;
        }

        // Check if this lambda is inside the Define() method of a class implementing IConstraint
        var enclosingMethod = lambda.Ancestors()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        if (enclosingMethod?.Identifier.Text != "Define")
        {
            return;
        }

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(enclosingMethod);
        if (methodSymbol is null)
        {
            return;
        }

        if (!WellKnownTypes.ImplementsIConstraint(methodSymbol.ContainingType, context.Compilation))
        {
            return;
        }

        // Only flag lambdas that are being converted to Expression<Func<...>> — the compiler infers this
        var typeInfo = context.SemanticModel.GetTypeInfo(lambda);
        if (!WellKnownTypes.IsExpressionType(typeInfo.ConvertedType))
        {
            return;
        }

        // The lambda body must be a pure member-access chain
        if (!WellKnownTypes.IsPureMemberAccessChain(body))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                body.GetLocation(),
                methodSymbol.ContainingType.Name,
                body.ToString()));
        }
    }
}
