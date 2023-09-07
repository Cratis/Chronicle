// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

/// <summary>
/// Exception that gets thrown when a type is not a valid type for a state.
/// </summary>
public class InvalidTypeForState : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidTypeForState"/> class.
    /// </summary>
    /// <param name="type">Type that is invalid.</param>
    public InvalidTypeForState(Type type)
        : base($"Type '{type.FullName}' is not a valid type for a state. States must inherit from '{typeof(IState<>).FullName}'")
    {
    }

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
