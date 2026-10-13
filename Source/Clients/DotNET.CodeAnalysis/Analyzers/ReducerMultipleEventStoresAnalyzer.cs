// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks if a reducer reduces event types from multiple event stores.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReducerMultipleEventStoresAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ReducerEventTypesMustBeFromSameEventStore,
        title: "Reducer event types must be from the same event store",
        messageFormat: "Reducer '{0}' reduces event types from multiple event stores: {1}. All event types in a reducer must originate from the same event store.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A reducer subscribes to one event store's event stream. Reducing events from multiple event stores would require multiple subscriptions, which Chronicle does not support for a single reducer. Ensure all event types in this reducer are from the same event store, or split it into separate reducers—one per event store.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var evidence = new Lazy<ExplicitRegistrationEvidence>(() => ExplicitRegistrationEvidence.Create(start.Compilation, start.CancellationToken));
            start.RegisterSymbolAction(symbol => AnalyzeNamedType(symbol, evidence), SymbolKind.NamedType);
        });
    }

    static void AnalyzeNamedType(SymbolAnalysisContext context, Lazy<ExplicitRegistrationEvidence> evidence)
    {
        var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

        if (SelfReducerPolicy.IsSelf(namedTypeSymbol, context.Compilation))
        {
            AnalyzeSelf(context, namedTypeSymbol, evidence.Value);
            return;
        }
        if (!WellKnownTypes.ImplementsIReducer(namedTypeSymbol, context.Compilation))
        {
            return;
        }

        var eventStores = namedTypeSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Ordinary && !m.IsStatic)
            .Select(m => m.Parameters.Length > 0 ? m.Parameters[0].Type : null)
            .Where(t => t is not null && WellKnownTypes.HasEventTypeAttribute(t))
            .Select(t => WellKnownTypes.GetExplicitEventStoreName(t!))
            .Where(_ => _ is not null)
            .Select(_ => _!)
            .Distinct()
            .ToList();

        if (eventStores.Count > 1)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                namedTypeSymbol.Locations.FirstOrDefault(),
                namedTypeSymbol.Name,
                string.Join(", ", eventStores.Select(WellKnownTypes.FormatEventStoreName)));
            context.ReportDiagnostic(diagnostic);
        }
    }

    static void AnalyzeSelf(SymbolAnalysisContext context, INamedTypeSymbol model, ExplicitRegistrationEvidence evidence)
    {
        if (SelfReducerPolicy.Attributes(model).Any(attribute => attribute.AttributeClass?.ToDisplayString() == WellKnownTypes.EventStoreAttributeName)) return;
        var handlers = SelfReducerPolicy.Methods(model).Where(method => SelfReducerPolicy.IsValid(method, model, context.Compilation)).ToArray();
        var events = SelfReducerPolicy.Types(context.Compilation.Assembly.GlobalNamespace)
            .Where(type => WellKnownTypes.HasEventTypeAttribute(type) || WellKnownTypes.HasEventTypeGenerationForAttribute(type))
            .Concat(evidence.EventTypes.OfType<INamedTypeSymbol>());
        var stores = events.Where(type => handlers.Any(method => context.Compilation.ClassifyCommonConversion(type, method.Parameters[0].Type) is { IsImplicit: true, IsUserDefined: false }))
            .Select(WellKnownTypes.GetExplicitEventStoreName).Where(store => store is not null).Distinct().ToArray();
        if (stores.Length > 1)
        {
            // Visible registrations can belong to different clients; runtime ownership remains authoritative.
            var diagnostic = Diagnostic.Create(
                Rule,
                model.Locations.FirstOrDefault(),
                DiagnosticSeverity.Warning,
                null,
                null,
                model.Name,
                string.Join(", ", stores.Select(store => WellKnownTypes.FormatEventStoreName(store!))));
            context.ReportDiagnostic(diagnostic);
        }
    }
}
