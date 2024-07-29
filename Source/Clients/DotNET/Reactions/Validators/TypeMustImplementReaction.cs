// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactions.Validators;

/// <summary>
/// Exception that gets thrown when a type does not implement <see cref="IReaction"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="TypeMustImplementReaction"/>.
/// </remarks>
/// <param name="type">Violating type.</param>
public class TypeMustImplementReaction(Type type) : Exception($"Type '{type.AssemblyQualifiedName}' does not implement `IReaction` interface")
{
    /// <summary>
    /// Throw if the type does not implement <see cref="IReaction"/>.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <exception cref="TypeMustImplementReaction">Thrown if type does not implement a reducer.</exception>
    public static void ThrowIfTypeDoesNotImplementReaction(Type type)
    {
        if (!type.GetInterfaces().Any(_ => _ == typeof(IReaction)))
        {
            throw new TypeMustImplementReaction(type);
        }
    }
}
