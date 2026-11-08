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
    /// <param name="compilation">The owning compilation.</param>
    /// <returns>The problem, or null when the method can be called.</returns>
    internal static string? ProblemWithReducerMethod(IMethodSymbol method, BatchObserverKind kind, Compilation compilation)
    {
        var readModel = BatchObservers.ReadModelOf(method.ContainingType, kind, compilation);
        if (readModel is null)
        {
            return null;
        }

        if (kind == BatchObserverKind.SelfReducer && method is not { IsStatic: true, DeclaredAccessibility: Accessibility.Public })
        {
            return "a self-reducing read model's batch method is public and static";
        }

        if (method.Parameters.Length < 2 || !SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, readModel))
        {
            return $"take the current read model '{readModel.Name}?' as the second parameter";
        }

        if (!ReturnsReadModel(method, readModel, compilation))
        {
            return $"return '{readModel.Name}?' or 'Task<{readModel.Name}?>'; null removes the read model";
        }

        return ProblemWithDependencies(method, 2, compilation);
    }

    /// <summary>
    /// Describes what is wrong with the parameters after the leading ones, if anything.
    /// </summary>
    /// <param name="method">The batch method.</param>
    /// <param name="first">The index of the first parameter that is resolved rather than passed.</param>
    /// <param name="compilation">The owning compilation.</param>
    /// <returns>The problem, or null when every parameter can be resolved.</returns>
    internal static string? ProblemWithDependencies(IMethodSymbol method, int first, Compilation compilation)
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
            if (BatchObservers.Is(type, WellKnownTypes.BatchContextName, compilation) || BatchObservers.Is(type, "System.Threading.CancellationToken", compilation))
            {
                continue;
            }

            if (BatchObservers.Is(type, WellKnownTypes.EventContextName, compilation))
            {
                return $"parameter '{parameter.Name}' is an EventContext; each event in the batch carries its own context, and the method takes a BatchContext";
            }

            if (BatchObservers.Is(type, WellKnownTypes.EventBatchName, compilation))
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

    static bool ReturnsReadModel(IMethodSymbol method, ITypeSymbol readModel, Compilation compilation) =>
        SymbolEqualityComparer.Default.Equals(method.ReturnType, readModel) ||
        (method.ReturnType is INamedTypeSymbol { IsGenericType: true } task &&
         SymbolEqualityComparer.Default.Equals(task.OriginalDefinition, compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1")) &&
         SymbolEqualityComparer.Default.Equals(task.TypeArguments[0], readModel));
}
