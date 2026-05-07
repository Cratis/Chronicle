// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks if a reducer combines <c>[EventStore]</c> with explicit event sequence configuration.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReducerEventStoreAndEventSequenceAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ReducerCannotCombineEventStoreWithExplicitEventSequence,
        title: "Reducer cannot combine EventStore with explicit event sequence",
        messageFormat: "Reducer '{0}' declares [EventStore(\"{1}\")] and explicit event sequence configuration. Remove [EventSequence]/[EventLog] or Reducer(eventSequence: ...) to use EventStore inbox routing.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A reducer with [EventStore] must use implicit inbox routing and cannot also define an explicit event sequence.");

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

        if (!WellKnownTypes.ImplementsIReducer(namedTypeSymbol, context.Compilation))
        {
            return;
        }

        var eventStoreAttribute = namedTypeSymbol
            .GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == WellKnownTypes.EventStoreAttributeName);

        if (eventStoreAttribute is null)
        {
            return;
        }

        var hasEventSequenceAttribute = namedTypeSymbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.ToDisplayString() == WellKnownTypes.EventSequenceAttributeName ||
            attr.AttributeClass?.ToDisplayString() == WellKnownTypes.EventLogAttributeName);

        var reducerAttribute = namedTypeSymbol.GetAttributes().FirstOrDefault(attr =>
            attr.AttributeClass?.ToDisplayString() == WellKnownTypes.ReducerAttributeName);

        var hasEventSequenceOnReducerAttribute = reducerAttribute?.ConstructorArguments.Length > 1 &&
            reducerAttribute.ConstructorArguments[1].Value is string;

        if (!hasEventSequenceAttribute && !hasEventSequenceOnReducerAttribute)
        {
            return;
        }

        var eventStoreName = eventStoreAttribute.ConstructorArguments.FirstOrDefault().Value as string ?? string.Empty;

        var diagnostic = Diagnostic.Create(
            Rule,
            namedTypeSymbol.Locations.FirstOrDefault(),
            namedTypeSymbol.Name,
            eventStoreName);

        context.ReportDiagnostic(diagnostic);
    }
}