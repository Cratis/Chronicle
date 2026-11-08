// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks every event type of a reactor or reducer reaches exactly one of its methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BatchMethodRoutingAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor OneMethodRule = new(
        id: DiagnosticIds.EventTypeMustReachOneMethod,
        title: "An event type reaches more than one method",
        messageFormat: "Event type '{0}' reaches both '{1}' and '{2}' of '{3}'. Each event type reaches exactly one method.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Chronicle never calls a batch method and a per-event method, or two batch methods, for the same event. An event type observed by two batch methods, or observed by a batch method and handled by a per-event method, fails registration. Move the event type to one of the methods.");

    static readonly DiagnosticDescriptor ReducerRule = new(
        id: DiagnosticIds.ReducerBatchMethodMustBeItsOnlyMethod,
        title: "A reducer with a batch method has no other reduce methods",
        messageFormat: "Reducer '{0}' {1}. A reducer has either per-event reduce methods or one batch method.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A reducer is a single fold over one event source's events in order. Splitting those events between methods would break the fold, so a reducer has either per-event reduce methods or exactly one batch method, whose [Observes<T>] list is the reducer's whole set of event types.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(OneMethodRule, ReducerRule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
    }

    static void Analyze(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        var compilation = context.Compilation;
        var methods = BatchObservers.MethodsOf(type).ToArray();
        var batchMethods = methods.Where(method => BatchObservers.IsBatchMethod(method, compilation)).ToArray();
        if (batchMethods.Length == 0)
        {
            return;
        }

        var evidence = ExplicitRegistrationEvidence.For(compilation, context.CancellationToken);
        var kind = BatchObservers.KindOf(type, compilation, evidence);
        var perEventMethods = methods.Where(method => BatchObservers.IsPerEventMethod(method, kind, compilation, evidence)).ToArray();
        switch (kind)
        {
            case BatchObserverKind.Reactor:
                AnalyzeReactor(context, type, batchMethods, perEventMethods);
                break;

            case BatchObserverKind.Reducer:
            case BatchObserverKind.SelfReducer:
                AnalyzeReducer(context, type, batchMethods, perEventMethods);
                break;
        }
    }

    static void AnalyzeReactor(SymbolAnalysisContext context, INamedTypeSymbol type, IMethodSymbol[] batchMethods, IMethodSymbol[] perEventMethods)
    {
        var compilation = context.Compilation;
        var claimed = new List<(ITypeSymbol EventType, IMethodSymbol Method)>();
        foreach (var method in batchMethods)
        {
            foreach (var (eventType, attribute) in BatchObservers.Observed(method, compilation))
            {
                var other = claimed.Find(_ => !SymbolEqualityComparer.Default.Equals(_.Method, method) && BatchObservers.Overlaps(_.EventType, eventType, compilation));
                if (other.Method is not null && Owns(type, method))
                {
                    context.ReportDiagnostic(Diagnostic.Create(OneMethodRule, BatchObservers.LocationOf(attribute, method), eventType.Name, other.Method.Name, method.Name, type.Name));
                }

                claimed.Add((eventType, method));
            }
        }

        foreach (var method in perEventMethods)
        {
            var eventType = method.Parameters[0].Type;
            var other = claimed.Find(_ => BatchObservers.Overlaps(_.EventType, eventType, compilation));
            if (other.Method is not null && (Owns(type, method) || Owns(type, other.Method)))
            {
                context.ReportDiagnostic(Diagnostic.Create(OneMethodRule, method.Locations.FirstOrDefault(), eventType.Name, other.Method.Name, method.Name, type.Name));
            }
        }
    }

    static void AnalyzeReducer(SymbolAnalysisContext context, INamedTypeSymbol type, IMethodSymbol[] batchMethods, IMethodSymbol[] perEventMethods)
    {
        foreach (var method in batchMethods.Skip(1).Where(method => Owns(type, method)))
        {
            context.ReportDiagnostic(Diagnostic.Create(ReducerRule, method.Locations.FirstOrDefault(), type.Name, $"declares a second batch method '{method.Name}'"));
        }

        if (perEventMethods.Length > 0 && (Owns(type, batchMethods[0]) || perEventMethods.Any(method => Owns(type, method))))
        {
            context.ReportDiagnostic(Diagnostic.Create(ReducerRule, batchMethods[0].Locations.FirstOrDefault(), type.Name, $"declares batch method '{batchMethods[0].Name}' and per-event method '{perEventMethods[0].Name}'"));
        }
    }

    static bool Owns(INamedTypeSymbol type, IMethodSymbol method) => SymbolEqualityComparer.Default.Equals(method.ContainingType, type);
}
