// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reducers.Validators;

/// <summary>
/// Exception that gets thrown when a type implements more than one <see cref="IReducerFor{T}"/>.
/// </summary>
public class TypeMustImplementOnlyOneReducer : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="TypeMustImplementOnlyOneReducer"/>.
    /// </summary>
    /// <param name="type">Violating type.</param>
    public TypeMustImplementOnlyOneReducer(Type type) : base($"Type '{type.AssemblyQualifiedName}' implements more than one `IReducerFor<>` interface")
    {
    }

    /// <summary>
    /// Throw if the type implements more than one <see cref="IReducerFor{T}"/>.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <exception cref="TypeMustImplementOnlyOneReducer">Thrown if type implements more than one reducer.</exception>
    public static void ThrowIfTypeImplementsMoreThanOneReducer(Type type)
    {
        var interfaces = type.GetInterfaces().Where(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IReducerFor<>));
        if (interfaces.Count() > 1)
        {
            throw new TypeMustImplementOnlyOneReducer(type);
        }
    }
}
