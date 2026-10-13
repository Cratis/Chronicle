// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks if model-bound projection attributes reference event types with the EventType attribute.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ModelBoundProjectionAttributeAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ModelBoundProjectionEventTypeMustHaveAttribute,
        title: "Model bound projection attribute must reference event type with [EventType] attribute",
        messageFormat: "Type '{0}' referenced in model bound projection attribute must be marked with [EventType] attribute",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Chronicle model-bound projection attributes (e.g., [FromEvent<T>], [Join<T>]) require the referenced type to carry [EventType] so Chronicle can correlate events during replay. Add [EventType(\"<guid>\")] to the referenced type, or use a type that is already marked with [EventType].");

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
        context.RegisterCompilationStartAction(start =>
        {
            var evidence = new Lazy<ExplicitRegistrationEvidence>(() => ExplicitRegistrationEvidence.Create(start.Compilation, start.CancellationToken));
            start.RegisterSymbolAction(symbol => AnalyzeNamedType(symbol, evidence.Value), SymbolKind.NamedType);
            start.RegisterSymbolAction(symbol => AnalyzeProperty(symbol, evidence.Value), SymbolKind.Property);
        });
    }

    static void AnalyzeNamedType(SymbolAnalysisContext context, ExplicitRegistrationEvidence evidence)
    {
        var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
        AnalyzeAttributes(context, namedTypeSymbol.GetAttributes(), namedTypeSymbol.Locations, evidence);
    }

    static void AnalyzeProperty(SymbolAnalysisContext context, ExplicitRegistrationEvidence evidence)
    {
        var propertySymbol = (IPropertySymbol)context.Symbol;
        AnalyzeAttributes(context, propertySymbol.GetAttributes(), propertySymbol.Locations, evidence);
    }

    static void AnalyzeAttributes(SymbolAnalysisContext context, ImmutableArray<AttributeData> attributes, ImmutableArray<Location> locations, ExplicitRegistrationEvidence evidence)
    {
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

            // Check the generic type argument
            if (attribute.AttributeClass.TypeArguments.Length > 0)
            {
                var eventType = attribute.AttributeClass.TypeArguments[0];
                if (!evidence.IsEventType(eventType, context.Compilation))
                {
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? locations.FirstOrDefault(),
                        eventType.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
