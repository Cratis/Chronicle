// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports an event-metadata filter attribute on a projection, where it does nothing.
/// </summary>
/// <remarks>
/// A projection observes every event of the types its definition declares. It cannot filter on event metadata at
/// all: there is no field for a filter anywhere in a projection's definition, so no client can express one and no
/// kernel could honour it. A reactor or a reducer can, and does.
/// <para>
/// Written because the attributes' own documentation used to say a projection could, and that is what an IDE
/// shows at the moment of authoring. A developer who trusts it designs a filtered projection that silently
/// observes everything - no build error, no startup failure, no runtime signal, just a read model carrying values
/// from streams it was never meant to see. The natural way to debug that, re-reading the attribute to confirm it
/// is present and spelled right, confirms the wrong conclusion. Correcting the documentation helps the reader who
/// reads it; this helps the one who does not.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InertEventFilterOnProjectionAnalyzer : DiagnosticAnalyzer
{
    static readonly string[] _filterAttributeNames =
    [
        "Cratis.Chronicle.Events.EventStreamTypeAttribute",
        "Cratis.Chronicle.Events.EventSourceTypeAttribute",
        "Cratis.Chronicle.FilterEventsByTagAttribute"
    ];

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.InertEventFilterOnProjection,
        title: "Event filter attribute on a projection has no effect",
        messageFormat: "'{0}' on projection '{1}' has no effect - a projection observes every event of the types it declares and cannot filter on event metadata",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Only a reactor or a reducer filters observed events on metadata; a projection's definition has no field for a filter, so nothing is transmitted and nothing is applied. Narrow the projection by the event types it declares, or pair it with a reactor or reducer that owns the filtered subset.");

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
        var typeSymbol = (INamedTypeSymbol)context.Symbol;
        if (!IsProjection(typeSymbol, context.Compilation))
        {
            return;
        }

        foreach (var attribute in typeSymbol.GetAttributes())
        {
            var name = attribute.AttributeClass?.OriginalDefinition.ToDisplayString();
            if (name is null || !Array.Exists(_filterAttributeNames, _ => string.Equals(_, name, StringComparison.Ordinal)))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? typeSymbol.Locations.FirstOrDefault(),
                attribute.AttributeClass!.Name,
                typeSymbol.Name));
        }
    }

    static bool IsProjection(INamedTypeSymbol typeSymbol, Compilation compilation) =>
        WellKnownTypes.ImplementsIProjectionFor(typeSymbol, compilation) ||
        typeSymbol.GetAttributes().Any(_ =>
            _.AttributeClass?.OriginalDefinition.ToDisplayString()
                .StartsWith("Cratis.Chronicle.Projections.ModelBound.", StringComparison.Ordinal) == true) ||
        typeSymbol.GetMembers().Any(member => member.GetAttributes().Any(_ =>
            _.AttributeClass?.OriginalDefinition.ToDisplayString()
                .StartsWith("Cratis.Chronicle.Projections.ModelBound.", StringComparison.Ordinal) == true));
}
