// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Validators;

/// <summary>
/// Exception that gets thrown when a type does not implement <see cref="IProjectionFor{T}"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="TypeMustImplementProjection"/>.
/// </remarks>
/// <param name="type">Violating type.</param>
public class TypeMustImplementProjection(Type type) : Exception($"Type '{type.AssemblyQualifiedName}' does not implement `IProjectionFor<>` interface")
{
    /// <summary>
    /// Throw if the type does not implement <see cref="IProjectionFor{T}"/>.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <exception cref="TypeMustImplementProjection">Thrown if type does not implement a reducer.</exception>
    public static void ThrowIfTypeDoesNotImplementProjection(Type type)
    {
        if (!type.GetInterfaces().Any(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IProjectionFor<>)))
        {
            throw new TypeMustImplementProjection(type);
        }
    }
}
