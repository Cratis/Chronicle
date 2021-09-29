// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Extensions.GraphQL
{
    /// <summary>
    /// Extension methods for working with types and methods related to schema.
    /// </summary>
    public static class SchemaRouteItemTypesExtensions
    {
        /// <summary>
        /// Get methods with a specific attribute
        /// </summary>
        /// <param name="type"><see cref="Type"/> to get from.</param>
        /// <typeparam name="TAttribute">Type of attribute - must implement <see cref="ICanHavePath"/></typeparam>
        /// <returns>Enumerable of <see cref="MethodInfo"/></returns>
        public static IEnumerable<MethodInfo> GetMethodsWithAttribute<TAttribute>(this Type type)
            where TAttribute : Attribute, ICanHavePath
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(_ => _.GetCustomAttribute<TAttribute>() != null);
        }

        /// <summary>
        /// Check if a method has explicit path specified.
        /// </summary>
        /// <param name="method"><see cref="MethodInfo"/> to check</param>
        /// <typeparam name="TAttribute">Type of attribute - must implement <see cref="ICanHavePath"/></typeparam>
        /// <returns>True if it has, false if not</returns>
        public static bool HasPath<TAttribute>(this MethodInfo method)
            where TAttribute : Attribute, ICanHavePath
        {
            var attribute = method.GetCustomAttribute<TAttribute>();
            return attribute?.HasPath == true;
        }

        /// <summary>
        /// Get path for a method.
        /// </summary>
        /// <param name="method"><see cref="MethodInfo"/> to get for</param>
        /// <typeparam name="TAttribute">Type of attribute - must implement <see cref="ICanHavePath"/></typeparam>
        /// <returns>Path for the method</returns>
        public static string GetPath<TAttribute>(this MethodInfo method)
            where TAttribute : Attribute, ICanHavePath
        {
            var attribute = method.GetCustomAttribute<TAttribute>();
            return (attribute?.HasPath == true ? attribute?.Path : method.Name) ?? string.Empty;
        }
    }
}
