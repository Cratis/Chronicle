// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks if a model-bound projection references event types from multiple event stores.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ModelBoundProjectionMultipleEventStoresAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ModelBoundProjectionEventTypesMustBeFromSameEventStore,
        title: "Model-bound projection event types must be from the same event store",
        messageFormat: "Projection '{0}' references event types from multiple event stores: {1}. All event types in a projection must originate from the same event store.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A model-bound projection may only reference event types from a single event store.");

    static readonly string[] ModelBoundProjectionAttributeNames =
    [
        "Cratis.Chronicle.Projections.ModelBound.FromEventAttribute`1",
        "Cratis.Chronicle.Projections.ModelBound.RemovedWithAttribute`1",
        "Cratis.Chronicle.Projections.ModelBound.RemovedWithJoinAttribute`1",
        "Cratis.Chronicle.Projections.ModelBound.JoinAttribute`1",
        "Cratis.Chronicle.Projections.ModelBound.ChildrenFromAttribute`1",
        "Cratis.Chronicle.Projections.ModelBound.FromEveryAttribute`1",
        "Cratis.Chronicle.Projections.ModelBound.SetFromAttribute`1",
        "Cratis.Chronicle.Projections.ModelBound.SetFromContextAttribute`1",
        "Cratis.Chronicle.Projections.ModelBound.SetValueAttribute`1",
        "Cratis.Chronicle.Projections.ModelBound.AddFromAttribute`1",
        "Cratis.Chronicle.Projections.ModelBound.SubtractFromAttribute`1",
        "Cratis.Chronicle.Projections.ModelBound.IncrementAttribute`1",
        "Cratis.Chronicle.Projections.ModelBound.DecrementAttribute`1",
        "Cratis.Chronicle.Projections.ModelBound.CountAttribute`1"
    ];

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

        var eventStores = CollectEventStoresFromAttributes(namedTypeSymbol.GetAttributes());

        foreach (var member in namedTypeSymbol.GetMembers())
        {
            eventStores.AddRange(CollectEventStoresFromAttributes(member.GetAttributes()));
        }

        var distinctEventStores = eventStores.Distinct().ToList();

        if (distinctEventStores.Count > 1)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                namedTypeSymbol.Locations.FirstOrDefault(),
                namedTypeSymbol.Name,
                string.Join(", ", distinctEventStores));
            context.ReportDiagnostic(diagnostic);
        }
    }

    static List<string> CollectEventStoresFromAttributes(ImmutableArray<AttributeData> attributes)
    {
        var eventStores = new List<string>();

        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass == null)
            {
                continue;
            }

            var constructedFrom = attribute.AttributeClass.ConstructedFrom;
            var attributeName = $"{constructedFrom.ContainingNamespace.ToDisplayString()}.{constructedFrom.MetadataName}";
            if (!ModelBoundProjectionAttributeNames.Contains(attributeName))
            {
                continue;
            }

            if (attribute.AttributeClass.TypeArguments.Length > 0)
            {
                var eventType = attribute.AttributeClass.TypeArguments[0];
                var eventStore = WellKnownTypes.GetEventStoreName(eventType);
                if (eventStore is not null)
                {
                    eventStores.Add(eventStore);
                }
            }
        }

        return eventStores;
    }
}
