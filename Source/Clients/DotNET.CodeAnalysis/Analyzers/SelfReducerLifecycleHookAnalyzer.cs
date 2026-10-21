// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Validates the optional static self-reducer replay hook contract.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SelfReducerLifecycleHookAnalyzer : DiagnosticAnalyzer
{
    static readonly string[] HookNames = ["BeginReplay", "EndReplay", "BeginReplayPartition", "EndReplayPartition"];
    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SelfReducerLifecycleHookSignatureMustMatchAllowed,
        "Self reducer lifecycle hook signature must match allowed signatures",
        "Self reducer hook '{0}' must be a unique public static non-generic Task method with the required Partition and trailing service parameters only",
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
        foreach (var group in SelfReducerPolicy.Methods(model).Where(method => HookNames.Contains(method.Name)).GroupBy(method => method.Name))
        {
            foreach (var method in group.Where(method => group.Count() > 1 || !IsValid(method, model, context.Compilation)))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations.FirstOrDefault(), method.Name));
            }
        }
    }

    static bool IsValid(IMethodSymbol method, INamedTypeSymbol model, Compilation compilation)
    {
        var partitioned = method.Name.EndsWith("Partition", StringComparison.Ordinal);
        var partition = compilation.GetTypeByMetadataName("Cratis.Chronicle.Observation.Partition");
        if (method is not { IsStatic: true, DeclaredAccessibility: Accessibility.Public, IsGenericMethod: false } ||
            !SelfReducerPolicy.Equal(method.ReturnType, compilation.GetTypeByMetadataName("System.Threading.Tasks.Task")) ||
            !method.Parameters.All(SelfReducerPolicy.IsPlainParameter) ||
            (partitioned && !SelfReducerPolicy.Equal(method.Parameters.FirstOrDefault()?.Type, partition)))
        {
            return false;
        }
        return method.Parameters.Skip(partitioned ? 1 : 0).All(parameter =>
            parameter.Type is INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Interface, IsUnboundGenericType: false } type &&
            !SelfReducerPolicy.Equal(type, model) && !SelfReducerPolicy.Equal(type, partition) &&
            !SelfReducerPolicy.Equal(type, compilation.GetTypeByMetadataName(WellKnownTypes.EventContextName)) &&
            !WellKnownTypes.HasEventTypeAttribute(type) && !WellKnownTypes.HasEventTypeGenerationForAttribute(type));
    }
}
