// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Orleans.StateMachines;

/// <summary>
/// Exception that gets thrown when a type is not a valid type for a state.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InvalidTypeForState"/> class.
/// </remarks>
/// <param name="type">Type that is invalid.</param>
public class InvalidTypeForState(Type type) : Exception($"Type '{type.FullName}' is not a valid type for a state. States must inherit from '{typeof(IState<>).FullName}'")
{
    /// <summary>
    /// Throw if the type is invalid.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <exception cref="InvalidTypeForState">Thrown if invalid.</exception>
    public static void ThrowIfInvalid(Type type)
    {
        if (!type.GetInterfaces().Any(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IState<>)))
        {
            throw new InvalidTypeForState(type);
        }
    }
}
