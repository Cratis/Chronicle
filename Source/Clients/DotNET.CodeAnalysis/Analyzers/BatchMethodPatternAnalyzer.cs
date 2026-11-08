// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that warns when a batch method matches on an event type it does not observe, a case that can never be reached.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BatchMethodPatternAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.BatchMethodMatchesUnobservedEventType,
        title: "Batch method matches an event type it does not observe",
        messageFormat: "Batch method '{0}' matches on event type '{1}', which it does not observe, so the match is never reached",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A batch method receives only the events of the types its [Observes<T>] attributes name. Matching on another event type is dead code, and usually means an [Observes<T>] is missing. Add the attribute or remove the case.");

    static readonly ImmutableArray<OperationKind> PatternKinds = ImmutableArray.Create(
        OperationKind.DeclarationPattern,
        OperationKind.TypePattern,
        OperationKind.RecursivePattern,
        OperationKind.IsType);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Analyze, PatternKinds);
    }

    static void Analyze(OperationAnalysisContext context)
    {
        var matchedType = context.Operation switch
        {
            IDeclarationPatternOperation declaration => declaration.MatchedType,
            ITypePatternOperation type => type.MatchedType,
            IRecursivePatternOperation recursive => recursive.MatchedType,
            IIsTypeOperation isType => isType.TypeOperand,
            _ => null
        };

        if (matchedType is null || ContainingMethod(context.ContainingSymbol) is not { } method)
        {
            return;
        }

        var compilation = context.Compilation;
        if (!BatchObservers.IsBatchMethod(method, compilation))
        {
            return;
        }

        var observed = BatchObservers.Observed(method, compilation).Select(_ => _.EventType).ToArray();
        if (observed.Length == 0 ||
            !ExplicitRegistrationEvidence.For(compilation, context.CancellationToken).IsEventType(matchedType, compilation) ||
            observed.Any(eventType => BatchObservers.Overlaps(eventType, matchedType, compilation)))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), method.Name, matchedType.Name));
    }

    static IMethodSymbol? ContainingMethod(ISymbol? symbol)
    {
        while (symbol is IMethodSymbol { MethodKind: MethodKind.LambdaMethod or MethodKind.LocalFunction })
        {
            symbol = symbol.ContainingSymbol;
        }

        return symbol as IMethodSymbol;
    }
}
