// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis;

/// <summary>
/// Checks the parameters and results of batch methods against what the client can supply and read.
/// </summary>
internal static class BatchMethodSignature
{
    /// <summary>
    /// Describes what is wrong with a reducer batch method, if anything.
    /// </summary>
    /// <param name="method">The batch method.</param>
    /// <param name="kind">The <see cref="BatchObserverKind"/> of its type.</param>
    /// <param name="symbols">The <see cref="BatchSymbols"/> of the compilation.</param>
    /// <returns>The problem, or null when the method can be called.</returns>
    internal static string? ProblemWithReducerMethod(IMethodSymbol method, BatchObserverKind kind, BatchSymbols symbols)
    {
        var readModel = BatchObservers.ReadModelOf(method.ContainingType, kind, symbols);
        if (readModel is null)
        {
            return null;
        }

        if (kind == BatchObserverKind.SelfReducer && method is not { IsStatic: true, DeclaredAccessibility: Accessibility.Public })
        {
            return "a self-reducing read model's batch method is public and static";
        }

        if (kind == BatchObserverKind.SelfReducer && !string.Equals(method.Name, BatchObservers.ReduceMethodName, StringComparison.Ordinal))
        {
            return $"a self-reducing read model's batch method is named '{BatchObservers.ReduceMethodName}'; the client discovers no other name, so this method would never be called";
        }

        if (method.Parameters.Length < 2 || !SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, readModel))
        {
            return $"take the current read model '{readModel.Name}?' as the second parameter";
        }

        if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, readModel) && !symbols.IsTaskOf(method.ReturnType, readModel))
        {
            return $"return '{readModel.Name}?' or 'Task<{readModel.Name}?>'; null removes the read model";
        }

        return ProblemWithDependencies(method, 2, symbols);
    }

    /// <summary>
    /// Describes what is wrong with the parameters after the leading ones, if anything.
    /// </summary>
    /// <param name="method">The batch method.</param>
    /// <param name="first">The index of the first parameter that is resolved rather than passed.</param>
    /// <param name="symbols">The <see cref="BatchSymbols"/> of the compilation.</param>
    /// <returns>The problem, or null when every parameter can be resolved.</returns>
    internal static string? ProblemWithDependencies(IMethodSymbol method, int first, BatchSymbols symbols)
    {
        if (method.IsGenericMethod)
        {
            return "a batch method cannot be generic";
        }

        foreach (var parameter in method.Parameters)
        {
            if (parameter.RefKind != RefKind.None || parameter.IsParams || parameter.IsOptional)
            {
                return $"parameter '{parameter.Name}' must be an ordinary parameter, not ref, out, in, params or optional";
            }
        }

        for (var index = first; index < method.Parameters.Length; index++)
        {
            var parameter = method.Parameters[index];
            var type = parameter.Type;
            if (BatchSymbols.Is(type, symbols.BatchContext) || BatchSymbols.Is(type, symbols.CancellationToken))
            {
                continue;
            }

            if (BatchSymbols.Is(type, symbols.EventContext))
            {
                return $"parameter '{parameter.Name}' is an EventContext; each event in the batch carries its own context, and the method takes a BatchContext";
            }

            if (BatchSymbols.Is(type, symbols.EventBatch))
            {
                return $"parameter '{parameter.Name}' is a second EventBatch; a batch method takes one, first";
            }

            if (type.IsValueType || type.SpecialType == SpecialType.System_String)
            {
                return $"parameter '{parameter.Name}' of type '{type.ToDisplayString()}' cannot be resolved; after the batch a method takes a BatchContext, a CancellationToken or services";
            }
        }

        return null;
    }
}
