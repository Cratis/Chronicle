// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Reflection;

/// <summary>
/// Provides a set of methods for working with <see cref="ParameterInfo"/>.
/// </summary>
public static class ParameterExtensions
{
    /// <summary>
    /// Check if a parameter has an attribute associated with it.
    /// </summary>
    /// <typeparam name="T">Attribute type to check for.</typeparam>
    /// <param name="parameter"><see cref="ParameterInfo"/> to check.</param>
    /// <returns>True if there is an attribute, false if not.</returns>
    public static bool HasAttribute<T>(this ParameterInfo parameter)
        where T : Attribute
        => parameter.GetCustomAttribute<T>() != default;
}
