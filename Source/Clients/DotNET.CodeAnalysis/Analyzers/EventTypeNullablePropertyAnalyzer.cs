// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that warns when event types have nullable properties.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EventTypeNullablePropertyAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.EventTypeHasNullableProperty,
        title: "Event type has nullable property",
        messageFormat: "Event type '{0}' has nullable property '{1}'. Consider modeling this as a separate domain event representing the specific fact that occurred, rather than making a property nullable.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Event types should not have nullable properties. Instead, use separate events to represent optional data.");

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

        foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (IsNullable(member.Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    member.Locations.FirstOrDefault() ?? Location.None,
                    typeSymbol.Name,
                    member.Name));
            }
        }
    }

    static bool IsNullable(ITypeSymbol typeSymbol) =>
        IsNullableValueType(typeSymbol) || IsNullableReferenceType(typeSymbol);

    static bool IsNullableValueType(ITypeSymbol typeSymbol) =>
        typeSymbol is INamedTypeSymbol namedType &&
        namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

    static bool IsNullableReferenceType(ITypeSymbol typeSymbol) =>
        typeSymbol.NullableAnnotation == NullableAnnotation.Annotated &&
        typeSymbol.IsReferenceType;
}
