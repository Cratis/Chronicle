// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports a read model marked both [Sink] and [Passive].
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SinkAndPassiveCombinedAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.SinkCannotBeCombinedWithPassive,
        title: "[Sink] cannot be combined with [Passive]",
        messageFormat: "'{0}' is marked [Passive] and [Sink]; a passive read model has no sink to store to. Remove one of them.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[Passive] means the read model is never stored and is projected on demand, which is the sink type None. [Sink] names the sink configuration a read model is stored in. A read model cannot be both.");

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
        var type = (INamedTypeSymbol)context.Symbol;
        var attributes = type.GetAttributes();
        if (attributes.Length < 2)
        {
            return;
        }

        var sink = attributes.FirstOrDefault(_ => Is(_, WellKnownTypes.SinkAttributeName));
        if (sink is null || !attributes.Any(_ => Is(_, WellKnownTypes.PassiveAttributeName)))
        {
            return;
        }

        var location = sink.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? type.Locations.FirstOrDefault();
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, type.Name));
    }

    static bool Is(AttributeData attribute, string name) =>
        string.Equals(attribute.AttributeClass?.ToDisplayString(), name, StringComparison.Ordinal);
}
