// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Validates typed event-source filters and their relationship with explicit metadata filters.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TypedEventSourceFilterAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor UnknownStream = new(
        id: DiagnosticIds.UnknownEventStreamOnTypedEventSourceFilter,
        title: "Unknown event stream on typed event-source filter",
        messageFormat: "Event source '{0}' does not declare an event stream named '{1}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A typed event-source filter must name a stream declared by its event source definition.");

    static readonly DiagnosticDescriptor ContradictoryRouting = new(
        id: DiagnosticIds.ExplicitRoutingContradictsTypedEventSourceFilter,
        title: "Explicit routing contradicts typed event-source filter",
        messageFormat: "'{0}' contradicts [FromEventSource<{1}>] routing",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[FromEventSource<TSource>(stream)] determines both event-source and event-stream filters. Explicit metadata filters may only repeat those values.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [UnknownStream, ContradictoryRouting];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
    }

    static void Analyze(SymbolAnalysisContext context)
    {
        var observer = (INamedTypeSymbol)context.Symbol;
        var typedFilter = observer.GetAttributes().FirstOrDefault(attribute =>
            attribute.AttributeClass?.OriginalDefinition.ToDisplayString() == WellKnownTypes.FromEventSourceAttributeName);
        if (typedFilter is null)
        {
            return;
        }

        if (typedFilter.AttributeClass!.TypeArguments[0] is not INamedTypeSymbol eventSource)
        {
            return;
        }

        var stream = typedFilter.ConstructorArguments.FirstOrDefault().Value as string;
        if (string.IsNullOrEmpty(stream))
        {
            return;
        }

        var eventStream = stream!;
        var declaredStreams = eventSource.GetAttributes()
            .Where(attribute => attribute.AttributeClass?.ToDisplayString() == WellKnownTypes.EventStreamAttributeName)
            .Select(attribute => attribute.ConstructorArguments.FirstOrDefault().Value as string);
        if (!declaredStreams.Contains(eventStream, StringComparer.Ordinal))
        {
            context.ReportDiagnostic(Diagnostic.Create(UnknownStream, GetLocation(typedFilter, observer), eventSource.Name, eventStream));
            return;
        }

        var eventSourceName = GetEventSourceName(eventSource);
        ReportContradiction(context, observer, WellKnownTypes.EventSourceTypeAttributeName, eventSourceName, eventSource.Name);
        ReportContradiction(context, observer, WellKnownTypes.EventStreamTypeAttributeName, eventStream, eventSource.Name);
    }

    static void ReportContradiction(SymbolAnalysisContext context, INamedTypeSymbol observer, string attributeName, string expected, string eventSourceName)
    {
        var attribute = observer.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == attributeName);
        var actual = attribute?.ConstructorArguments.FirstOrDefault().Value as string;
        if (attribute is null || string.IsNullOrEmpty(actual) || string.Equals(actual, expected, StringComparison.Ordinal))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(ContradictoryRouting, GetLocation(attribute, observer), attribute.AttributeClass!.Name, eventSourceName));
    }

    static string GetEventSourceName(INamedTypeSymbol eventSource)
    {
        var attribute = eventSource.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == WellKnownTypes.EventSourceAttributeName);
        var name = attribute?.ConstructorArguments.FirstOrDefault().Value as string;
        return !string.IsNullOrEmpty(name) ? name! : eventSource.Name.Replace("EventSource", string.Empty);
    }

    static Location GetLocation(AttributeData attribute, INamedTypeSymbol type) =>
        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? type.Locations.FirstOrDefault() ?? Location.None;
}
