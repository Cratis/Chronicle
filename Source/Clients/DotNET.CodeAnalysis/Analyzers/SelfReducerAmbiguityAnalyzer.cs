// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Validates definite declaration-level self-reducer dispatch ambiguity.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SelfReducerAmbiguityAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SelfReducerMethodMustBeUnambiguous,
        "Self reducer method must be unambiguous",
        "Self reducer '{0}' declares multiple handlers for the same event parameter '{1}'",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
    }

    static void Analyze(SymbolAnalysisContext context)
    {
        var model = (INamedTypeSymbol)context.Symbol;
        if (!SelfReducerPolicy.IsSelf(model, context.Compilation)) return;
        var methods = SelfReducerPolicy.Methods(model).Where(method => SelfReducerPolicy.IsValid(method, model, context.Compilation));
        foreach (var group in methods.GroupBy(method => method.Parameters[0].Type, SymbolEqualityComparer.Default).Where(group => group.Count() > 1))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, model.Locations.FirstOrDefault(), model.Name, group.Key!.Name));
        }

        // Interface overlap depends on the owning runtime registry; compilation membership is not authority.
    }
}
