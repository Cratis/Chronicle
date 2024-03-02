// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Reflection;

namespace Aksio.Cratis.Reducers.Validators;

/// <summary>
/// Exception that gets thrown when a type is not adorned with the <see cref="ReducerAttribute"/>.
/// </summary>
public class TypeMustBeAdornedWithReducerAttribute : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="TypeMustBeAdornedWithReducerAttribute"/>.
    /// </summary>
    /// <param name="type">Violating type.</param>
    public TypeMustBeAdornedWithReducerAttribute(Type type) : base($"Type '{type.AssemblyQualifiedName}' must be adorned with a `[Reducer]` attribute")
    {
    }

    /// <summary>
    /// Throw if the type is not adorned with the <see cref="ReducerAttribute"/>.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <exception cref="TypeMustBeAdornedWithReducerAttribute">Thrown if the type is missing the <see cref="ReducerAttribute"/>.</exception>
    public static void ThrowIfReducerAttributeMissing(Type type)
    {
        if (!type.HasAttribute<ReducerAttribute>())
        {
            throw new TypeMustBeAdornedWithReducerAttribute(type);
        }
    }
}
