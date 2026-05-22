// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks that a class implementing IReactor does not inject IEventLog via its constructor.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReactorInjectionAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ReactorMustNotInjectIEventLog,
        title: "Reactor must not inject IEventLog",
        messageFormat: "Reactor '{0}' must not inject IEventLog. Reactors should trigger commands via ICommandPipeline, not append events directly.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Reactors observe events and should react by issuing commands, not by appending events directly via IEventLog.");

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

        if (!WellKnownTypes.ImplementsIReactor(namedTypeSymbol, context.Compilation))
        {
            return;
        }

        foreach (var constructor in namedTypeSymbol.Constructors)
        {
            if (constructor.IsImplicitlyDeclared)
            {
                continue;
            }

            foreach (var parameter in constructor.Parameters)
            {
                if (WellKnownTypes.IsIEventLog(parameter.Type))
                {
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        parameter.Locations.FirstOrDefault(),
                        namedTypeSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
