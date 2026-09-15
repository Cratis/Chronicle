// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Reducers.Validators;

/// <summary>
/// Exception that gets thrown when a reducer method declares its current read model parameter as non-nullable.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ReducerMethodCurrentReadModelMustBeNullable"/>.
/// </remarks>
/// <param name="reducerType">The reducer type holding the violating method.</param>
/// <param name="methodName">Name of the violating method.</param>
/// <param name="readModelType">Type of the read model the reducer reduces to.</param>
public class ReducerMethodCurrentReadModelMustBeNullable(Type reducerType, string methodName, Type readModelType)
    : Exception($"Reducer method '{reducerType.Name}.{methodName}' declares its current read model parameter as '{readModelType.Name}' rather than '{readModelType.Name}?'. The current read model is null for the event that creates the instance, so the method must accept null. Change the parameter to '{readModelType.Name}?'.")
{
    /// <summary>
    /// Throw if any reducer-shaped method on the type declares a non-nullable current read model parameter.
    /// </summary>
    /// <param name="reducerType">Reducer type to check.</param>
    /// <param name="readModelType">Type of the read model the reducer reduces to.</param>
    /// <param name="eventTypes">Known event types in the process.</param>
    /// <exception cref="ReducerMethodCurrentReadModelMustBeNullable">Thrown when a method would never be dispatched to because of its current read model parameter.</exception>
    public static void ThrowIfAnyMethodHasNonNullableCurrentReadModel(Type reducerType, Type readModelType, IEnumerable<Type> eventTypes)
    {
        var violating = reducerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(method =>
                method.HasReducerMethodShape(readModelType, eventTypes) &&
                method.HasNonNullableCurrentReadModelParameter());

        if (violating is not null)
        {
            throw new ReducerMethodCurrentReadModelMustBeNullable(reducerType, violating.Name, readModelType);
        }
    }
}
