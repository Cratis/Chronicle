// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis;

/// <summary>
/// Resolves the declared scenario terminal signatures once for each compilation.
/// </summary>
internal static class ScenarioTerminalMethods
{
    /// <summary>
    /// Resolve unambiguous scenario terminal method definitions in a compilation.
    /// </summary>
    /// <param name="compilation">The compilation supplying the testing and framework symbols.</param>
    /// <returns>The matching original method definitions.</returns>
    internal static ImmutableHashSet<IMethodSymbol> Resolve(Compilation compilation)
    {
        var methods = ImmutableHashSet.CreateBuilder<IMethodSymbol>(SymbolEqualityComparer.Default);
        Add(compilation, methods, "Cratis.Chronicle.Testing.Reactors.ReactorSourceGivenBuilder`1", "Events", HasEventArray);
        Add(compilation, methods, "Cratis.Chronicle.Testing.Reactors.ReactorScenario`1", "Redeliver", method => HasOccurrence(method, compilation));
        Add(compilation, methods, "Cratis.Chronicle.Testing.EventSequences.EventSourceGivenBuilder", "Events", HasEventArray);
        Add(compilation, methods, "Cratis.Chronicle.Testing.EventSequences.EventSourceWhenBuilder", "Events", HasFirstEventAndArray);
        return methods.ToImmutable();
    }

    static void Add(Compilation compilation, ImmutableHashSet<IMethodSymbol>.Builder methods, string metadataName, string methodName, Func<IMethodSymbol, bool> matchesParameters)
    {
        var type = compilation.GetTypeByMetadataName(metadataName);
        var candidates = type?.GetMembers(methodName).OfType<IMethodSymbol>()
            .Where(method => !method.IsStatic && method.Arity == 0 && matchesParameters(method) && IsFrameworkAwaitable(method.ReturnType, compilation))
            .ToArray();
        if (candidates?.Length == 1)
        {
            methods.Add(candidates[0].OriginalDefinition);
        }
    }

    static bool HasEventArray(IMethodSymbol method) =>
        method.Parameters.Length == 1 && IsEventArray(method.Parameters[0]);

    static bool HasFirstEventAndArray(IMethodSymbol method) =>
        method.Parameters.Length == 2 &&
        method.Parameters[0].RefKind == RefKind.None &&
        method.Parameters[0].Type.SpecialType == SpecialType.System_Object &&
        IsEventArray(method.Parameters[1]);

    static bool IsEventArray(IParameterSymbol parameter) =>
        parameter.IsParams && parameter.RefKind == RefKind.None &&
        parameter.Type is IArrayTypeSymbol { IsSZArray: true, ElementType.SpecialType: SpecialType.System_Object };

    static bool HasOccurrence(IMethodSymbol method, Compilation compilation) =>
        method.Parameters.Length == 1 && method.Parameters[0].RefKind == RefKind.None &&
        SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, compilation.GetTypeByMetadataName("Cratis.Chronicle.Testing.Reactors.RecordedOccurrence"));

    static bool IsFrameworkAwaitable(ITypeSymbol type, Compilation compilation)
    {
        var definition = type.OriginalDefinition;
        return SymbolEqualityComparer.Default.Equals(definition, compilation.GetTypeByMetadataName("System.Threading.Tasks.Task")) ||
            SymbolEqualityComparer.Default.Equals(definition, compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1")) ||
            SymbolEqualityComparer.Default.Equals(definition, compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask")) ||
            SymbolEqualityComparer.Default.Equals(definition, compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1"));
    }
}
