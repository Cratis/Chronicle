// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that warns when an event type is declared as a class instead of a record.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EventTypeRecordAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.EventTypeShouldBeRecordType,
        title: "Event types should be record types",
        messageFormat: "Event type '{0}' is a class. Declare it as a record to enforce immutability.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Events in Chronicle are permanent, immutable facts appended to the event log and replayed to rebuild state. Declaring an event as a class allows its properties to be modified after creation, which can lead to subtle bugs during replay or serialization. Use a record instead (e.g. public record MyEvent(string Name, int Count)) to get built-in immutability, concise positional syntax, and structural equality. If the event has many properties, a record with an init-only property body is also acceptable.");

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

        if (!WellKnownTypes.HasEventTypeAttribute(typeSymbol))
        {
            return;
        }

        // Only warn on classes (not structs, interfaces, etc.)
        if (typeSymbol.TypeKind != TypeKind.Class)
        {
            return;
        }

        // Already a record — nothing to do
        if (typeSymbol.IsRecord)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            typeSymbol.Locations.FirstOrDefault(),
            typeSymbol.Name));
    }
}
