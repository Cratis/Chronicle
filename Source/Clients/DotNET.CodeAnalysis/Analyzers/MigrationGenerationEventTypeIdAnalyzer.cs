// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports when the two event type generations referenced by an
/// <c>EventTypeMigration&lt;TUpgrade, TPrevious&gt;</c> do not share one explicit <c>[EventType]</c> id.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MigrationGenerationEventTypeIdAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.MigrationGenerationEventTypeId,
        title: "Event type migration generations must share one explicit [EventType] id",
        messageFormat: "Event type generations '{0}' and '{1}' referenced by migration '{2}' must share one explicit [EventType] id and differ only by generation",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An EventTypeMigration<TUpgrade, TPrevious> upcasts stored events of the previous generation into the newer generation. Chronicle keys generations by the [EventType] id, so both generations must carry the same explicit id and differ only by their generation number. If the id is absent (defaulting to the type name) or the two ids differ, Chronicle treats them as unrelated event types and the migration never applies.");

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
