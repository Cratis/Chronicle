// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks if a reactor handles event types from multiple event stores.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReactorMultipleEventStoresAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ReactorEventTypesMustBeFromSameEventStore,
        title: "Reactor event types must be from the same event store",
        messageFormat: "Reactor '{0}' handles event types from multiple event stores: {1}. All event types in a reactor must originate from the same event store.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A reactor may only handle event types from a single event store.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

        if (!WellKnownTypes.ImplementsIReactor(namedTypeSymbol, context.Compilation))
        {
            return;
        }

        var eventStores = namedTypeSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Ordinary && !m.IsStatic)
            .Select(m => m.Parameters.Length > 0 ? m.Parameters[0].Type : null)
            .Where(t => t is not null && WellKnownTypes.HasEventTypeAttribute(t))
            .Select(t => WellKnownTypes.GetEventStoreName(t!))
            .Where(name => name is not null)
            .Distinct()
            .ToList();

        if (eventStores.Count > 1)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                namedTypeSymbol.Locations.FirstOrDefault(),
                namedTypeSymbol.Name,
                string.Join(", ", eventStores));
            context.ReportDiagnostic(diagnostic);
        }
    }
}
