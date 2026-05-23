// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks that a class implementing IConstraint does not inject ICommandPipeline or IEventLog.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConstraintSideEffectAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ConstraintMustNotHaveSideEffects,
        title: "Constraint must not have side effects",
        messageFormat: "Constraint '{0}' must not inject '{1}'. Constraints are pure rule builders and must not produce side effects.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Constraints are evaluated by Chronicle before each command is allowed to proceed. Injecting ICommandPipeline or IEventLog would cause commands to be issued or events to be appended as a side effect of constraint evaluation, creating unpredictable behavior and potential infinite loops. Remove the offending constructor parameter. If you need to react to events, implement a separate IReactor instead.");

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

        if (!WellKnownTypes.ImplementsIConstraint(namedTypeSymbol, context.Compilation))
        {
            return;
        }

        foreach (var constructor in namedTypeSymbol.Constructors)
        {
            if (constructor.IsImplicitlyDeclared)
            {
                continue;
            }

            foreach (var parameter in constructor.Parameters.Where(parameter => WellKnownTypes.IsEventLogOrCommandPipeline(parameter.Type)))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    parameter.Locations.FirstOrDefault(),
                    namedTypeSymbol.Name,
                    parameter.Type.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
