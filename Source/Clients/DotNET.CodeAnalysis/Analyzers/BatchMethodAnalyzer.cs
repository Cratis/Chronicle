// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks each method of a reactor or reducer against the rules that keep batch methods apart from per-event methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BatchMethodAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor ObservesOnlyOnBatchMethodsRule = new(
        id: DiagnosticIds.ObservesOnlyOnBatchMethods,
        title: "[Observes<T>] is only for batch methods",
        messageFormat: "Method '{0}' declares [Observes<{1}>] but does not take an EventBatch as its first parameter",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[Observes<T>] declares the event types of a batch method, a method whose first parameter is an EventBatch. A per-event method declares its event type with its first parameter instead, so the attribute on it is a mistake that fails registration.");

    static readonly DiagnosticDescriptor MustObserveRule = new(
        id: DiagnosticIds.BatchMethodMustObserveEventTypes,
        title: "Batch method observes no event types",
        messageFormat: "Batch method '{0}' takes an EventBatch but declares no [Observes<T>]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A batch method receives the events of the types its [Observes<T>] attributes name. Without one it receives nothing and registration fails. Add [Observes<TEvent>] once per event type the method handles.");

    static readonly DiagnosticDescriptor BatchContextRule = new(
        id: DiagnosticIds.BatchContextOnlyOnBatchMethods,
        title: "BatchContext is only for batch methods",
        messageFormat: "Per-event method '{0}' takes a BatchContext, which only batch methods receive",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "BatchContext describes the page a batch method is called for. A per-event method is called once per event and takes the EventContext instead. Registration fails for a per-event method that takes a BatchContext.");

    static readonly DiagnosticDescriptor ReactorReturnTypeRule = new(
        id: DiagnosticIds.ReactorBatchMethodMustReturnTaskOrVoid,
        title: "Reactor batch method must return Task or void",
        messageFormat: "Reactor batch method '{0}' returns '{1}'. A reactor batch method returns Task or void.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Returning events or other side effects from a reactor batch method is not supported, because an appended event cannot be tied to the one event it came from. Append through an injected event log instead, and return Task or void. Registration fails for any other return type.");

    static readonly DiagnosticDescriptor SignatureRule = new(
        id: DiagnosticIds.BatchMethodSignatureMustMatchAllowed,
        title: "Batch method has a signature Chronicle cannot call",
        messageFormat: "Batch method '{0}' cannot be called: {1}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A reactor batch method takes (EventBatch batch, BatchContext context). A reducer batch method takes (EventBatch events, TReadModel? current, BatchContext context) and returns TReadModel? or Task<TReadModel?>; on a self-reducing read model it is public, static and named Reduce, the only name the client discovers. Further parameters are a CancellationToken or services resolved from the service provider.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ObservesOnlyOnBatchMethodsRule, MustObserveRule, BatchContextRule, ReactorReturnTypeRule, SignatureRule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var symbols = BatchSymbols.For(start.Compilation, start.CancellationToken);
            if (symbols is null)
            {
                return;
            }

            start.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, symbols), SymbolKind.Method);
        });
    }

    static void AnalyzeMethod(SymbolAnalysisContext context, BatchSymbols symbols)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (method.MethodKind != MethodKind.Ordinary)
        {
            return;
        }

        if (!BatchObservers.IsBatchMethod(method, symbols))
        {
            foreach (var (eventType, attribute) in BatchObservers.Observed(method, symbols))
            {
                context.ReportDiagnostic(Diagnostic.Create(ObservesOnlyOnBatchMethodsRule, BatchObservers.LocationOf(attribute, method), method.Name, eventType.Name));
            }

            AnalyzePerEventMethod(context, method, symbols);
            return;
        }

        var kind = BatchObservers.KindOf(method.ContainingType, symbols);
        if (kind == BatchObserverKind.None)
        {
            return;
        }

        if (!BatchObservers.Observed(method, symbols).Any())
        {
            context.ReportDiagnostic(Diagnostic.Create(MustObserveRule, method.Locations.FirstOrDefault(), method.Name));
        }

        if (kind == BatchObserverKind.Reactor)
        {
            AnalyzeReactorBatchMethod(context, method, symbols);
            return;
        }

        var problem = BatchMethodSignature.ProblemWithReducerMethod(method, kind, symbols);
        if (problem is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(SignatureRule, method.Locations.FirstOrDefault(), method.Name, problem));
        }
    }

    static void AnalyzeReactorBatchMethod(SymbolAnalysisContext context, IMethodSymbol method, BatchSymbols symbols)
    {
        if (!method.ReturnsVoid && !BatchSymbols.Is(method.ReturnType, symbols.Task))
        {
            context.ReportDiagnostic(Diagnostic.Create(ReactorReturnTypeRule, method.Locations.FirstOrDefault(), method.Name, method.ReturnType.ToDisplayString()));
        }

        var problem = BatchMethodSignature.ProblemWithDependencies(method, 1, symbols);
        if (problem is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(SignatureRule, method.Locations.FirstOrDefault(), method.Name, problem));
        }
    }

    static void AnalyzePerEventMethod(SymbolAnalysisContext context, IMethodSymbol method, BatchSymbols symbols)
    {
        var batchContext = method.Parameters.FirstOrDefault(parameter => BatchSymbols.Is(parameter.Type, symbols.BatchContext));
        if (batchContext is null)
        {
            return;
        }

        var observer = method.ContainingType;
        var kind = BatchObservers.KindOf(observer, symbols);
        var takesItInstead = kind == BatchObserverKind.Reducer
            ? BatchObservers.IsReduceShapedWithBatchContext(method, observer, symbols) && !BatchObservers.HasBatchMethod(observer, symbols)
            : BatchObservers.IsPerEventMethod(method, observer, kind, symbols);
        if (takesItInstead)
        {
            context.ReportDiagnostic(Diagnostic.Create(BatchContextRule, batchContext.Locations.FirstOrDefault(), method.Name));
        }
    }
}
