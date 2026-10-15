// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Validates the structural self-reducer method contract.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SelfReducerMethodAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SelfReducerMethodSignatureMustMatchAllowed,
        "Self reducer method signature must match allowed signatures",
        "Self reducer method '{0}' must be public static Reduce(event, nullable current[, EventContext]) returning the model or Task of the model",
        "Usage",
        DiagnosticSeverity.Warning,
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
        if (!SelfReducerPolicy.CanBeSelf(model, context.Compilation)) return;
        var self = SelfReducerPolicy.IsSelf(model, context.Compilation);
        foreach (var method in SelfReducerPolicy.Methods(model).Where(method => method.Name == "Reduce" &&
            (self || SelfReducerPolicy.IsNearMiss(method, model, context.Compilation)) && !SelfReducerPolicy.IsValid(method, model, context.Compilation)))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations.FirstOrDefault(), method.Name));
        }
    }
}
