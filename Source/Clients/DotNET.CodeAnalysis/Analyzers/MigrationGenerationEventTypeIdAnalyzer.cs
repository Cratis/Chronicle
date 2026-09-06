// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports when the two event type generations referenced by an
/// <c>EventTypeMigration&lt;TUpgrade, TPrevious&gt;</c> do not belong to one event type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MigrationGenerationEventTypeIdAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.MigrationGenerationEventTypeId,
        title: "Event type migration generations must belong to one event type",
        messageFormat: "Event type generations '{0}' and '{1}' referenced by migration '{2}' must resolve to the same event type and differ only by generation",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An EventTypeMigration<TUpgrade, TPrevious> upcasts stored events of the previous generation into the newer generation. Chronicle keys generations by event type id, so both generations must resolve to the same id and differ only by their generation number. Prefer marking the previous generation with [EventTypeGenerationFor<TUpgrade>(N)] - it resolves the id from TUpgrade directly, so the two can never drift apart. If using [EventType] on both, the id must be given explicitly and match on both; an absent id (defaulting to the type name) or a mismatched id makes Chronicle treat them as unrelated event types, and the migration never applies.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    static void AnalyzeType(SymbolAnalysisContext context)
    {
        var typeSymbol = (INamedTypeSymbol)context.Symbol;

        var migrationBase = WellKnownTypes.GetEventTypeMigrationBase(typeSymbol);
        if (migrationBase is null || migrationBase.TypeArguments.Length != 2)
        {
            return;
        }

        var upgrade = migrationBase.TypeArguments[0];
        var previous = migrationBase.TypeArguments[1];

        var previousGenerationFor = WellKnownTypes.GetEventTypeGenerationForAttributeData(previous);
        if (previousGenerationFor is not null)
        {
            var referencedType = WellKnownTypes.GetEventTypeGenerationForTarget(previousGenerationFor);
            if (referencedType is null || !SymbolEqualityComparer.Default.Equals(referencedType, upgrade))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    typeSymbol.Locations.FirstOrDefault(),
                    upgrade.Name,
                    previous.Name,
                    typeSymbol.Name));
            }

            return;
        }

        var upgradeAttribute = WellKnownTypes.GetEventTypeAttributeData(upgrade);
        var previousAttribute = WellKnownTypes.GetEventTypeAttributeData(previous);
        if (upgradeAttribute is null || previousAttribute is null)
        {
            return;
        }

        var upgradeId = WellKnownTypes.GetEventTypeExplicitId(upgradeAttribute);
        var previousId = WellKnownTypes.GetEventTypeExplicitId(previousAttribute);

        if (upgradeId is null || previousId is null || upgradeId != previousId)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                typeSymbol.Locations.FirstOrDefault(),
                upgrade.Name,
                previous.Name,
                typeSymbol.Name));
        }
    }
}
