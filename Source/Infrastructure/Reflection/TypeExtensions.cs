// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Reflection;

/// <summary>
/// Extension methods for <see cref="Type"/>.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Get the type string for a <see cref="Type"/>.
    /// </summary>
    /// <param name="type">Type to get from.</param>
    /// <returns>A type string.</returns>
    public static string GetTypeString(this Type type) =>
        (type.Namespace?.StartsWith("System") ?? false) ?
            (type.FullName ?? type.Name) :
            $"{type.FullName ?? type.Name}, {type.Assembly.GetName().Name}";

    /// <summary>
    /// Check whether or not a type is in a nullable context.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if it is in a nullable context, false if not.</returns>
    public static bool IsNullableContext(this Type type) =>
        type.GetCustomAttributes().Any(_ => _.GetType().FullName == "System.Runtime.CompilerServices.NullableContextAttribute");

    /// <summary>
    /// Check whether or not a member that implements <see cref="ICustomAttributeProvider"/> is a nullable reference type.
    /// </summary>
    /// <param name="member">Member that implements <see cref="ICustomAttributeProvider"/> to check.</param>
    /// <returns>True if it nullable, false if not.</returns>
    public static bool IsNullableReferenceType(this ICustomAttributeProvider member) =>
        member.GetCustomAttributes(false).Any(_ => _.GetType().FullName == "System.Runtime.CompilerServices.NullableAttribute");
}
