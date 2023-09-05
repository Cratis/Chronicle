// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reducers.Validators;

/// <summary>
/// Exception that gets thrown when a type does not implement <see cref="IReducerFor{T}"/>.
/// </summary>
public class TypeMustImplementReducer : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="TypeMustImplementReducer"/>.
    /// </summary>
    /// <param name="type">Violating type.</param>
    public TypeMustImplementReducer(Type type) : base($"Type '{type.AssemblyQualifiedName}' does not implement `IReducerFor<>` interface")
    {
    }

    /// <summary>
    /// Throw if the type does not implement <see cref="IReducerFor{T}"/>.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <exception cref="TypeMustImplementReducer">Thrown if type does not implement a reducer.</exception>
    public static void ThrowIfTypeDoesNotImplementReducer(Type type)
    {
        if (!type.GetInterfaces().Any(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IReducerFor<>)))
        {
            throw new TypeMustImplementReducer(type);
        }
    }
}
