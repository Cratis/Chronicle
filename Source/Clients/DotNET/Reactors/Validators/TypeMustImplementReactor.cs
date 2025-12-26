// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.Validators;

/// <summary>
/// Exception that gets thrown when a type does not implement <see cref="IReactor"/>.
/// </summary>
/// <param name="type">Violating type.</param>
public class TypeMustImplementReactor(Type type) : Exception($"Type '{type.AssemblyQualifiedName}' does not implement `IReactor` interface")
{
    /// <summary>
    /// Throw if the type does not implement <see cref="IReactor"/>.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <exception cref="TypeMustImplementReactor">Thrown if type does not implement a reducer.</exception>
    public static void ThrowIfTypeDoesNotImplementReactor(Type type)
    {
        if (!type.GetInterfaces().Any(_ => _ == typeof(IReactor)))
        {
            throw new TypeMustImplementReactor(type);
        }
    }
}
