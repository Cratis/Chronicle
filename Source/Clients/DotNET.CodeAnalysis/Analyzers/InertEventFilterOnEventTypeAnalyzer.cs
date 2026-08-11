// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports an event stream metadata attribute on an event type, where nothing reads it.
/// </summary>
/// <remarks>
/// An append resolves its event source type and event stream type from the arguments it is given - the command's
/// metadata, the reactor's, or the explicit parameters - never from the CLR type of the event being appended.
/// Nothing in the client reads either attribute off an event type, so the value is not transmitted, does not
/// reach the appended event's context, and narrows nothing on the way back out.
/// <para>
/// Written because the attributes' own documentation used to say an event type was a valid placement, and that is
/// what an IDE shows at the moment of authoring. A developer who trusts it tags the event once at its
/// declaration and expects every append of it to carry that stream identity - then sees events land under the
/// default source and stream type, observers that filter on them match nothing, and no build error, startup
/// failure or runtime signal anywhere to say why. Re-reading the attribute to confirm it is present and spelled
/// right confirms the wrong conclusion.
/// </para>
/// <para>
/// The placement is refused rather than honoured, and deliberately so: both values are persisted in every
/// appended event's context and are load-bearing keys for the concurrency scope and for observer filtering.
/// Letting an event's CLR type contribute them would retroactively change stream identity for every event type
/// already declared, against data already written.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InertEventFilterOnEventTypeAnalyzer : DiagnosticAnalyzer
{
    static readonly string[] _streamMetadataAttributeNames =
    [
        "Cratis.Chronicle.Events.EventStreamTypeAttribute",
        "Cratis.Chronicle.Events.EventSourceTypeAttribute"
    ];

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.InertEventFilterOnEventType,
        title: "Event stream metadata attribute on an event type has no effect",
        messageFormat: "'{0}' on event type '{1}' has no effect - an append resolves its stream metadata from the append itself, never from the event's type",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Only a command or an observer carries event source type and event stream type: a command contributes them to the append it makes, and a reactor or reducer filters the events it observes on them. Nothing reads either attribute off an event type, so it neither tags what is appended nor narrows what is observed. Move it to the command that appends the event, or to the reactor or reducer that observes it.");

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
        if (!WellKnownTypes.HasEventTypeAttribute(typeSymbol))
        {
            return;
        }

        foreach (var attribute in typeSymbol.GetAttributes())
        {
            var name = attribute.AttributeClass?.OriginalDefinition.ToDisplayString();
            if (name is null || !Array.Exists(_streamMetadataAttributeNames, _ => string.Equals(_, name, StringComparison.Ordinal)))
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
}
