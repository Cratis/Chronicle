// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Diagnoses fold-shaped read model methods that discovery ignores because of their name or instance declaration.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SelfReducerIntentAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SelfReducerMethodWillNotBeDiscovered,
        "Read model fold method will not be discovered as a self reducer",
        "Read model fold method '{0}' will not be discovered because {1}; use public static Reduce(event, nullable current[, EventContext]) returning the model or Task of the model",
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
        if (!self && !SelfReducerPolicy.Attributes(model).Any(attribute => attribute.AttributeClass?.ToDisplayString() == WellKnownTypes.ReadModelAttributeName) &&
            !SelfReducerPolicy.Methods(model).Any(method => SelfReducerPolicy.IsNearMiss(method, model, context.Compilation)))
        {
            return;
        }

        foreach (var method in model.GetMembers().OfType<IMethodSymbol>().Where(method =>
            method.MethodKind == MethodKind.Ordinary && (method.Name != "Reduce" || !method.IsStatic) &&
            HasFoldShape(method, model, context.Compilation)))
        {
            // CHR0058 already reports invalid sibling Reduce declarations on a recognized self reducer.
            if (self && method.Name == "Reduce") continue;
            var reason = (method.Name, method.IsStatic) switch
            {
                ("Reduce", _) => "it is not static",
                (_, true) => "its name is not 'Reduce'",
                _ => "its name is not 'Reduce' and it is not static"
            };
            context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations.FirstOrDefault(), method.Name, reason));
        }
    }

    static bool HasFoldShape(IMethodSymbol method, INamedTypeSymbol model, Compilation compilation) =>
        method is { DeclaredAccessibility: Accessibility.Public, IsGenericMethod: false } &&
        method.Parameters.Length is 2 or 3 && method.Parameters.All(SelfReducerPolicy.IsPlainParameter) &&
        method.Parameters[0].Type is { TypeKind: TypeKind.Class or TypeKind.Interface } eventType &&
        WellKnownTypes.HasEventTypeAttribute(eventType) &&
        SelfReducerPolicy.Equal(method.Parameters[1].Type, model) &&
        (method.Parameters.Length == 2 || SelfReducerPolicy.Equal(method.Parameters[2].Type, compilation.GetTypeByMetadataName(WellKnownTypes.EventContextName))) &&
        SelfReducerPolicy.ReturnsModel(method, model, compilation);
}
