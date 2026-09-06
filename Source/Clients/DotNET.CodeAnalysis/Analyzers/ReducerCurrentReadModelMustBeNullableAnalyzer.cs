// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks a reducer method accepts a nullable current read model.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReducerCurrentReadModelMustBeNullableAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ReducerCurrentReadModelMustBeNullable,
        title: "Reducer current read model parameter must be nullable",
        messageFormat: "Reducer method '{0}' declares its current read model parameter as '{1}' rather than '{1}?'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Chronicle passes null as the current read model for the event that brings an instance into existence, so a reducer method cannot promise the parameter is never null. Declare the parameter as nullable and handle the null case as the creation of the instance.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var methodSymbol = (IMethodSymbol)context.Symbol;

        if (methodSymbol.MethodKind != MethodKind.Ordinary || methodSymbol.IsStatic)
        {
            return;
        }

        if (!WellKnownTypes.ImplementsIReducer(methodSymbol.ContainingType, context.Compilation))
        {
            return;
        }

        var readModelType = GetReadModelType(methodSymbol.ContainingType, context.Compilation);
        if (readModelType is null)
        {
            return;
        }

        var parameters = methodSymbol.Parameters;
        if (parameters.Length is < 2 or > 3)
        {
            return;
        }

        if (!WellKnownTypes.HasEventTypeAttribute(parameters[0].Type))
        {
            return;
        }

        var current = parameters[1];
        if (!SymbolEqualityComparer.Default.Equals(current.Type, readModelType))
        {
            return;
        }

        // Only an explicit non-nullable annotation is a broken promise. In a #nullable disable context the
        // annotation carries no claim at all, which is why Chronicle keeps dispatching to those methods.
        if (current.NullableAnnotation != NullableAnnotation.NotAnnotated)
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            Rule,
            current.Locations.FirstOrDefault(),
            methodSymbol.Name,
            readModelType.Name);
        context.ReportDiagnostic(diagnostic);
    }

    static ITypeSymbol? GetReadModelType(INamedTypeSymbol reducerType, Compilation compilation)
    {
        var reducerForInterface = compilation.GetTypeByMetadataName(WellKnownTypes.IReducerForName);
        if (reducerForInterface is null)
        {
            return null;
        }

        var closed = reducerType.AllInterfaces.FirstOrDefault(_ =>
            _.IsGenericType && SymbolEqualityComparer.Default.Equals(_.OriginalDefinition, reducerForInterface));

        return closed?.TypeArguments.FirstOrDefault();
    }
}
