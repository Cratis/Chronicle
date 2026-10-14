// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Reports declared concurrency that applies across event source instances.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConcurrencyWithoutEventSourceIdAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.ConcurrencyWithoutEventSourceId,
        "Concurrency applies across event sources",
        "Concurrency without EventSourceId applies across all event sources; include EventSourceId for a per-source guard",
        "Usage",
        DiagnosticSeverity.Info,
        true,
        description: "A definition or stream concurrency declaration without EventSourceId is an independent cross-source condition, not a read guard.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
    }

    static void Analyze(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (!type.AllInterfaces.Any(_ => _.ToDisplayString() == "Cratis.Chronicle.EventSources.IEventSource")) return;
        foreach (var attribute in type.GetAttributes().Where(_ => _.AttributeClass?.ToDisplayString() == "Cratis.Chronicle.EventSources.EventSourceAttribute" || _.AttributeClass?.ToDisplayString() == "Cratis.Chronicle.EventSources.EventStreamAttribute"))
        {
            var concurrency = attribute.NamedArguments.FirstOrDefault(_ => _.Key == "Concurrency").Value;
            var sourceFlag = concurrency.Type?.GetMembers("EventSourceId").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue;
            if (concurrency.Value is null || sourceFlag is null) continue;
            var value = Convert.ToInt64(concurrency.Value);
            if (value == 0 || (value & Convert.ToInt64(sourceFlag)) != 0) continue;
            var syntax = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken) as AttributeSyntax;
            var argument = syntax?.ArgumentList?.Arguments.FirstOrDefault(_ => _.NameEquals?.Name.Identifier.ValueText == "Concurrency");
            if (argument is not null) context.ReportDiagnostic(Diagnostic.Create(Rule, argument.Expression.GetLocation()));
        }
    }
}
