// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that warns when event types define nullable properties.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EventTypeNullablePropertiesAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.EventTypeShouldAvoidNullableProperties,
        title: "Event types should avoid nullable properties",
        messageFormat: "Event type '{0}' has nullable property '{1}'. Prefer modeling optional facts as separate event types.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Nullable properties are supported on events but are often an anti-pattern in event sourcing. Prefer dedicated event types when expressing optional facts.");

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

        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (property.IsStatic ||
                property.IsIndexer ||
                !SymbolEqualityComparer.Default.Equals(property.ContainingType, typeSymbol))
            {
                continue;
            }

            if (!IsNullable(property))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                property.Locations.FirstOrDefault(),
                typeSymbol.Name,
                property.Name));
        }
    }

    static bool IsNullable(IPropertySymbol property) =>
        property.NullableAnnotation == NullableAnnotation.Annotated ||
        (property.Type is INamedTypeSymbol namedType &&
         namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T);
}
