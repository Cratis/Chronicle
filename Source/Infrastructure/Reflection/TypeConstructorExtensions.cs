// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Reflection;

/// <summary>
/// Provides a set of methods for working with <see cref="Type">types</see> and their constructors.
/// </summary>
public static class TypeConstructorExtensions
{
    /// <summary>
    /// Check if a type has a default constructor that does not take any arguments.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>true if it has a default constructor, false if not.</returns>
    public static bool HasDefaultConstructor(this Type type)
    {
        return type.GetTypeInfoDetails().HasDefaultConstructor ||
            type.GetConstructors().Any(c => c.GetParameters().Length == 0);
    }

    /// <summary>
    /// Check if a type has a non default constructor.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>true if it has a non default constructor, false if not.</returns>
    public static bool HasNonDefaultConstructor(this Type type)
    {
        return type.GetConstructors().Any(c => c.GetParameters().Length > 0);
    }

    /// <summary>
    /// Get the default constructor from a type.
    /// </summary>
    /// <param name="type">Type to get from.</param>
    /// <returns>The default <see cref="ConstructorInfo"/>.</returns>
    public static ConstructorInfo GetDefaultConstructor(this Type type)
    {
        return type.GetConstructors().Single(c => c.GetParameters().Length == 0);
    }

    /// <summary>
    /// Get the non default constructor, assuming there is only one.
    /// </summary>
    /// <param name="type">Type to get from.</param>
    /// <returns>The <see cref="ConstructorInfo"/> for the constructor.</returns>
    public static ConstructorInfo GetNonDefaultConstructor(this Type type)
    {
        return type.GetConstructors().Single(c => c.GetParameters().Length > 0);
    }

    /// <summary>
    /// Get the non default constructor matching the types.
    /// </summary>
    /// <param name="type">Type to get from.</param>
    /// <param name="parameterTypes">Types for matching the parameters.</param>
    /// <returns>The <see cref="ConstructorInfo"/> for the constructor.</returns>
    public static ConstructorInfo GetNonDefaultConstructor(this Type type, Type[] parameterTypes)
    {
        return type.GetTypeInfo().GetConstructor(parameterTypes)!;
    }

    /// <summary>
    /// Get the non default constructor with the greatest number of parameters.
    /// Should be used with care. Constructors are not ordered, so if there are multiple constructors with the
    /// same number of parameters, it is indeterminate which will be returned.
    /// </summary>
    /// <param name="type">Type to get from.</param>
    /// <returns>The <see cref="ConstructorInfo"/> for the constructor.</returns>
    public static ConstructorInfo GetNonDefaultConstructorWithGreatestNumberOfParameters(this Type type)
    {
        return type.GetTypeInfo()
                        .DeclaredConstructors.Where(c => c.GetParameters().Length > 0)
                        .OrderByDescending((t) => t.GetParameters().Length)
                        .FirstOrDefault()!;
    }
}
