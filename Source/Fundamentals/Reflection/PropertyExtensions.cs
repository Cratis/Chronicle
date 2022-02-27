// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Reflection
{
    /// <summary>
    /// Provides a set of methods for working with <see cref="PropertyInfo"/>.
    /// </summary>
    public static class PropertyExtensions
    {
        /// <summary>
        /// Check if a type has an attribute associated with it.
        /// </summary>
        /// <typeparam name="T">Attribute type to check for.</typeparam>
        /// <param name="property"><see cref="PropertyInfo"/> to check.</param>
        /// <returns>True if there is an attribute, false if not.</returns>
        public static bool HasAttribute<T>(this PropertyInfo property)
            where T : Attribute
            => property.GetCustomAttribute<T>() != default;
    }
}
