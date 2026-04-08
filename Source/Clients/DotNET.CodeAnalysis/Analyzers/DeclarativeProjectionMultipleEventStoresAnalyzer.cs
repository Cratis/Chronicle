// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks if a declarative projection references event types from multiple event stores.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DeclarativeProjectionMultipleEventStoresAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DeclarativeProjectionEventTypesMustBeFromSameEventStore,
        title: "Declarative projection event types must be from the same event store",
        messageFormat: "Declarative projection references event types from multiple event stores: {0}. All event types in a projection must originate from the same event store.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A declarative projection may only reference event types from a single event store.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCodeBlockStartAction<SyntaxKind>(AnalyzeCodeBlock);
    }

    static void AnalyzeCodeBlock(CodeBlockStartAnalysisContext<SyntaxKind> context)
    {
        var eventStoreNames = new List<string>();
        var firstConflictLocation = (Location?)null;

        context.RegisterSyntaxNodeAction(
            nodeContext =>
            {
                var invocation = (InvocationExpressionSyntax)nodeContext.Node;

                if (nodeContext.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
                {
                    return;
                }

                if (!IsProjectionBuilderMethod(methodSymbol))
                {
                    return;
                }

                if (!methodSymbol.IsGenericMethod)
                {
                    return;
                }

                foreach (var typeArgument in methodSymbol.TypeArguments)
                {
                    if (typeArgument.SpecialType == SpecialType.System_Object)
                    {
                        continue;
                    }

                    if (!WellKnownTypes.HasEventTypeAttribute(typeArgument))
                    {
                        continue;
                    }

                    var eventStore = WellKnownTypes.GetEventStoreNameOrDefault(typeArgument);

                    if (!eventStoreNames.Contains(eventStore))
                    {
                        eventStoreNames.Add(eventStore);
                    }

                    if (eventStoreNames.Count > 1 && firstConflictLocation is null)
                    {
                        firstConflictLocation = invocation.GetLocation();
                    }
                }
            },
            SyntaxKind.InvocationExpression);

        context.RegisterCodeBlockEndAction(endContext =>
        {
            if (eventStoreNames.Count > 1)
            {
                endContext.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    firstConflictLocation ?? endContext.CodeBlock.GetLocation(),
                    string.Join(", ", eventStoreNames.Select(WellKnownTypes.FormatEventStoreName))));
            }
        });
    }

    static bool IsProjectionBuilderMethod(IMethodSymbol methodSymbol)
    {
        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
        {
            return false;
        }

        var typeName = containingType.ToDisplayString();

        return typeName.Contains("IProjectionBuilderFor") ||
               typeName.Contains("IProjectionBuilder") ||
               typeName.Contains("IFromBuilder") ||
               typeName.Contains("IJoinBuilder") ||
               typeName.Contains("IChildrenBuilder");
    }
}
