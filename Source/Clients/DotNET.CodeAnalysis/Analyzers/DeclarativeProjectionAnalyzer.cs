// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks if declarative projection methods reference event types with the EventType attribute.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DeclarativeProjectionAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DeclarativeProjectionEventTypeMustHaveAttribute,
        title: "Declarative projection event type must have [EventType] attribute",
        messageFormat: "Type '{0}' used in declarative projection must be marked with [EventType] attribute",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Event types referenced in declarative projections must be marked with the [EventType] attribute.");

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

        // Check if this is a method call on a projection builder
        if (invocation.Expression is not MemberAccessExpressionSyntax)
        {
            return;
        }

        if (!(context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol))
        {
            return;
        }

        // Check if the method name indicates a projection builder method (From, Join, etc.)
        if (!IsProjectionBuilderMethod(methodSymbol))
        {
            return;
        }

        // Check if the method has generic type arguments
        if (methodSymbol is not IMethodSymbol { IsGenericMethod: true } genericMethod)
        {
            return;
        }

        // Check each generic type argument
        foreach (var typeArgument in genericMethod.TypeArguments)
        {
            if (typeArgument.SpecialType == SpecialType.System_Object)
            {
                continue;
            }

            if (!WellKnownTypes.HasEventTypeAttribute(typeArgument))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    invocation.GetLocation(),
                    typeArgument.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    static bool IsProjectionBuilderMethod(IMethodSymbol methodSymbol)
    {
        // Check if the containing type is a projection builder
        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
        {
            return false;
        }

        var typeName = containingType.ToDisplayString();

        // Check for IProjectionBuilderFor<T> or related interfaces
        return typeName.Contains("IProjectionBuilderFor") ||
               typeName.Contains("IProjectionBuilder") ||
               typeName.Contains("IFromBuilder") ||
               typeName.Contains("IJoinBuilder") ||
               typeName.Contains("IChildrenBuilder");
    }
}
