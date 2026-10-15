// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Reports types implementing multiple projection or reducer interfaces.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MultipleArtifactInterfacesAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MultipleArtifactInterfaces,
        "An artifact must target one read model",
        "Type '{0}' implements multiple {1} interfaces for read models: {2}. Split it into one artifact per read model.",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Artifact identifiers are per class, so a projection or reducer cannot target multiple read models. Discovery throws when a type implements multiple closed versions of the same artifact interface.");

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
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class) return;

        ReportMultipleInterfaces(context, type, "Cratis.Chronicle.Projections.IProjectionFor`1");
        ReportMultipleInterfaces(context, type, WellKnownTypes.IReducerForName);
    }

    static void ReportMultipleInterfaces(SymbolAnalysisContext context, INamedTypeSymbol type, string metadataName)
    {
        var artifactInterface = context.Compilation.GetTypeByMetadataName(metadataName);
        if (artifactInterface is null) return;

        var interfaces = type.AllInterfaces.Where(_ => SymbolEqualityComparer.Default.Equals(_.OriginalDefinition, artifactInterface)).ToArray();
        if (interfaces.Length < 2) return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            type.Locations.FirstOrDefault(),
            type.Name,
            artifactInterface.Name,
            string.Join(", ", interfaces.Select(_ => _.TypeArguments[0].ToDisplayString()))));
    }
}
