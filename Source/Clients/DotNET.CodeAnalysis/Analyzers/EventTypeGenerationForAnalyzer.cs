// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that validates usage of the <c>EventTypeGenerationFor&lt;TEventType&gt;</c> attribute.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EventTypeGenerationForAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor TargetMustBeEventTypeRule = new(
        id: DiagnosticIds.EventTypeGenerationForMustReferenceEventType,
        title: "[EventTypeGenerationFor<T>] must reference a type marked with [EventType]",
        messageFormat: "'{0}' declares itself as a generation for '{1}', but '{1}' is not marked with [EventType]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "EventTypeGenerationFor<TEventType> ties a previous-generation representation to its current event type. The referenced type must be marked with [EventType] directly - referencing a type with no event type identity, or another [EventTypeGenerationFor<T>] representation, leaves the event type id unresolvable.");

    static readonly DiagnosticDescriptor CannotCombineWithEventTypeRule = new(
        id: DiagnosticIds.EventTypeGenerationForCannotCombineWithEventType,
        title: "[EventType] and [EventTypeGenerationFor<T>] cannot be combined",
        messageFormat: "'{0}' is marked with both [EventType] and [EventTypeGenerationFor<T>]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A type is either the current event type (marked with [EventType]) or a previous generation of one (marked with [EventTypeGenerationFor<T>]), never both. Remove one of the two attributes.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(TargetMustBeEventTypeRule, CannotCombineWithEventTypeRule);

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

        var generationForAttribute = WellKnownTypes.GetEventTypeGenerationForAttributeData(typeSymbol);
        if (generationForAttribute is null)
        {
            return;
        }

        if (WellKnownTypes.HasEventTypeAttribute(typeSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                CannotCombineWithEventTypeRule,
                typeSymbol.Locations.FirstOrDefault(),
                typeSymbol.Name));
            return;
        }

        var referencedType = WellKnownTypes.GetEventTypeGenerationForTarget(generationForAttribute);
        if (referencedType is null || !WellKnownTypes.HasEventTypeAttribute(referencedType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                TargetMustBeEventTypeRule,
                typeSymbol.Locations.FirstOrDefault(),
                typeSymbol.Name,
                referencedType?.Name ?? "?"));
        }
    }
}
