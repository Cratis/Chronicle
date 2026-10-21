// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers.Validators;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Infers one model only for explicit reducer intent, using the existing instance dispatch shapes.
/// </summary>
internal static class RegisteredReducerReadModel
{
    /// <summary>
    /// Resolves the unique supported instance-method model and validates declaration agreement and current-state nullability.
    /// </summary>
    /// <param name="type">The explicitly nominated reducer.</param>
    /// <param name="eventTypes">The finalized owning registry.</param>
    /// <returns>The inferred model.</returns>
    /// <exception cref="CannotInferReducerReadModel">No single compatible model can be inferred.</exception>
    internal static Type Infer(Type type, IEventTypes eventTypes)
    {
        var known = eventTypes.AllClrTypes.ToArray();
        var candidates = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(_ => !_.IsSpecialName && !_.ContainsGenericParameters)
            .Select(_ => (Method: _, Parameters: _.GetParameters()))
            .Where(_ => _.Parameters.Length >= 2 && IsRegisteredEvent(_.Parameters[0].ParameterType, known) && _.Method.HasReducerMethodShape(_.Parameters[1].ParameterType, known))
            .Select(_ => _.Parameters[1].ParameterType)
            .Distinct().ToArray();
        var declared = type.GetInterfaces().Where(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IReducerFor<>))
            .Select(_ => _.GetGenericArguments()[0]).Distinct().ToArray();
        if (candidates.Length != 1 || declared.Length > 1 || (declared.Length == 1 && declared[0] != candidates[0]))
        {
            throw new CannotInferReducerReadModel(type, candidates);
        }

        ReducerMethodCurrentReadModelMustBeNullable.ThrowIfAnyMethodHasNonNullableCurrentReadModel(type, candidates[0], known);
        return candidates[0];
    }

    static bool IsRegisteredEvent(Type type, IEnumerable<Type> known) => type != typeof(object) && known.Any(_ => _.IsAssignableTo(type));
}
