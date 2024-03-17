// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Reflection;

/// <summary>
/// Provides a set of methods for working with <see cref="PropertyInfo"/>.
/// </summary>
public static class MemberInfoExtensions
{
    /// <summary>
    /// Check if a property has an attribute associated with it.
    /// </summary>
    /// <typeparam name="T">Attribute type to check for.</typeparam>
    /// <param name="member"><see cref="PropertyInfo"/> to check.</param>
    /// <returns>True if there is an attribute, false if not.</returns>
    public static bool HasAttribute<T>(this MemberInfo member)
        where T : Attribute
        => member.GetCustomAttribute<T>() != default;
}
